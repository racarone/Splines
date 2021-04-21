using System;
using UnityEngine;

namespace Splines
{
    [AddComponentMenu("Splines/Spline Transform")]
    public class SplineTransform : SplineEvaluator
    {
        public enum EvaluationMode
        {
            Percent,
            Distance, 
            Time
        }

        [SerializeField]
        EvaluationMode m_EvaluationMode;
        public EvaluationMode evaluationMode
        {
            get => m_EvaluationMode;
            set
            {
                m_EvaluationMode = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField, Range(0f, 1f)]
        float m_Percent;
        public float percent
        {
            get => m_Percent;
            set
            {
                m_Percent = Mathf.Clamp(value, 0.0f, 1.0f);
                SetNeedsRebuild();
            }
        }

        [SerializeField, Min(0)]
        float m_Distance;
        public float distance
        {
            get => m_Distance;
            set
            {
                m_Distance = Mathf.Clamp(value, 0.0f, spline?.splineLength ?? 0.0f);
                SetNeedsRebuild();
            }
        }

        [SerializeField, Min(0)]
        float m_Time;
        public float time
        {
            get => m_Time;
            set
            {
                m_Time = Mathf.Clamp(value, 0.0f, spline?.duration ?? 1.0f);
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector3 m_LocalOffset;
        public Vector3 localOffset
        {
            get => m_LocalOffset;
            set
            {
                m_LocalOffset = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        bool m_UseSplineOrientation = true;
        public bool useSplineOrientation
        {
            get => m_UseSplineOrientation;
            set
            {
                m_UseSplineOrientation = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector3 m_LocalRotation;
        public Vector3 localRotation
        {
            get => m_LocalRotation;
            set
            {
                m_LocalRotation = value;
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
        bool m_UseUniformScale = true;
        public bool useUniformScale
        {
            get => m_UseUniformScale;
            set
            {
                m_UseUniformScale = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector3 m_LocalScale = Vector3.one;
        public Vector3 localScale
        {
            get => m_LocalScale;
            set
            {
                m_LocalScale = value;
                SetNeedsRebuild();
            }
        }

        public float localUniformScale
        {
            get => m_LocalScale.x;
            set
            {
                m_LocalScale = new Vector3(value, value, value);
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
        bool m_RotateToGroundNormal;
        public bool rotateToTerrainNormal
        {
            get => m_RotateToGroundNormal;
            set
            {
                m_RotateToGroundNormal = value;
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

        protected override void Build()
        {
            if (GetComponent<Spline>() != null)
            {
                Debug.LogError("SplineTransform: Cannot transform a spline on itself! Please remove this component from the spline.");
                return;
            }
            
            Quaternion splineRotation = Quaternion.identity;
            Vector3 splinePosition;
            switch (m_EvaluationMode)
            {
                case EvaluationMode.Time:
                    float clippedTime = ClipTime(m_Time);
                    splinePosition = spline.GetPositionAtTime(clippedTime, Space.World);
                    if (m_UseSplineOrientation)
                        splineRotation = spline.GetRotationAtTime(clippedTime, Space.World);
                    break;
                case EvaluationMode.Distance:
                    float clippedDistance = ClipDistance(m_Distance);
                    splinePosition = spline.GetPositionAtDistance(clippedDistance, Space.World);
                    if (m_UseSplineOrientation)
                        splineRotation = spline.GetRotationAtDistance(clippedDistance, Space.World);
                    break;
                default:
                    clippedDistance = ClipPercent(m_Percent) * spline.splineLength;
                    splinePosition = spline.GetPositionAtDistance(clippedDistance, Space.World);
                    if (m_UseSplineOrientation)
                        splineRotation = spline.GetRotationAtDistance(clippedDistance, Space.World);
                    break;
            }

            transform.position = splinePosition;
            transform.rotation = splineRotation;

            transform.localPosition += splineRotation * Vector3.right * m_LocalOffset.x;
            transform.localPosition += splineRotation * Vector3.up * m_LocalOffset.y;
            transform.localPosition += splineRotation * Vector3.forward * m_LocalOffset.z;

            transform.localRotation *= Quaternion.Euler(m_LocalRotation);

            var scale = m_LocalScale;
            if (m_UseSplineScale)
                scale = Vector3.Scale(scale, spline.GetScaleAtDistance(distance));

            transform.localScale = scale;

            if (m_ProjectOnGround)
            {
                var startOffset = transform.position + Vector3.up * m_ProjectFromDistance;

                if (Physics.Raycast(startOffset, -Vector3.up, out RaycastHit hit, Mathf.Infinity, m_GroundLayerMask, QueryTriggerInteraction.Ignore))
                {
                    transform.position = hit.point;

                    if (m_RotateToGroundNormal)
                        transform.rotation *= Quaternion.FromToRotation(transform.up, hit.normal);
                }
            }
        }
    }
}
