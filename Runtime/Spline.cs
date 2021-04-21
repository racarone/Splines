using System;
using UnityEngine;

namespace Splines
{
    [Flags]
    public enum SplineAxis
    {
        X   = 1 << 0,
        Y   = 1 << 1,
        Z   = 1 << 2,
        All = X | Y | Z
    }
    
    public struct SplinePoint
    {
        public float key;
        public Vector3 position;
        public Vector3 inTangent;
        public Vector3 outTangent;
        public Quaternion rotation;
        public Vector3 scale;
        public CurveTangentMode tangentMode;

        public SplinePoint(float key, Vector3 position, Vector3 inTangent, Vector3 outTangent, Quaternion rotation, Vector3 scale, CurveTangentMode tangentMode = CurveTangentMode.Auto)
        {
            this.key = key;
            this.position = position;
            this.inTangent = inTangent;
            this.outTangent = outTangent;
            this.rotation = rotation;
            this.scale = scale;
            this.tangentMode = tangentMode;
        }

        public SplinePoint(float key, Vector3 position, Vector3 inTangent, Vector3 outTangent, Quaternion rotation)
            : this(key, position, inTangent, outTangent, rotation, Vector3.one) { }

        public SplinePoint(float key, Vector3 position, Vector3 inTangent, Vector3 outTangent)
            : this(key, position, inTangent, outTangent, Quaternion.identity, Vector3.one) { }

        public SplinePoint(float key, Vector3 position)
            : this(key, position, Vector3.zero, Vector3.zero, Quaternion.identity, Vector3.one) { }
    }

    [AddComponentMenu("Splines/Spline")]
    public sealed class Spline : MonoBehaviour, ISerializationCallbackReceiver
    {
        public delegate void OnSplineUpdated();
        public delegate void OnSplinePointAdded(int index);
        public delegate void OnSplinePointRemoved(int index);

        public event OnSplineUpdated onUpdated;
        public event OnSplinePointAdded onPointAdded;
        public event OnSplinePointRemoved onPointRemoved;

        [SerializeField]
        Curve<Vector3> m_PositionCurve = new Curve<Vector3>();
        public Curve<Vector3> positionCurve => m_PositionCurve;

        [SerializeField]
        Curve<Quaternion> m_RotationCurve = new Curve<Quaternion>();
        public Curve<Quaternion> rotationCurve => m_RotationCurve;

        [SerializeField]
        Curve<Vector3> m_ScaleCurve = new Curve<Vector3>();
        public Curve<Vector3> scaleCurve => m_ScaleCurve;

        [SerializeField]
        Curve<float> m_LengthCacheCurve = new Curve<float>();

        [SerializeField]
        SplineAxis m_Axis = SplineAxis.All;
        public SplineAxis axis
        {
            get => m_Axis;
            set => m_Axis = value;
        }

        [SerializeField]
        bool m_Closed;
        public bool closed => m_Closed;

        [SerializeField, Min(0.0001f)]
        float m_Duration = 1.0f;
        public float duration
        {
            get => m_Duration;
            set => m_Duration = value;
        }

        [SerializeField, Min(1)]
        int m_CacheStepsPerSegment = 10;
        public int cacheStepsPerSegment
        {
            get => m_CacheStepsPerSegment;
            set => m_CacheStepsPerSegment = value;
        }

        [SerializeField]
        Vector3 m_DefaultUpDirection = Vector3.up;
        public Vector3 defaultUpDirection
        {
            get => m_DefaultUpDirection;
            set => m_DefaultUpDirection = value;
        }

        [SerializeField]
        bool m_ShowBounds;
        public bool showBounds
        {
            get => m_ShowBounds;
            set => m_ShowBounds = value;
        }

        [SerializeField]
        int m_SerializedVersion;

        [NonSerialized]
        int m_DeserializedVersion;
        
        public int version => m_DeserializedVersion;

        public int pointCount => m_PositionCurve.count;
        
        public int segmentCount => m_Closed ? m_PositionCurve.count : Mathf.Max(m_PositionCurve.count - 1, 1);

        public float splineLength => m_LengthCacheCurve.count > 0 ? m_LengthCacheCurve.last.key : 0;
        
        public bool wasUndoRedoPerformed => m_DeserializedVersion != m_SerializedVersion;

