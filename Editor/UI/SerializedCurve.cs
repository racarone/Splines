using UnityEditor;

namespace Splines
{
    sealed class SerializedKeyframe
    {
        public readonly SerializedProperty root;
        public readonly SerializedProperty value;
        public readonly SerializedProperty inTangent;
        public readonly SerializedProperty outTangent;
        public readonly SerializedProperty tangentMode;

        public SerializedKeyframe(SerializedProperty root)
        {
            this.root = root;
            value = root.FindPropertyRelative("value");
            inTangent = root.FindPropertyRelative("inTangent");
            outTangent = root.FindPropertyRelative("outTangent");
            tangentMode = root.FindPropertyRelative("tangentMode");
        }
    }

    sealed class SerializedCurve
    {
        public readonly SerializedProperty root;
        public readonly SerializedProperty keyframes;
        public readonly SerializedProperty loop;
        public readonly SerializedProperty loopKeyOffset;

        public SerializedCurve(SerializedProperty root)
        {
            this.root = root;
            keyframes = root.FindPropertyRelative("keyframes");
            loop = root.FindPropertyRelative("loop");
            loopKeyOffset = root.FindPropertyRelative("loopKeyOffset");
        }
    }
}
