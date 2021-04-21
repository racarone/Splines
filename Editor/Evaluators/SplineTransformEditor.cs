using UnityEditor;
using UnityEngine;

namespace Splines
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SplineTransform))]
    class SplineTransformEditor : SplineEvaluatorEditor
    {
        SerializedProperty m_EvaluationMode;
        SerializedProperty m_Percent;
        SerializedProperty m_Distance;
        SerializedProperty m_Time;

        SerializedProperty m_UseSplineOrientation;
        SerializedProperty m_UseSplineScale;
        SerializedProperty m_UseUniformScale;
        SerializedProperty m_LocalRotation;
        SerializedProperty m_LocalOffset;
        SerializedProperty m_LocalScale;

        SerializedProperty m_ProjectOnGround;
        SerializedProperty m_ProjectFromDistance;
        SerializedProperty m_GroundLayerMask;
        SerializedProperty m_RotateToGroundNormal;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_EvaluationMode = serializedObject.FindProperty("m_EvaluationMode");
            m_Percent = serializedObject.FindProperty("m_Percent");
            m_Distance = serializedObject.FindProperty("m_Distance");
            m_Time = serializedObject.FindProperty("m_Time");

            m_UseSplineOrientation = serializedObject.FindProperty("m_UseSplineOrientation");
            m_UseSplineScale = serializedObject.FindProperty("m_UseSplineScale");
            m_UseUniformScale = serializedObject.FindProperty("m_UseUniformScale");
            m_LocalRotation = serializedObject.FindProperty("m_LocalRotation");
            m_LocalOffset = serializedObject.FindProperty("m_LocalOffset");
            m_LocalScale = serializedObject.FindProperty("m_LocalScale");

            m_ProjectOnGround = serializedObject.FindProperty("m_ProjectOnGround");
            m_ProjectFromDistance = serializedObject.FindProperty("m_ProjectFromDistance");
            m_GroundLayerMask = serializedObject.FindProperty("m_GroundLayerMask");
            m_RotateToGroundNormal = serializedObject.FindProperty("m_RotateToGroundNormal");
        }

        protected override void DoInspectorBodyGUI()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                var splineLength = evaluator.spline?.splineLength ?? 1.0f;
                var splineDuration = evaluator.spline?.duration ?? 1.0f;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(m_EvaluationMode, GUIContent.none);

                    var evaluationMode = (SplineTransform.EvaluationMode) m_EvaluationMode.intValue;
                    switch (evaluationMode)
                    {
                        case SplineTransform.EvaluationMode.Distance:
                            EditorGUILayout.Slider(m_Distance, 0.0f, splineLength, GUIContent.none);
                            break;
                        case SplineTransform.EvaluationMode.Time:
                            EditorGUILayout.Slider(m_Time, 0.0f, splineDuration, GUIContent.none);
                            break;
                        default:
                            EditorGUILayout.Slider(m_Percent, 0.0f, 1.0f, GUIContent.none);
                            break;
                    }
                }

                EditorGUILayout.PropertyField(m_UseSplineOrientation);
                EditorGUILayout.PropertyField(m_UseSplineScale);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.PropertyField(m_LocalRotation);
                EditorGUILayout.PropertyField(m_LocalOffset);

                bool linkScale = m_UseUniformScale.boolValue;
                m_LocalScale.vector3Value = SplineEditorUtility.LinkedVector3Field(EditorGUIUtility.TrTextContent("Local Scale"), m_LocalScale.vector3Value, ref linkScale);
                m_UseUniformScale.boolValue = linkScale;
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Projection", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.PropertyField(m_ProjectOnGround);
                EditorGUILayout.PropertyField(m_ProjectFromDistance);
                EditorGUILayout.PropertyField(m_GroundLayerMask);
                EditorGUILayout.PropertyField(m_RotateToGroundNormal);
            }
            EditorGUI.indentLevel--;
        }
    }
}