        void OnValidate()
        {
            if (m_PositionCurve.count == 0)
            {
                AddPoint(new Vector3(0, 0, 0), Space.Self);
                AddPoint(new Vector3(0, 0, 1), Space.Self);
                UpdateSpline();
            }
            
            m_DefaultUpDirection = m_DefaultUpDirection.normalized;
            m_PositionCurve.ClearCache();
            m_RotationCurve.ClearCache();
            m_ScaleCurve.ClearCache();
        }
        
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_SerializedVersion = m_DeserializedVersion;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_DeserializedVersion = m_SerializedVersion;
        }

        #region Modify

        public void UpdateSpline()
        {
            // Ensure each curves looping status matches with that of the spline component
            if (m_Closed)
            {
                float lastKey = m_PositionCurve.count > 0 ? m_PositionCurve.last.key : 0.0f;
                float loopKey = lastKey + 1.0f;
                m_PositionCurve.SetLoopKey(loopKey);
                m_RotationCurve.SetLoopKey(loopKey);
                m_ScaleCurve.SetLoopKey(loopKey);
            }
            else
            {
                m_PositionCurve.ClearLoopKey();
                m_RotationCurve.ClearLoopKey();
                m_ScaleCurve.ClearLoopKey();
            }

            m_PositionCurve.ComputeAutoTangents();
            m_RotationCurve.ComputeAutoTangents();
            m_ScaleCurve.ComputeAutoTangents();

            m_LengthCacheCurve.Clear();

            int lastPoint = pointCount - 1;
            float accumulatedLength = 0f;

            for (int segmentIndex = 0; segmentIndex < segmentCount; ++segmentIndex)
            {
                ref var thisPoint = ref m_PositionCurve[segmentIndex];
                ref var nextPoint = ref m_PositionCurve[segmentIndex == lastPoint ? 0 : segmentIndex + 1];

                if (m_Axis != SplineAxis.All)
                {
                    if ((m_Axis & SplineAxis.X) == 0)
                    {
                        thisPoint.value.x = 0;
                        thisPoint.inTangent.x = 0;
                        thisPoint.outTangent.x = 0;
                        nextPoint.value.x = 0;
                        nextPoint.inTangent.x = 0;
                        nextPoint.outTangent.x = 0;
                    }
                    
                    if ((m_Axis & SplineAxis.Y) == 0)
                    {
                        thisPoint.value.y = 0;
                        thisPoint.inTangent.y = 0;
                        thisPoint.outTangent.y = 0;
                        nextPoint.value.y = 0;
                        nextPoint.inTangent.y = 0;
                        nextPoint.outTangent.y = 0;
                    }
                    
                    if ((m_Axis & SplineAxis.Z) == 0)
                    {
                        thisPoint.value.z = 0;
                        thisPoint.inTangent.z = 0;
                        thisPoint.outTangent.z = 0;
                        nextPoint.value.z = 0;
                        nextPoint.inTangent.z = 0;
                        nextPoint.outTangent.z = 0;
                    }
                }

                for (int step = 0; step < m_CacheStepsPerSegment; ++step)
                {
                    float t = (float) step / m_CacheStepsPerSegment;
                    float segmentLength;

                    switch (thisPoint.tangentMode)
                    {
                        // Special cases for linear or constant segments
                        case CurveTangentMode.Linear:
                            segmentLength = Vector3.Scale(nextPoint.value - thisPoint.value, transform.localScale).magnitude * t;
                            break;
                        case CurveTangentMode.Constant:
                            segmentLength = 0.0f;
                            break;
                        default:
                            segmentLength = step == 0
                                ? 0f
                                : CurveMath.ComputeArcLength(thisPoint.value,
                                                             thisPoint.outTangent,
                                                             nextPoint.inTangent,
                                                             nextPoint.value,
                                                             t,
                                                             transform.localScale);
                            break;
                    }

                    m_LengthCacheCurve.Add(segmentLength + accumulatedLength, segmentIndex + t, 0, 0, CurveTangentMode.Linear);
                }

                accumulatedLength += CurveMath.ComputeArcLength(thisPoint.value,
                                                                thisPoint.outTangent,
                                                                nextPoint.inTangent,
                                                                nextPoint.value,
                                                                1f,
                                                                transform.localScale);
            }

            m_LengthCacheCurve.Add(accumulatedLength, segmentCount, 0, 0, CurveTangentMode.Linear);
            ++m_DeserializedVersion;
            onUpdated?.Invoke();
        }

