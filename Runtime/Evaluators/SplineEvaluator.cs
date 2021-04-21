using UnityEngine;

namespace Splines
{
    public enum SplineClipMode
    {
        None,
        Percent,
        Distance
    }

    public enum SplineRebuildMode
    {
        Update,
        LateUpdate,
        ViaScripting
    }
    
    [ExecuteAlways]
    public abstract class SplineEvaluator : MonoBehaviour
    {
        [SerializeField]
        Spline m_Spline;
        public Spline spline
        {
            get => m_Spline;
            set
            {
                if (value == m_Spline)
                    return;

                if (m_Spline)
                {
                    m_Spline.onUpdated -= SplineUpdated;
                    m_Spline.onPointAdded -= SplinePointAdded;
                    m_Spline.onPointRemoved -= SplinePointRemoved;
                }

                Reset();
                m_Spline = value;

                if (m_Spline)
                {
                    m_Spline.onUpdated += SplineUpdated;
                    m_Spline.onPointAdded += SplinePointAdded;
                    m_Spline.onPointRemoved += SplinePointRemoved;
                }
            }
        }

        [SerializeField]
        SplineRebuildMode m_RebuildMode = SplineRebuildMode.LateUpdate;
        public SplineRebuildMode rebuildMode
        {
            get => m_RebuildMode;
            set => m_RebuildMode = value;
        }
        
        [SerializeField]
        SplineClipMode m_ClipMode;
        public SplineClipMode clipMode
        {
            get => m_ClipMode;
            set
            {
                m_ClipMode = value;
                SetNeedsRebuild();
            }
        }

        [SerializeField]
        Vector2 m_ClipRange = new Vector2(0f, 1f);
        public Vector2 clipRange
        {
            get => m_ClipRange;
            set
            {
                m_ClipRange = value;
                SetNeedsRebuild();
            }
        }

        int m_LastFrameRebuilt;
        bool m_NeedsRebuild;

        #region Unity Events

        protected void OnEnable()
        {
            if (m_Spline)
            {
                m_Spline.onUpdated += SplineUpdated;
                m_Spline.onPointAdded += SplinePointAdded;
                m_Spline.onPointRemoved += SplinePointRemoved;
            }
            else
            {
                spline = GetComponent<Spline>();
                if (!spline)
                    spline = GetComponentInParent<Spline>();
            }
            
            Initialize();
        }

        protected virtual void Initialize() { }

        protected void OnDisable()
        {
            Deinitialize();
            
            if (m_Spline)
            {
                m_Spline.onUpdated -= SplineUpdated;
                m_Spline.onPointAdded -= SplinePointAdded;
                m_Spline.onPointRemoved -= SplinePointRemoved;
            }
        }

        protected virtual void Deinitialize() { }

        protected void Update()
        {
            if (m_RebuildMode == SplineRebuildMode.Update)
                Rebuild();
            
            OnUpdate();
        }

        protected virtual void OnUpdate() { }

        protected void LateUpdate()
        {
            if (m_RebuildMode == SplineRebuildMode.LateUpdate)
                Rebuild();
            
            OnLateUpdate();
        }

        protected virtual void OnLateUpdate() { }

        public void Reset()
        {
            OnReset();
        }

        protected virtual void OnReset() { }
        
        #endregion
        
        
        #region Build

        public bool NeedsRebuild()
        {
            return m_NeedsRebuild;
        }

        public void SetNeedsRebuild()
        {
            if (!m_NeedsRebuild)
            {
                m_NeedsRebuild = true;
                OnSetNeedsRebuild();
            }
        }

        public virtual void OnSetNeedsRebuild() { }

        public void Rebuild()
        {
            DoRebuild(false);
        }

        public void RebuildImmediate()
        {
            DoRebuild(true);
        }

        void DoRebuild(bool immediate)
        {
            if (m_Spline && (immediate || (m_NeedsRebuild && m_LastFrameRebuilt < Time.frameCount)))
            {
                // Only allow rebuilding once per frame
                m_LastFrameRebuilt = Time.frameCount;
                m_NeedsRebuild = false;
                Build();
                PostBuild();
            }
        }

        protected abstract void Build();

        protected virtual void PostBuild() { }
        
        #endregion
        
        
        #region Spline Events

        void SplineUpdated()
        {
            OnSplineUpdated();
            SetNeedsRebuild();
        }

        protected virtual void OnSplineUpdated() { }

        void SplinePointAdded(int index)
        {
            OnSplinePointAdded(index);
            SetNeedsRebuild();
        }

        protected virtual void OnSplinePointAdded(int index) { }

        void SplinePointRemoved(int index)
        {
            OnSplinePointRemoved(index);
            SetNeedsRebuild();
        }

        protected virtual void OnSplinePointRemoved(int index) { }
        
        #endregion
        

        #region Clipping
        
        public float clipSpan
        {
            get
            {
                if (m_ClipMode == SplineClipMode.None) return 1.0f;
                return Mathf.Clamp01(m_ClipRange.y - m_ClipRange.x);
            }
        }

        public float ClipPercent(float percent)
        {
            if (!m_Spline || m_Spline.pointCount == 0)
                return 0.0f;

            percent = m_Spline.closed ? Mathf.Repeat(percent, 1.0f) : Mathf.Clamp(percent, 0.0f, 1.0f);
                
            if (m_ClipMode == SplineClipMode.None)
                return percent;

            return Mathf.Lerp(m_ClipRange.x, m_ClipRange.y, percent);
        }

        public float ClipTime(float time)
        {
            if (!m_Spline) return 0f;
            return ClipPercent(time / m_Spline.duration) * m_Spline.duration;
        }

        public float ClipDistance(float distance)
        {
            if (!m_Spline) return 0f;
            return ClipPercent(distance / m_Spline.splineLength) * m_Spline.splineLength;
        }

        public void GetClipDistanceMinMax(out float minDistance, out float maxDistance)
        {
            if (m_ClipMode != SplineClipMode.None)
            {
                minDistance = m_ClipRange.x * spline.splineLength;
                maxDistance = m_ClipRange.y * spline.splineLength;
            }
            else
            {
                minDistance = 0f;
                maxDistance = spline.splineLength;
            }
        }

        #endregion
    }
}
