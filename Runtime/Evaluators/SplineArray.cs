using System;
using System.Collections.Generic;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Splines
{
    [AddComponentMenu("Splines/Spline Array")]
    public class SplineArray : SplineEvaluator
    {
        [SerializeField]
        List<GameObject> m_Prefabs = new List<GameObject>();
        public List<GameObject> prefabs => m_Prefabs;
        
        [SerializeField, Min(1)]
        int m_Seed = 1;
        public int seed
        {
            get => m_Seed;
            set
            {
                m_Seed = Mathf.Max(1, value);
                SetNeedsRebuild();
            }
        }

        [SerializeField, Min(1)]
        int m_Count = 1;
        public int count
        {
            get => m_Count;
            set
            {
                m_Count = Mathf.Max(1, value);
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        bool m_UseDistance;
        public bool useDistance
        {
            get => m_UseDistance;
            set
            { 
                m_UseDistance = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField, Min(0.001f)]
        float m_Distance = 2f;
        public float distance
        {
            get => m_Distance;
            set
            {
                m_Distance = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        bool m_UseSplineOrientation = true;
        public bool ueSplineOrientation
        {
            get => m_UseSplineOrientation;
            set
            {
                m_UseSplineOrientation = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector3 m_InitialRotation;
        public Vector3 initialRotation
        {
            get => m_InitialRotation;
            set
            {
                m_InitialRotation = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        bool m_RandomizeRotation;
        public bool randomizeRotation
        {
            get => m_RandomizeRotation;
            set
            {
                m_RandomizeRotation = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector2 m_RandomRotationRangeX;
        public Vector2 randomRotationRangeX
        {
            get => m_RandomRotationRangeX;
            set
            {
                m_RandomRotationRangeX = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector2 m_RandomRotationRangeY = new Vector2(-180f, 180f);
        public Vector2 randomRotationRangeY
        {
            get => m_RandomRotationRangeY;
            set
            {
                m_RandomRotationRangeY = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector2 m_RandomRotationRangeZ;
        public Vector2 randomRotationRangeZ
        {
            get => m_RandomRotationRangeZ;
            set
            {
                m_RandomRotationRangeZ = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector3 m_InitialOffset;
        public Vector3 initialOffset
        {
            get => m_InitialOffset;
            set
            {
                m_InitialOffset = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        bool m_RandomizeOffset;
        public bool randomizeOffset
        {
            get => m_RandomizeOffset;
            set
            {
                m_RandomizeOffset = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector2 m_RandomOffsetRangeX;
        public Vector2 randomOffsetRangeX
        {
            get => m_RandomOffsetRangeX;
            set
            {
                m_RandomOffsetRangeX = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector2 m_RandomOffsetRangeY;
        public Vector2 randomOffsetRangeY
        {
            get => m_RandomOffsetRangeY;
            set
            {
                m_RandomOffsetRangeY = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector2 m_RandomOffsetRangeZ;
        public Vector2 randomOffsetRangeZ
        {
            get => m_RandomOffsetRangeZ;
            set
            {
                m_RandomOffsetRangeZ = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        bool m_UniformScale = true;
        public bool uniformScale
        {
            get => m_UniformScale;
            set
            {
                m_UniformScale = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        bool m_UseSplineScale = true;
        public bool useSplineScale
        {
            get => m_UseSplineScale;
            set
            {
                m_UseSplineScale = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector3 m_InitialScale = Vector3.one;
        public Vector3 initialScale
        {
            get => m_InitialScale;
            set
            {
                m_InitialScale = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        bool m_RandomizeScale;
        public bool randomizeScale
        {
            get => m_RandomizeScale;
            set
            {
                m_RandomizeScale = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector2 m_RandomScaleRangeX = new Vector2(0.1f, 1f);
        public Vector2 randomScaleRangeX
        {
            get => m_RandomScaleRangeX;
            set
            {
                m_RandomScaleRangeX = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector2 m_RandomScaleRangeY = new Vector2(0.1f, 1f);
        public Vector2 randomScaleRangeY
        {
            get => m_RandomScaleRangeY;
            set
            {
                m_RandomScaleRangeY = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector2 m_RandomScaleRangeZ = new Vector2(0.1f, 1f);
        public Vector2 randomScaleZ
        {
            get => m_RandomScaleRangeZ;
            set
            {
                m_RandomScaleRangeZ = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        bool m_ProjectOnGround;
        public bool projectOnGround
        {
            get => m_ProjectOnGround;
            set
            {
                m_ProjectOnGround = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        float m_ProjectFromDistance = 100f;
        public float projectFromDistance
        {
            get => m_ProjectFromDistance;
            set
            {
                m_ProjectFromDistance = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        LayerMask m_GroundLayerMask = -1;
        public LayerMask groundLayerMask
        {
            get => m_GroundLayerMask;
            set
            {
                m_GroundLayerMask = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        bool m_RotateToGroundNormal;
        public bool rotateToGroundNormal
        {
            get => m_RotateToGroundNormal;
            set
            {
                m_RotateToGroundNormal = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField, HideInInspector]
        List<GameObject> m_Instances = new List<GameObject>();

        void OnDestroy()
        {
            if (m_Instances.Count > 0)
            {
                foreach (var go in m_Instances)
                    ObjectUtility.Destroy(go);
                    
                m_Instances.Clear();
            }
        }

        protected override void Build()
        {
            if (m_Prefabs.Count == 0)
                return;

            int count;
            float distanceStep;

            if (m_UseDistance)
            {
                count = Mathf.Max(Mathf.FloorToInt(clipSpan * spline.splineLength / m_Distance), 1);
                distanceStep = m_Distance;
            }
            else
            {
                count = Mathf.Max(m_Count, 1);
                distanceStep = clipSpan * spline.splineLength / Math.Max(2, (count - 1));
            }

            foreach (var go in m_Instances)
                ObjectUtility.Destroy(go);
            
            m_Instances.Clear();

            var random = new Random((uint)m_Seed);
            GetClipDistanceMinMax(out var minDistance, out var maxDistance);

            for (int i = 0; i < count; ++i)
            {
                float step = i * distanceStep;
                float d = Mathf.Min(minDistance + step, maxDistance);

                int randomPrefabIndex = Mathf.Min(random.NextInt(0, m_Prefabs.Count), m_Prefabs.Count - 1);
                var prefab = m_Prefabs[randomPrefabIndex];
                if (prefab == null)
                    continue;

                GameObject newInstance = null;
#if UNITY_EDITOR
                if (UnityEditor.PrefabUtility.GetPrefabAssetType(prefab) == UnityEditor.PrefabAssetType.NotAPrefab ||
                    UnityEditor.PrefabUtility.IsPartOfNonAssetPrefabInstance(prefab))
                    newInstance = Instantiate(prefab, transform);
                else
                    newInstance = (GameObject) UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
#else
                    newInstance = Instantiate(prefab, transform);
#endif

                newInstance.hideFlags = HideFlags.NotEditable | HideFlags.HideInHierarchy;

                m_Instances.Add(newInstance);

                Quaternion splineRotation = m_UseSplineOrientation ? spline.GetRotationAtDistance(d, Space.World) : Quaternion.identity;

                Transform instanceTransform = m_Instances[i].transform;
                instanceTransform.position = spline.GetPositionAtDistance(d, Space.World);
                instanceTransform.rotation = splineRotation;

                if (m_ProjectOnGround)
                {
                    var up = splineRotation * Vector3.up;
                    var startOffset = instanceTransform.position + up * m_ProjectFromDistance;

                    if (Physics.Raycast(startOffset, -up, out RaycastHit hit, 2000f, m_GroundLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        instanceTransform.position = hit.point;

                        if (m_RotateToGroundNormal)
                            instanceTransform.rotation *= Quaternion.FromToRotation(instanceTransform.up, hit.normal);
                    }
                }

                Quaternion localRotation = Quaternion.Euler(m_InitialRotation);
                if (m_RandomizeRotation)
                {
                    var randomRotation = Quaternion.Euler(
                        random.NextFloat(m_RandomRotationRangeX.x, m_RandomRotationRangeX.y),
                        random.NextFloat(m_RandomRotationRangeY.x, m_RandomRotationRangeY.y),
                        random.NextFloat(m_RandomRotationRangeZ.x, m_RandomRotationRangeZ.y));
                    localRotation *= randomRotation;
                }

                instanceTransform.localRotation *= localRotation;

                var localOffset = m_InitialOffset;
                if (m_RandomizeOffset)
                {
                    localOffset.x += random.NextFloat(m_RandomOffsetRangeX.x, m_RandomOffsetRangeX.y);
                    localOffset.y += random.NextFloat(m_RandomOffsetRangeY.x, m_RandomOffsetRangeY.y);
                    localOffset.z += random.NextFloat(m_RandomOffsetRangeZ.x, m_RandomOffsetRangeZ.y);
                }

                instanceTransform.localPosition += splineRotation * Vector3.right * localOffset.x;
                instanceTransform.localPosition += splineRotation * Vector3.up * localOffset.y;
                instanceTransform.localPosition += splineRotation * Vector3.forward * localOffset.z;

                var localScale = m_UseSplineScale ? Vector3.Scale(spline.GetScaleAtDistance(d), m_InitialScale) : m_InitialScale;
                if (m_RandomizeScale)
                {
                    localScale.x *= random.NextFloat(m_RandomScaleRangeX.x, m_RandomScaleRangeX.y);
                    localScale.y *= random.NextFloat(m_RandomScaleRangeY.x, m_RandomScaleRangeY.y);
                    localScale.z *= random.NextFloat(m_RandomScaleRangeZ.x, m_RandomScaleRangeZ.y);
                }

                instanceTransform.localScale = m_UniformScale ? new Vector3(localScale.x, localScale.x, localScale.x) : localScale;
            }
        }
    }
}