        public Bounds ComputeBounds(Space space)
        {
            var bounds = new Bounds();

            if (pointCount < 2)
                return bounds;
            
            for (int i = 0; i < segmentCount; ++i)
            {
                Bounds segmentBounds;
                if (m_Closed && i == pointCount - 1)
                {
                    segmentBounds = CurveMath.ComputeBounds(m_PositionCurve[i].value, m_PositionCurve[i].outTangent,
                                                            m_PositionCurve[0].inTangent, m_PositionCurve[0].value);
                }
                else
                {
                    segmentBounds = CurveMath.ComputeBounds(m_PositionCurve[i].value, m_PositionCurve[i].outTangent,
                                                            m_PositionCurve[i + 1].inTangent, m_PositionCurve[i + 1].value);
                }
                    
                bounds.Encapsulate(segmentBounds);
            }

            if (space == Space.World)
            {
                var minWS = transform.TransformPoint(bounds.min);
                var maxWS = transform.TransformPoint(bounds.max);
                bounds.SetMinMax(minWS, maxWS);
            }
            
            return bounds;
        }

        public void SetClosed(bool newClosed, bool updateSpline = true)
        {
            m_Closed = newClosed;
            ++m_DeserializedVersion;
            
            if (updateSpline)
                UpdateSpline();
        }

        public void Clear(bool updateSpline = true)
        {
            m_PositionCurve.Clear();
            m_RotationCurve.Clear();
            m_ScaleCurve.Clear();
            ++m_DeserializedVersion;
            
            if (updateSpline)
                UpdateSpline();
        }

        public void AddPoint(Vector3 position, Space space, bool updateSpline = true)
        {
            var transformedPosition = space == Space.World ? transform.InverseTransformPoint(position) : position;

            // Add the spline point at the end of the array, adding 1.0 to the current last input key.
            var key = m_PositionCurve.count > 0 ? m_PositionCurve[m_PositionCurve.count - 1].key + 1.0f : 0.0f;
            m_PositionCurve.Add(key, transformedPosition, Vector3.zero, Vector3.zero, CurveTangentMode.Auto);
            m_RotationCurve.Add(key, Quaternion.identity, Quaternion.identity, Quaternion.identity, CurveTangentMode.Auto);
            m_ScaleCurve.Add(key, Vector3.one, Vector3.zero, Vector3.zero, CurveTangentMode.Auto);
            ++m_DeserializedVersion;

            onPointAdded?.Invoke(m_PositionCurve.count - 1);
            
            if (updateSpline)
                UpdateSpline();
        }

        public void InsertPointAtIndex(int index, Vector3 position, Space space, bool updateSpline = true)
        {
            if (index < 0 || index > pointCount)
                return;

            var transformedPosition = space == Space.World ? transform.InverseTransformPoint(position) : position;
            var key = index == 0 ? 0.0f : m_PositionCurve[index - 1].key + 1.0f;
            m_PositionCurve.Insert(index, key, transformedPosition, Vector3.zero, Vector3.zero, CurveTangentMode.Auto);
            m_RotationCurve.Insert(index, key, Quaternion.identity, Quaternion.identity, Quaternion.identity, CurveTangentMode.Auto);
            m_ScaleCurve.Insert(index, key, Vector3.one, Vector3.zero, Vector3.zero, CurveTangentMode.Auto);
            ++m_DeserializedVersion;

            onPointAdded?.Invoke(index);

            // Adjust subsequent points' input keys to make room for the value just added
            for (int i = index + 1; i < pointCount; ++i)
            {
                m_PositionCurve[i].key += 1.0f;
            }

            if (updateSpline)
                UpdateSpline();
        }

        public void RemovePointAtIndex(int index, bool updateSpline = true)
        {
            if (index < 0 || index > pointCount)
                return;

            m_PositionCurve.RemoveAt(index);

            onPointRemoved?.Invoke(index);

            while (index < pointCount)
            {
                m_PositionCurve[index].key -= 1.0f;
                index++;
            }

            ++m_DeserializedVersion;
            
            if (updateSpline)
                UpdateSpline();
        }

        #endregion

        #region Set

