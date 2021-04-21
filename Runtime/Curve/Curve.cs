using System;
using System.Linq;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace Splines
{
    public enum CurveTangentMode
    {
        /// The tangent can be freely set by dragging the tangent handle.
        Free,
        /// The tangents are automatically set to make the curve go smoothly through the key.
        Auto,
        /// The tangent points towards the neighboring key.
        Linear,
        /// The curve retains a constant value between two keys.
        Constant,
        /// The tangents are automatically set to make the curve go smoothly through the key.
        ClampedAuto,
    }

    /// Stores the indices of the last two keyframes that were used for evaluation.
    /// Speeds up the performance when evaluating several times within the same interval.
    public struct CurveCache
    {
        /// Index of the key on the left-hand side of the interval.
        public int lhsIndex;
        /// Index of the key on the right-hand side of the interval.
        public int rhsIndex;
        /// True if this segment should loop around to the beginning of the curve.
        public bool loopSegment;
        /// Resets the indices of the cache to the start of the curve.
        public void Reset()
        {
            lhsIndex = rhsIndex = 0;
            loopSegment = false;
        }
    }

    [Serializable]
    public class Curve<T> : ISerializationCallbackReceiver where T : unmanaged
    {
        /// Stores the indices of the last two keyframes that were used for evaluation.
        /// Speeds up the performance when evaluating several times within the same interval.
        [Serializable]
        public struct Keyframe
        {
            /// The time of the curve at this keyframe.
            public float key;
            /// The value of the curve at this keyframe.
            public T value;
            /// The incoming tangent for this key.
            /// It affects the slope of the curve from the previous key to this one.
            public T inTangent;
            /// The outgoing tangent for this key.
            /// It affects the slope of the curve from this key to the next.
            public T outTangent;
            /// Tangent constraints on Keyframe.
            public CurveTangentMode tangentMode;
        }

        /// The data associated with the times.
        public Keyframe[] keyframes;
        /// Determines if the curve is closed.
        public bool loop;
        /// Specify the offset from the last point's time to the corresponding loop point.
        public float loopKeyOffset;

        [NonSerialized]
        internal CurveCache cache;
        [NonSerialized]
        internal int deserializedVersion;
        
        [SerializeField]
        int m_Count;
        [SerializeField]
        int m_SerializedVersion;

        static readonly Keyframe[] s_EmptyKeyframesData = new Keyframe[0];

        public int count => m_Count;

        public int capacity
        {
            get => keyframes.Length;
            set
            {
                if (value == keyframes.Length)
                    return;

                if (value > 0)
                {
                    var dataArray = new Keyframe[value];

                    if (m_Count > 0)
                    {
                        Array.Copy(keyframes, 0, dataArray, 0, m_Count);
                    }

                    keyframes = dataArray;
                }
                else
                {
                    keyframes = s_EmptyKeyframesData;
                }

                ++deserializedVersion;
            }
        }

        public ref Keyframe this[int index] => ref keyframes[index];
        public ref Keyframe last => ref this[m_Count - 1];
        public bool wasUndoRedoPerformed => deserializedVersion != m_SerializedVersion;

        public Curve(int capacity)
        {
            keyframes = capacity == 0 ? s_EmptyKeyframesData : new Keyframe[capacity];
        }

        public Curve() : this(0) { }

        public void OnBeforeSerialize()
        {
            m_SerializedVersion = deserializedVersion;
        }

        public void OnAfterDeserialize()
        {
            deserializedVersion = m_SerializedVersion;
        }

        void EnsureCapacity(int min)
        {
            if (keyframes.Length >= min)
                return;

            int newCapacity = keyframes.Length == 0 ? 4 : keyframes.Length * 2;
            if ((uint) newCapacity > 2146435071U)
                newCapacity = 2146435071;
            if (newCapacity < min)
                newCapacity = min;

            capacity = newCapacity;
            ++deserializedVersion;
        }

        public void Reverse()
        {
            ClearCache();

            var reversedKeyframes = keyframes.Reverse().ToArray();
            for (int i = 0; i < keyframes.Length; ++i)
                reversedKeyframes[i].key = keyframes[i].key;
            keyframes = reversedKeyframes;
        }

        public void SetLoopKey(float loopKey)
        {
            //Can't set a loop key if there are no points
            if (m_Count == 0)
            {
                loop = false;
                return;
            }
            else
            {
                var lastKey = last.key;
                if (loopKey > lastKey)
                {
                    // Calculate loop key offset from the input key of the final point
                    loop = true;
                    loopKeyOffset = loopKey - lastKey;
                }
                else
                {
                    // Specified a loop key lower than the final point; turn off looping.
                    loop = false;
                }
            }

            ++deserializedVersion;
        }

        public void Clear()
        {
            if (m_Count <= 0) return;
            Array.Clear(keyframes, 0, m_Count);
            m_Count = 0;
            cache.Reset();
            ++deserializedVersion;
        }

        public void ClearCache()
        {
            cache.Reset();
        }

        public void ClearLoopKey()
        {
            loop = false;
            ++deserializedVersion;
        }

        public void Add(Keyframe item)
        {
            if (m_Count == keyframes.Length)
                EnsureCapacity(m_Count + 1);

            cache.Reset();
            keyframes[m_Count] = item;
            m_Count++;
            ++deserializedVersion;
        }

        public void Add(float key, T value, T inTangent, T outTangent, CurveTangentMode tangentMode)
        {
            Add(new Keyframe {key = key, value = value, inTangent = inTangent, outTangent = outTangent, tangentMode = tangentMode});
        }

        public void Insert(int index, Keyframe item)
        {
            if (m_Count == keyframes.Length)
                EnsureCapacity(m_Count + 1);

            if (index < m_Count)
            {
                Array.Copy(keyframes, index, keyframes, index + 1, m_Count - index);
            }

            cache.Reset();
            keyframes[index] = item;
            ++m_Count;
            ++deserializedVersion;
        }

        public void Insert(int index, float key, T value, T inTangent, T outTangent, CurveTangentMode tangentMode)
        {
            Insert(index, new Keyframe {key = key, value = value, inTangent = inTangent, outTangent = outTangent, tangentMode = tangentMode});
        }

        public void RemoveAt(int index)
        {
            --m_Count;

            if (index < m_Count)
            {
                Array.Copy(keyframes, index + 1, keyframes, index, m_Count - index);
            }

            cache.Reset();
            keyframes[m_Count] = default;
            ++deserializedVersion;
        }

        public bool TryFindSurroundingKeyframes(float key, out float leftKey, out float rightKey)
        {
            var numPoints = count;

            if (numPoints == 0)
            {
                leftKey = 0;
                rightKey = 0;
                ClearCache();
                return false;
            }

#if UNITY_EDITOR
            if (wasUndoRedoPerformed)
            {
                ClearCache();
            }
#endif
            {
                var lhsKey = keyframes[cache.lhsIndex].key;
                var rhsKey = keyframes[cache.rhsIndex].key;

                // Check if we are in the cached interval
                if (cache.lhsIndex != cache.rhsIndex && key >= lhsKey && key <= rhsKey)
                {
                    leftKey = lhsKey;
                    rightKey = rhsKey;
                    return true;
                }
            }

            // Binary search to find index of lower bound of input value
            int index = FindIndexForKey(key);
            int lastPoint = numPoints - 1;

            // If before the first point, return its value
            if (index == -1)
            {
                cache.loopSegment = false;
                cache.lhsIndex = 0;
                cache.rhsIndex = 0;
                leftKey = keyframes[cache.lhsIndex].key;
                rightKey = keyframes[cache.rhsIndex].key;
                return true;
            }
            // If on or beyond the last point, return its value.
            else if (index == lastPoint)
            {
                if (!loop)
                {
                    cache.loopSegment = false;
                    cache.lhsIndex = lastPoint;
                    cache.rhsIndex = lastPoint;
                    leftKey = keyframes[cache.lhsIndex].key;
                    rightKey = keyframes[cache.rhsIndex].key;
                    return true;
                }
                else if (key >= keyframes[lastPoint].key + loopKeyOffset)
                {
                    cache.loopSegment = false;
                    cache.lhsIndex = 0;
                    cache.rhsIndex = 0;
                    leftKey = keyframes[cache.lhsIndex].key;
                    rightKey = keyframes[cache.rhsIndex].key;
                    return true;
                }
            }

            // Somewhere within curve range - interpolate.
            Debug.Assert(index >= 0 && ((loop && index < numPoints) || (!loop && index < lastPoint)));
            cache.loopSegment = loop && index == lastPoint;
            cache.lhsIndex = index;
            cache.rhsIndex = cache.loopSegment ? 0 : index + 1;
            leftKey = keyframes[cache.lhsIndex].key;
            rightKey = keyframes[cache.rhsIndex].key;
            return true;
        }
        
        public bool TryFindSurroundingKeyframes(float key, out float leftKey, out float rightKey, out float t)
        {
            if (!TryFindSurroundingKeyframes(key, out leftKey, out rightKey))
            {
                t = 0;
                return false;
            }

            var dx = cache.loopSegment ? loopKeyOffset : rightKey - leftKey;
            t = dx != 0.0f ? (key - leftKey) / dx : 0.0f;
            return true;
        }

        public int FindIndexForKey(float key)
        {
            Debug.Assert(m_Count > 0);

            int lastPoint = m_Count - 1;

            if (key < keyframes[0].key)
            {
                return -1;
            }

            if (key >= keyframes[lastPoint].key)
            {
                return lastPoint;
            }

            int minIndex = 0;
            int maxIndex = m_Count;

            while (maxIndex - minIndex > 1)
            {
                int midIndex = (minIndex + maxIndex) >> 1;

                if (keyframes[midIndex].key <= key)
                {
                    minIndex = midIndex;
                }
                else
                {
                    maxIndex = midIndex;
                }
            }

            return minIndex;
        }
    }

    /// Contains overloads and methods required for specific math types (float, Vector3, Quaternion)
    static class CurveImplementations
    {
        public static float Evaluate(this Curve<float> curve, float key, float defaultValue = default)
        {
            if (!curve.TryFindSurroundingKeyframes(key, out float leftKey, out float rightKey))
                return defaultValue;

            var cache = curve.cache;
            if (cache.lhsIndex == cache.rhsIndex)

                return curve.keyframes[cache.lhsIndex].value;
            return curve.Interpolate(key, cache.loopSegment, leftKey, curve[cache.lhsIndex], rightKey, curve[cache.rhsIndex]);
        }

        public static Vector3 Evaluate(this Curve<Vector3> curve, float key, Vector3 defaultValue = default)
        {
            if (!curve.TryFindSurroundingKeyframes(key, out float leftKey, out float rightKey))
                return defaultValue;

            var cache = curve.cache;
            if (cache.lhsIndex == cache.rhsIndex)
                return curve.keyframes[cache.lhsIndex].value;

            return curve.Interpolate(key, cache.loopSegment, leftKey, curve[cache.lhsIndex], rightKey, curve[cache.rhsIndex]);
        }

        public static Quaternion Evaluate(this Curve<Quaternion> curve, float key, Quaternion defaultValue = default)
        {
            if (!curve.TryFindSurroundingKeyframes(key, out float leftKey, out float rightKey))
                return defaultValue;

            var cache = curve.cache;
            if (cache.lhsIndex == cache.rhsIndex)
                return curve.keyframes[cache.lhsIndex].value;

            return curve.Interpolate(key, cache.loopSegment, leftKey, curve[cache.lhsIndex], rightKey, curve[cache.rhsIndex]);
        }

        public static float EvaluateTangent(this Curve<float> curve, float key, float defaultValue = default)
        {
            if (!curve.TryFindSurroundingKeyframes(key, out float leftKey, out float rightKey))
                return defaultValue;

            var cache = curve.cache;
            if (cache.lhsIndex == cache.rhsIndex)
            {
                if (cache.lhsIndex == 0)
                    return curve.keyframes[0].inTangent;
                else
                    return curve.keyframes[cache.lhsIndex].outTangent;
            }

            return curve.InterpolateTangent(key, cache.loopSegment, leftKey, curve[cache.lhsIndex], rightKey, curve[cache.rhsIndex]);
        }

        public static Vector3 EvaluateTangent(this Curve<Vector3> curve, float key, Vector3 defaultValue = default)
        {
            if (!curve.TryFindSurroundingKeyframes(key, out float leftKey, out float rightKey))
                return defaultValue;

            var cache = curve.cache;
            if (cache.lhsIndex == cache.rhsIndex)
            {
                if (cache.lhsIndex == 0)
                    return curve.keyframes[0].inTangent;
                else
                    return curve.keyframes[cache.lhsIndex].outTangent;
            }

            return curve.InterpolateTangent(key, cache.loopSegment, leftKey, curve[cache.lhsIndex], rightKey, curve[cache.rhsIndex]);
        }

        static float Interpolate(
            this Curve<float> curve, float key, bool loopSegment,
            float leftKey, in Curve<float>.Keyframe leftKeyframe,
            float rightKey, in Curve<float>.Keyframe rightKeyframe)
        {
            var dx = loopSegment ? curve.loopKeyOffset : rightKey - leftKey;
            var t = (key - leftKey) / dx;
            Debug.Assert(t >= 0.0f && t <= 1.0f);

            var m0 = leftKeyframe.outTangent * dx;
            var m1 = rightKeyframe.inTangent * dx;

            switch (leftKeyframe.tangentMode)
            {
                case CurveTangentMode.Free:
                case CurveTangentMode.Auto:
                case CurveTangentMode.ClampedAuto:
                    return CurveMath.InterpolatePosition(leftKeyframe.value, m0, m1, rightKeyframe.value, t);
                case CurveTangentMode.Linear:
                    return lerp(leftKeyframe.value, rightKeyframe.value, t);
                case CurveTangentMode.Constant:
                default:
                    return leftKeyframe.value;
            }
        }

        static Vector3 Interpolate(
            this Curve<Vector3> curve, float key, bool loopSegment,
            float leftKey, in Curve<Vector3>.Keyframe leftKeyframe,
            float rightKey, in Curve<Vector3>.Keyframe rightKeyframe)
        {
            var dx = loopSegment ? curve.loopKeyOffset : rightKey - leftKey;
            var t = (key - leftKey) / dx;
            Debug.Assert(t >= 0.0f && t <= 1.0f);

            var m0 = leftKeyframe.outTangent * dx;
            var m1 = rightKeyframe.inTangent * dx;

            switch (leftKeyframe.tangentMode)
            {
                case CurveTangentMode.Free:
                case CurveTangentMode.Auto:
                case CurveTangentMode.ClampedAuto:
                    return CurveMath.InterpolatePosition(leftKeyframe.value, m0, m1, rightKeyframe.value, t);
                case CurveTangentMode.Linear:
                    return lerp(leftKeyframe.value, rightKeyframe.value, t);
                case CurveTangentMode.Constant:
                default:
                    return leftKeyframe.value;
            }
        }

        static Quaternion Interpolate(
            this Curve<Quaternion> curve, float key, bool loopSegment,
            float leftKey, in Curve<Quaternion>.Keyframe leftKeyframe,
            float rightKey, in Curve<Quaternion>.Keyframe rightKeyframe)
        {
            var dx = loopSegment ? curve.loopKeyOffset : rightKey - leftKey;
            var t = (key - leftKey) / dx;
            Debug.Assert(t >= 0.0f && t <= 1.0f);

            var m0 = (quaternion) (((quaternion) leftKeyframe.outTangent).value * dx);
            var m1 = (quaternion) (((quaternion) rightKeyframe.outTangent).value * dx);

            switch (leftKeyframe.tangentMode)
            {
                case CurveTangentMode.Free:
                case CurveTangentMode.Auto:
                case CurveTangentMode.ClampedAuto:
                    return CurveMath.InterpolateRotation(leftKeyframe.value, m0, m1, rightKeyframe.value, t);
                case CurveTangentMode.Linear:
                    return nlerp(leftKeyframe.value, rightKeyframe.value, t);
                case CurveTangentMode.Constant:
                default:
                    return leftKeyframe.value;
            }
        }

        static float InterpolateTangent(
            this Curve<float> curve, float key, bool loopSegment,
            float leftKey, in Curve<float>.Keyframe leftKeyframe,
            float rightKey, in Curve<float>.Keyframe rightKeyframe)
        {
            var dx = loopSegment ? curve.loopKeyOffset : rightKey - leftKey;
            var t = (key - leftKey) / dx;
            Debug.Assert(t >= 0.0f && t <= 1.0f);

            var m0 = leftKeyframe.outTangent * dx;
            var m1 = rightKeyframe.inTangent * dx;

            return CurveMath.InterpolateTangent(leftKeyframe.value, m0, m1, rightKeyframe.value, t);
        }

        static Vector3 InterpolateTangent(
            this Curve<Vector3> curve, float key, bool loopSegment,
            float leftKey, in Curve<Vector3>.Keyframe leftKeyframe,
            float rightKey, in Curve<Vector3>.Keyframe rightKeyframe)
        {
            var dx = loopSegment ? curve.loopKeyOffset : rightKey - leftKey;
            var t = (key - leftKey) / dx;
            Debug.Assert(t >= 0.0f && t <= 1.0f);

            var m0 = leftKeyframe.outTangent * dx;
            var m1 = rightKeyframe.inTangent * dx;

            return CurveMath.InterpolateTangent(leftKeyframe.value, m0, m1, rightKeyframe.value, t);
        }

        public static void ComputeAutoTangents(this Curve<float> curve)
        {
            if (curve.count == 0)
                return;

            var numPoints = curve.count;
            var lastPoint = numPoints - 1;

            for (var thisIndex = 0; thisIndex < numPoints; ++thisIndex)
            {
                var prevIndex = (thisIndex == 0) ? (curve.loop ? lastPoint : 0) : (thisIndex - 1);
                var nextIndex = (thisIndex == lastPoint) ? (curve.loop ? 0 : lastPoint) : (thisIndex + 1);

                ref var thisPoint = ref curve[thisIndex];
                ref var prevPoint = ref curve[prevIndex];
                ref var nextPoint = ref curve[nextIndex];

                if (thisPoint.tangentMode != CurveTangentMode.Auto)
                    continue;

                var prevTime = (curve.loop && thisIndex == 0) ? (thisPoint.key - 1f) : prevPoint.key;
                var nextTime = (curve.loop && thisIndex == lastPoint) ? (thisPoint.key + 1f) : nextPoint.key;

                var dt = max(FLT_MIN_NORMAL, nextTime - prevTime);
                var tangent = CurveMath.ComputeTangent(prevPoint.value, thisPoint.value, nextPoint.value, 0f) / dt;

                thisPoint.inTangent = tangent;
                thisPoint.outTangent = tangent;
            }

            curve.cache.Reset();
            curve.deserializedVersion++;
        }

        public static void ComputeAutoTangents(this Curve<Vector3> curve)
        {
            if (curve.count == 0)
                return;

            var numPoints = curve.count;
            var lastPoint = numPoints - 1;

            for (var thisIndex = 0; thisIndex < numPoints; ++thisIndex)
            {
                var prevIndex = (thisIndex == 0) ? (curve.loop ? lastPoint : 0) : (thisIndex - 1);
                var nextIndex = (thisIndex == lastPoint) ? (curve.loop ? 0 : lastPoint) : (thisIndex + 1);

                ref var thisPoint = ref curve[thisIndex];
                ref var prevPoint = ref curve[prevIndex];
                ref var nextPoint = ref curve[nextIndex];

                if (thisPoint.tangentMode != CurveTangentMode.Auto)
                    continue;

                var prevTime = (curve.loop && thisIndex == 0) ? (thisPoint.key - 1f) : prevPoint.key;
                var nextTime = (curve.loop && thisIndex == lastPoint) ? (thisPoint.key + 1f) : nextPoint.key;

                var dt = max(FLT_MIN_NORMAL, nextTime - prevTime);
                var tangent = CurveMath.ComputeTangent(prevPoint.value, thisPoint.value, nextPoint.value, 0f) / dt;

                thisPoint.inTangent = tangent;
                thisPoint.outTangent = tangent;
            }

            curve.cache.Reset();
            curve.deserializedVersion++;
        }

        public static void ComputeAutoTangents(this Curve<Quaternion> curve)
        {
            if (curve.count == 0)
                return;

            var numPoints = curve.count;
            var lastPoint = numPoints - 1;

            for (var thisIndex = 0; thisIndex < numPoints; ++thisIndex)
            {
                var prevIndex = (thisIndex == 0) ? (curve.loop ? lastPoint : 0) : (thisIndex - 1);
                var nextIndex = (thisIndex == lastPoint) ? (curve.loop ? 0 : lastPoint) : (thisIndex + 1);

                ref var thisPoint = ref curve[thisIndex];
                ref var prevPoint = ref curve[prevIndex];
                ref var nextPoint = ref curve[nextIndex];

                if (thisPoint.tangentMode != CurveTangentMode.Auto)
                    continue;

                var prevTime = (curve.loop && thisIndex == 0) ? (thisPoint.key - 1f) : prevPoint.key;
                var nextTime = (curve.loop && thisIndex == lastPoint) ? (thisPoint.key + 1f) : nextPoint.key;

                var dt = max(FLT_MIN_NORMAL, nextTime - prevTime);
                quaternion tangent = CurveMath.ComputeTangent(prevPoint.value, thisPoint.value, nextPoint.value, 0f).value / dt;

                thisPoint.inTangent = tangent;
                thisPoint.outTangent = tangent;
            }

            curve.cache.Reset();
            curve.deserializedVersion++;
        }
        
        public static float FindNearestKey(this Curve<Vector3> curve, Vector3 localPosition, out float outDistanceSq, out float outSegment)
        {
            int numSegments = curve.loop ? curve.count : curve.count - 1;

            outDistanceSq = -1f;
            outSegment = -1f;

            if (curve.count > 1)
            {
                float bestResult = curve.FindNearestKeyOnSegment(localPosition, 0, out var bestDistanceSq);
                float bestSegment = 0;

                for (int segment = 1; segment < numSegments; ++segment)
                {
                    float localResult = curve.FindNearestKeyOnSegment(localPosition, segment, out var localDistanceSq);
                    if (localDistanceSq < bestDistanceSq)
                    {
                        bestDistanceSq = localDistanceSq;
                        bestResult = localResult;
                        bestSegment = segment;
                    }
                }

                outDistanceSq = bestDistanceSq;
                outSegment = bestSegment;
                return bestResult;
            }

            if (curve.count == 1)
            {
                outDistanceSq = (localPosition - curve[0].value).sqrMagnitude;
                outSegment = 0;
                return curve[0].key;
            }

            return 0.0f;
        }

        static unsafe float FindNearestKeyOnSegment(this Curve<Vector3> curve, float3 localPosition, int ptIdx, out float outSquaredDistance)
        {
            int lastPoint = curve.count - 1;
            int nextPtIdx = (curve.loop && ptIdx == lastPoint) ? 0 : (ptIdx + 1);
            Debug.Assert(ptIdx >= 0 && ((curve.loop && ptIdx < curve.count) || (!curve.loop && ptIdx < lastPoint)));

            float nextInVal = (curve.loop && ptIdx == lastPoint) ? (lastPoint + 1f) : nextPtIdx;

            if (curve[ptIdx].tangentMode == CurveTangentMode.Constant)
            {
                float distance1 = lengthsq((float3) curve[ptIdx].value - localPosition);
                float distance2 = lengthsq((float3) curve[ptIdx].value - localPosition);
                if (distance1 < distance2)
                {
                    outSquaredDistance = distance1;
                    return curve[ptIdx].key;
                }

                outSquaredDistance = distance2;
                return nextInVal;
            }

            float diff = nextInVal - ptIdx;

            if (curve[ptIdx].tangentMode == CurveTangentMode.Linear)
            {
                // Project on line
                float a = dot((float3) curve[ptIdx].value - localPosition, curve[nextPtIdx].value - curve[ptIdx].value);
                float b = (curve[nextPtIdx].value - curve[ptIdx].value).sqrMagnitude;
                float v = clamp(-a / b, 0.0f, 1.0f);
                outSquaredDistance = lengthsq(lerp(curve[ptIdx].value, curve[nextPtIdx].value, v) - localPosition);
                return v * diff + curve[ptIdx].key;
            }

            const int pointsToCheck = 3;
            const int iterationCount = 3;
            const float scale = 0.75f;

            // Newton's methods is repeated 3 times, starting with t = 0, 0.5, 1.
            var valuesT = stackalloc float[pointsToCheck];
            valuesT[0] = 0.0f;
            valuesT[1] = 0.5f;
            valuesT[2] = 1.0f;

            var initialPoints = stackalloc float3[pointsToCheck];
            initialPoints[0] = curve[ptIdx].value;
            initialPoints[1] = CurveMath.InterpolatePosition(curve[ptIdx].value,
                                                             curve[ptIdx].outTangent * diff,
                                                             curve[nextPtIdx].inTangent * diff,
                                                             curve[nextPtIdx].value,
                                                             valuesT[1]);
            initialPoints[2] = curve[nextPtIdx].value;

            var distancesSq = stackalloc float[pointsToCheck];

            for (int point = 0; point < pointsToCheck; ++point)
            {
                // Algorithm explanation: http://permalink.gmane.org/gmane.games.devel.sweng/8285
                float3 foundPoint = initialPoints[point];
                float lastMove = 1.0f;
                for (int iter = 0; iter < iterationCount; ++iter)
                {
                    float3 lastBestTangent = CurveMath.InterpolateTangent(curve[ptIdx].value,
                                                                          curve[ptIdx].outTangent * diff,
                                                                          curve[nextPtIdx].inTangent * diff,
                                                                          curve[nextPtIdx].value,
                                                                          valuesT[point]);

                    float3 delta = (localPosition - foundPoint);
                    float move = (dot(lastBestTangent, delta)) / lengthsq(lastBestTangent);
                    move = clamp(move, -lastMove * scale, lastMove * scale);
                    valuesT[point] += move;
                    valuesT[point] = clamp(valuesT[point], 0.0f, 1.0f);
                    lastMove = abs(move);
                    foundPoint = CurveMath.InterpolatePosition(curve[ptIdx].value,
                                                               curve[ptIdx].outTangent * diff,
                                                               curve[nextPtIdx].inTangent * diff,
                                                               curve[nextPtIdx].value,
                                                               valuesT[point]);
                }

                distancesSq[point] = lengthsq(foundPoint - localPosition);
                valuesT[point] = valuesT[point] * diff + ptIdx;
            }

            if (distancesSq[0] <= distancesSq[1] && distancesSq[0] <= distancesSq[2])
            {
                outSquaredDistance = distancesSq[0];
                return valuesT[0];
            }

            if (distancesSq[1] <= distancesSq[2])
            {
                outSquaredDistance = distancesSq[1];
                return valuesT[1];
            }

            outSquaredDistance = distancesSq[2];
            return valuesT[2];
        }
    }
}