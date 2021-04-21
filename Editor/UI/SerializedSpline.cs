using UnityEditor;

namespace Splines
{
    sealed class SerializedSpline
    {
        public readonly SerializedObject serializedObject;
        public readonly SerializedCurve positionCurve;
        public readonly SerializedCurve rotationCurve;
        public readonly SerializedCurve scaleCurve;
        public readonly SerializedProperty closed;
        public readonly SerializedProperty defaultUpDirection;
        public readonly SerializedProperty duration;
        public readonly SerializedProperty axis;
        public readonly SerializedProperty cacheStepsPerSegment;
        public readonly SerializedProperty showBounds;

        public SerializedSpline(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            positionCurve = new SerializedCurve(serializedObject.FindProperty("m_PositionCurve"));
            rotationCurve = new SerializedCurve(serializedObject.FindProperty("m_RotationCurve"));
            scaleCurve = new SerializedCurve(serializedObject.FindProperty("m_ScaleCurve"));
            axis = serializedObject.FindProperty("m_Axis");;
            closed = serializedObject.FindProperty("m_Closed");;
            duration = serializedObject.FindProperty("m_Duration");
            cacheStepsPerSegment = serializedObject.FindProperty("m_CacheStepsPerSegment");
            defaultUpDirection = serializedObject.FindProperty("m_DefaultUpDirection");
            showBounds = serializedObject.FindProperty("m_ShowBounds");
        }

        public void Update()
        {
            serializedObject.Update();
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