        public void SetPositionAtIndex(int index, Vector3 position, Space space, bool updateSpline = true)
        {
            if (index < 0 || index >= pointCount)
                return;

            var transformedPosition = space == Space.World ? transform.InverseTransformPoint(position) : position;
            m_PositionCurve[index].value = transformedPosition;
            ++m_DeserializedVersion;

            if (updateSpline)
                UpdateSpline();
        }

        public void SetTangentAtIndex(int index, Vector3 tangent, Space space, bool updateSpline = true)
        {
            if (index < 0 || index >= pointCount)
                return;

            var transformedTangent = space == Space.World ? transform.InverseTransformDirection(tangent) : tangent;
            m_PositionCurve[index].inTangent = transformedTangent;
            m_PositionCurve[index].outTangent = transformedTangent;
            m_PositionCurve[index].tangentMode = CurveTangentMode.Free;
            ++m_DeserializedVersion;

            if (updateSpline)
                UpdateSpline();
        }

        public void SetTangentsAtIndex(int index, Vector3 tangentIn, Vector3 tangentOut, Space space, bool updateSpline = true)
        {
            if (index < 0 || index >= pointCount)
                return;

            var transformedInTangent = space == Space.World ? transform.InverseTransformDirection(tangentIn) : tangentIn;
            var transformedOutTangent = space == Space.World ? transform.InverseTransformDirection(tangentOut) : tangentOut;
            m_PositionCurve[index].inTangent = transformedInTangent;
            m_PositionCurve[index].outTangent = transformedOutTangent;
            m_PositionCurve[index].tangentMode = CurveTangentMode.Free;
            ++m_DeserializedVersion;

            if (updateSpline)
                UpdateSpline();
        }

        public void SetUpVectorAtIndex(int index, Vector3 upVector, Space space, bool updateSpline = true)
        {
            if (index < 0 || index >= pointCount)
                return;

            var transformedUpVector = space == Space.World ? transform.InverseTransformDirection(upVector.normalized) : upVector.normalized;
            m_RotationCurve[index].value = Quaternion.FromToRotation(m_DefaultUpDirection, transformedUpVector);
            ++m_DeserializedVersion;

            if (updateSpline)
                UpdateSpline();
        }

        public void SetQuaternionAtIndex(int index, Quaternion rotation, Space space, bool updateSpline = true)
        {
            if (index < 0 || index >= pointCount)
                return;

            m_RotationCurve[index].value = space == Space.World ? transform.rotation * rotation : rotation;
            ++m_DeserializedVersion;

            if (updateSpline)
                UpdateSpline();
        }

        public void SetLocalScaleAtIndex(int index, Vector3 scale, bool updateSpline = true)
        {
            if (index < 0 || index >= pointCount)
                return;

            m_ScaleCurve[index].value = scale;
            ++m_DeserializedVersion;

            if (updateSpline)
                UpdateSpline();
        }

        #endregion

        #region At Index

        static readonly Curve<Vector3>.Keyframe s_DummyPosition = new Curve<Vector3>.Keyframe {key = 0, value = Vector3.zero, inTangent = Vector3.forward, outTangent = Vector3.forward, tangentMode = CurveTangentMode.Constant};
        static readonly Curve<Quaternion>.Keyframe s_DummyRotation = new Curve<Quaternion>.Keyframe {key = 0, value = Quaternion.identity};
        static readonly Curve<Vector3>.Keyframe s_DummyScale = new Curve<Vector3>.Keyframe {key = 0, value = Vector3.one};

        Curve<Vector3>.Keyframe GetPositionSafe(int index)
        {
            if (pointCount <= 0) return s_DummyPosition;
            int clampedIndex = (m_Closed && index >= pointCount) ? 0 : Mathf.Clamp(index, 0, pointCount - 1);
            return m_PositionCurve[clampedIndex];
        }

        Curve<Quaternion>.Keyframe GetRotationSafe(int index)
        {
            if (pointCount <= 0) return s_DummyRotation;
            int clampedIndex = (m_Closed && index >= pointCount) ? 0 : Mathf.Clamp(index, 0, pointCount - 1);
            return m_RotationCurve[clampedIndex];
        }

        Curve<Vector3>.Keyframe GetScaleSafe(int index)
        {
            if (pointCount <= 0) return s_DummyScale;
            int clampedIndex = (m_Closed && index >= pointCount) ? 0 : Mathf.Clamp(index, 0, pointCount - 1);
            return m_ScaleCurve[clampedIndex];
        }

