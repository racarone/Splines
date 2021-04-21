using UnityEngine;

namespace Splines
{
    using UnityObject = UnityEngine.Object;
    
    public static class ObjectUtility
    {
        /// Destroys a UnityObject safely.
        public static void Destroy(UnityObject obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    UnityObject.Destroy(obj);
                else
                    UnityObject.DestroyImmediate(obj);
#else
                UnityObject.Destroy(obj);
#endif
            }
        }
    }
}