        public Vector3 GetPositionAtIndex(int index, Space space)
        {
            var keyframe = GetPositionSafe(index);
            return space == Space.World ? transform.TransformPoint(keyframe.value) : keyframe.value;
        }

        public float GetKeyAtIndex(int index)
        {
            var keyframe = GetPositionSafe(index);
            return keyframe.key;
        }

        public Vector3 GetTangentAtIndex(int index, Space space)
        {
            return GetOutTangentAtIndex(index, space);
        }

        public Vector3 GetInTangentAtIndex(int index, Space space)
        {
            var keyframe = GetPositionSafe(index);
            return space == Space.World ? transform.TransformPoint(keyframe.inTangent) : keyframe.inTangent;
        }

        public Vector3 GetOutTangentAtIndex(int index, Space space)
        {
            var keyframe = GetPositionSafe(index);
            return space == Space.World ? transform.TransformPoint(keyframe.outTangent) : keyframe.outTangent;
        }

        public Vector3 GetForwardAtIndex(int index, Space space)
        {
            return GetOutTangentAtIndex(index, space).normalized;
        }

        public Quaternion GetQuaternionAtIndex(int index, Space space)
        {
            var keyframe = GetRotationSafe(index);
            return space == Space.World ? transform.rotation * keyframe.value : keyframe.value;
        }

        public Quaternion GetRotationAtIndex(int index, Space space)
        {
            var keyframe = GetRotationSafe(index);
            return GetRotationAtKey(keyframe.key, space);
        }

        public Vector3 GetUpAtIndex(int index, Space space)
        {
            var keyframe = GetRotationSafe(index);
            return GetUpAtKey(keyframe.key, space);
        }

        public Vector3 GetRightAtIndex(int index, Space space)
        {
            var keyframe = GetRotationSafe(index);
            return GetRightAtKey(keyframe.key, space);
        }

        public float GetRollAtIndex(int index, Space space)
        {
            var keyframe = GetRotationSafe(index);
            return GetRollAtKey(keyframe.key, space);
        }

        public Vector3 GetScaleAtIndex(int index)
        {
            return GetScaleSafe(index).value;
        }

        public CurveTangentMode GetTangentModeAtIndex(int index)
        {
            return GetPositionSafe(index).tangentMode;
        }

        public float GetDistanceAlongSplineAtIndex(int index)
        {
            int numPoints = m_PositionCurve.count;
            int numSegments = m_Closed ? numPoints : numPoints - 1;

            if ((index >= 0) && (index < numSegments + 1))
            {
                return m_LengthCacheCurve[index * m_CacheStepsPerSegment].value;
            }

            return 0.0f;
        }

        public SplinePoint GetSplinePointAtIndex(int index, Space space)
        {
            return new SplinePoint
            {
                key = pointCount > 0 ? m_PositionCurve[index].key : (float) index,
                position = GetPositionAtIndex(index, space),
                inTangent = GetInTangentAtIndex(index, space),
                outTangent = GetOutTangentAtIndex(index, space),
                rotation = GetRotationAtIndex(index, space),
                scale = GetScaleAtIndex(index),
                tangentMode = GetTangentModeAtIndex(index)
            };
        }

        #endregion

        #region At Key

        public Vector3 GetPositionAtKey(float key, Space space)
        {
            Vector3 position = m_PositionCurve.Evaluate(key);

            if (space == Space.World)
                position = transform.TransformPoint(position);

            return position;
        }

        public Vector3 GetTangentAtKey(float key, Space space)
        {
            Vector3 tangent = m_PositionCurve.EvaluateTangent(key);

            if (space == Space.World)
                tangent = transform.TransformDirection(tangent);

            return tangent;
        }

        public Quaternion GetRotationAtKey(float key, Space space)
        {
            Quaternion curveRotation = m_RotationCurve.Evaluate(key).normalized;
            Vector3 direction = m_PositionCurve.EvaluateTangent(key).normalized;
            Vector3 upVector = curveRotation * m_DefaultUpDirection;
            
            Quaternion rotation = Quaternion.LookRotation(direction, upVector);
            if (space == Space.World)
                rotation = transform.rotation * rotation;

            return rotation;
        }

        public Vector3 GetForwardAtKey(float key, Space space)
        {
            return GetTangentAtKey(key, space).normalized;
        }

        public Vector3 GetUpAtKey(float key, Space space)
        {
            Quaternion rotation = GetRotationAtKey(key, Space.Self);
            Vector3 upVector = rotation * Vector3.up;

            if (space == Space.World)
                upVector = transform.TransformDirection(upVector);

            return upVector;
        }

        public Vector3 GetRightAtKey(float key, Space space)
        {
            Quaternion rotation = GetRotationAtKey(key, Space.Self);
            Vector3 rightVector = rotation * Vector3.right;

            if (space == Space.World)
                rightVector = transform.TransformDirection(rightVector);

            return rightVector;
        }

        public float GetRollAtKey(float key, Space space)
        {
            Quaternion rotation = GetRotationAtKey(key, space);
            return rotation.eulerAngles.z;
        }

        public Vector3 GetScaleAtKey(float key)
        {
            return pointCount < 1 ? Vector3.one : m_ScaleCurve.Evaluate(key);
        }

        #endregion

        #region At Distance

        public float GetDistanceAtSplineInputKey(float key)
        {
            int numPoints = m_PositionCurve.count;
            int numSegments = m_Closed ? numPoints : numPoints - 1;

            if ((key >= 0) && (key < numSegments))
            {
                int reparamPrevIndex = (int)(key * m_CacheStepsPerSegment);
                int reparamNextIndex = reparamPrevIndex + 1;

                float alpha = (key * m_CacheStepsPerSegment) - (float)reparamPrevIndex;

                float prevDistance = m_LengthCacheCurve[reparamPrevIndex].key;
                float nextDistance = m_LengthCacheCurve[reparamNextIndex].key;

                // ReparamTable assumes that distance and input keys have a linear relationship in-between entries.
                float diff = (nextDistance - prevDistance) * alpha;

                return prevDistance + diff;
            }
            else if (key >= numSegments)
            {
                return splineLength;
            }

            return 0.0f;
        }

        public float GetKeyAtDistance(float distance)
        {
            return m_LengthCacheCurve.Evaluate(distance);
        }

        public Vector3 GetPositionAtDistance(float distance, Space space)
        {
            float key = m_LengthCacheCurve.Evaluate(distance);
            return GetPositionAtKey(key, space);
        }

        public Vector3 GetTangentAtDistance(float distance, Space space)
        {
            float key = m_LengthCacheCurve.Evaluate(distance);
            return GetTangentAtKey(key, space);
        }

        public Quaternion GetRotationAtDistance(float distance, Space space)
        {
            float key = m_LengthCacheCurve.Evaluate(distance);
            return GetRotationAtKey(key, space);
        }

        public float GetRollAtDistance(float distance, Space space)
        {
            float key = m_LengthCacheCurve.Evaluate(distance);
            return GetRollAtKey(key, space);
        }

        public Vector3 GetForwardAtDistance(float distance, Space space)
        {
            float key = m_LengthCacheCurve.Evaluate(distance);
            return GetForwardAtKey(key, space);
        }

        public Vector3 GetUpAtDistance(float distance, Space space)
        {
            float key = m_LengthCacheCurve.Evaluate(distance);
            return GetUpAtKey(key, space);
        }

        public Vector3 GetRightAtDistance(float distance, Space space)
        {
            float key = m_LengthCacheCurve.Evaluate(distance);
            return GetRightAtKey(key, space);
        }

        public Vector3 GetScaleAtDistance(float distance)
        {
            float key = m_LengthCacheCurve.Evaluate(distance);
            return GetScaleAtKey(key);
        }

        #endregion

        #region At Time

        public Vector3 GetPositionAtTime(float time, Space space, bool useUniformVelocity = false)
        {
            if (m_Duration == 0.0f)
                return Vector3.zero;

            if (useUniformVelocity)
            {
                return GetPositionAtDistance(time / m_Duration * splineLength, space);
            }
            else
            {
                float timeMultiplier = segmentCount / m_Duration;
                return GetPositionAtKey(time * timeMultiplier, space);
            }
        }

        public Vector3 GetTangentAtTime(float time, Space space, bool useUniformVelocity = false)
        {
            if (m_Duration == 0.0f)
                return Vector3.zero;

            if (useUniformVelocity)
            {
                return GetTangentAtDistance(time / m_Duration * splineLength, space);
            }
            else
            {
                float timeMultiplier = segmentCount / m_Duration;
                return GetTangentAtKey(time * timeMultiplier, space);
            }
        }

        public Quaternion GetRotationAtTime(float time, Space space, bool useUniformVelocity = false)
        {
            if (m_Duration == 0.0f)
                return Quaternion.identity;

            if (useUniformVelocity)
            {
                return GetRotationAtDistance(time / m_Duration * splineLength, space);
            }
            else
            {
                float timeMultiplier = segmentCount / m_Duration;
                return GetRotationAtKey(time * timeMultiplier, space);
            }
        }

        public Vector3 GetForwardAtTime(float time, Space space, bool useUniformVelocity = false)
        {
            if (m_Duration == 0.0f)
                return Vector3.zero;

            if (useUniformVelocity)
            {
                return GetForwardAtDistance(time / m_Duration * splineLength, space);
            }
            else
            {
                float timeMultiplier = segmentCount / m_Duration;
                return GetForwardAtKey(time * timeMultiplier, space);
            }
        }

        public Vector3 GetUpAtTime(float time, Space space, bool useUniformVelocity = false)
        {
            if (m_Duration == 0.0f)
                return Vector3.zero;

            if (useUniformVelocity)
            {
                return GetUpAtDistance(time / m_Duration * splineLength, space);
            }
            else
            {
                float timeMultiplier = segmentCount / m_Duration;
                return GetUpAtKey(time * timeMultiplier, space);
            }
        }

        public Vector3 GetRightAtTime(float time, Space space, bool useUniformVelocity = false)
        {
            if (m_Duration == 0.0f)
                return Vector3.zero;

            if (useUniformVelocity)
            {
                return GetRightAtDistance(time / m_Duration * splineLength, space);
            }
            else
            {
                float timeMultiplier = segmentCount / m_Duration;
                return GetRightAtKey(time * timeMultiplier, space);
            }
        }

        public Vector3 GetScaleAtTime(float time, bool useUniformVelocity = false)
        {
            if (m_Duration == 0.0f)
                return Vector3.one;

            if (useUniformVelocity)
            {
                return GetScaleAtDistance(time / m_Duration * splineLength);
            }
            else
            {
                float timeMultiplier = segmentCount / m_Duration;
                return GetScaleAtKey(time * timeMultiplier);
            }
        }

        #endregion

        #region At Position

        public float FindDistanceClosestToWorldPosition(Vector3 worldPosition)
        {
            float key = FindKeyClosestToWorldPosition(worldPosition);
            return GetDistanceAtSplineInputKey(key);
        }

        public Vector3 FindPositionClosestToWorldPosition(Vector3 worldPosition, Space space)
        {
            float key = FindKeyClosestToWorldPosition(worldPosition);
            return GetPositionAtKey(key, space);
        }

        public Quaternion FindRotationClosestToWorldPosition(Vector3 worldPosition, Space space)
        {
            float key = FindKeyClosestToWorldPosition(worldPosition);
            return GetRotationAtKey(key, space);
        }

        public Vector3 FindForwardClosestToWorldPosition(Vector3 worldPosition, Space space)
        {
            float key = FindKeyClosestToWorldPosition(worldPosition);
            return GetForwardAtKey(key, space);
        }

        public Vector3 FindUpClosestToWorldPosition(Vector3 worldPosition, Space space)
        {
            float key = FindKeyClosestToWorldPosition(worldPosition);
            return GetUpAtKey(key, space);
        }

        public Vector3 FindRightClosestToWorldPosition(Vector3 worldPosition, Space space)
        {
            float key = FindKeyClosestToWorldPosition(worldPosition);
            return GetRightAtKey(key, space);
        }

        public float FindRollClosestToWorldPosition(Vector3 worldPosition, Space space)
        {
            float key = FindKeyClosestToWorldPosition(worldPosition);
            return GetRollAtKey(key, space);
        }

        public Vector3 FindScaleClosestToWorldPosition(Vector3 worldPosition)
        {
            float key = FindKeyClosestToWorldPosition(worldPosition);
            return GetScaleAtKey(key);
        }

        public float FindKeyClosestToWorldPosition(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.TransformPoint(worldPosition);
            return m_PositionCurve.FindNearestKey(localPosition, out float _, out float _);
        }

        #endregion
    }
}
