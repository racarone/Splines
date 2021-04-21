using UnityEditor;
using UnityEngine;

namespace Splines
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SplineArray))]
    class SplineArrayEditor : SplineEvaluatorEditor
    {
        SerializedProperty m_Seed;
        SerializedProperty m_Count;
        SerializedProperty m_UseDistance;
        SerializedProperty m_Distance;
        SerializedProperty m_Prefabs;

        SerializedProperty m_UseSplineOrientation;
        SerializedProperty m_InitialRotation;
        SerializedProperty m_RandomizeRotation;
        SerializedProperty m_RandomRotationRangeX;
        SerializedProperty m_RandomRotationRangeY;
        SerializedProperty m_RandomRotationRangeZ;

        SerializedProperty m_InitialOffset;
        SerializedProperty m_RandomizeOffset;
        SerializedProperty m_RandomOffsetRangeX;
        SerializedProperty m_RandomOffsetRangeY;
        SerializedProperty m_RandomOffsetRangeZ;

        SerializedProperty m_UniformScale;
        SerializedProperty m_UseSplineScale;
        SerializedProperty m_InitialScale;
        SerializedProperty m_RandomizeScale;
        SerializedProperty m_RandomScaleRangeX;
        SerializedProperty m_RandomScaleRangeY;
        SerializedProperty m_RandomScaleRangeZ;

        SerializedProperty m_ProjectOnGround;
        SerializedProperty m_ProjectFromDistance;
        SerializedProperty m_GroundLayerMask;
        SerializedProperty m_RotateToGroundNormal;

        SerializedProperty m_Instances;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Seed = serializedObject.FindProperty("m_Seed");
            m_Count = serializedObject.FindProperty("m_Count");
            m_UseDistance = serializedObject.FindProperty("m_UseDistance");
            m_Distance = serializedObject.FindProperty("m_Distance");
            m_Prefabs = serializedObject.FindProperty("m_Prefabs");

            m_UseSplineOrientation = serializedObject.FindProperty("m_UseSplineOrientation");
            m_InitialRotation = serializedObject.FindProperty("m_InitialRotation");
            m_RandomizeRotation = serializedObject.FindProperty("m_RandomizeRotation");
            m_RandomRotationRangeX = serializedObject.FindProperty("m_RandomRotationRangeX");
            m_RandomRotationRangeY = serializedObject.FindProperty("m_RandomRotationRangeY");
            m_RandomRotationRangeZ = serializedObject.FindProperty("m_RandomRotationRangeZ");

            m_InitialOffset = serializedObject.FindProperty("m_InitialOffset");
            m_RandomizeOffset = serializedObject.FindProperty("m_RandomizeOffset");
            m_RandomOffsetRangeX = serializedObject.FindProperty("m_RandomOffsetRangeX");
            m_RandomOffsetRangeY = serializedObject.FindProperty("m_RandomOffsetRangeY");
            m_RandomOffsetRangeZ = serializedObject.FindProperty("m_RandomOffsetRangeZ");

            m_UniformScale = serializedObject.FindProperty("m_UniformScale");
            m_UseSplineScale = serializedObject.FindProperty("m_UseSplineScale");
            m_InitialScale = serializedObject.FindProperty("m_InitialScale");
            m_RandomizeScale = serializedObject.FindProperty("m_RandomizeScale");
            m_RandomScaleRangeX = serializedObject.FindProperty("m_RandomScaleRangeX");
            m_RandomScaleRangeY = serializedObject.FindProperty("m_RandomScaleRangeY");
            m_RandomScaleRangeZ = serializedObject.FindProperty("m_RandomScaleRangeZ");

            m_ProjectOnGround = serializedObject.FindProperty("m_ProjectOnGround");
            m_ProjectFromDistance = serializedObject.FindProperty("m_ProjectFromDistance");
            m_GroundLayerMask = serializedObject.FindProperty("m_GroundLayerMask");
            m_RotateToGroundNormal = serializedObject.FindProperty("m_RotateToGroundNormal");

            m_Instances = serializedObject.FindProperty("m_Instances");
        }

        protected override void DoInspectorBodyGUI()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.PropertyField(m_UseDistance);

                if (m_UseDistance.boolValue)
                {
                    EditorGUILayout.DelayedFloatField(m_Distance);
                }
                else
                {
                    EditorGUILayout.PropertyField(m_Count);
                }

                EditorGUILayout.PropertyField(m_Prefabs);
                EditorGUILayout.PropertyField(m_Seed);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.PropertyField(m_InitialRotation);
                EditorGUILayout.PropertyField(m_UseSplineOrientation);
                EditorGUILayout.PropertyField(m_RandomizeRotation);
                if (m_RandomizeRotation.boolValue)
                {
                    EditorGUI.indentLevel++;
                    SplineEditorUtility.MinMaxSlider(m_RandomRotationRangeX, "Random Rotation X", -180f, 180f);
                    SplineEditorUtility.MinMaxSlider(m_RandomRotationRangeY, "Random Rotation Y", -180f, 180f);
                    SplineEditorUtility.MinMaxSlider(m_RandomRotationRangeZ, "Random Rotation Z", -180f, 180f);
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Offset", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.PropertyField(m_InitialOffset);
                EditorGUILayout.PropertyField(m_RandomizeOffset);
                if (m_RandomizeOffset.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_RandomOffsetRangeX);
                    EditorGUILayout.PropertyField(m_RandomOffsetRangeY);
                    EditorGUILayout.PropertyField(m_RandomOffsetRangeZ);
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                bool linkScale = m_UniformScale.boolValue;
                m_InitialScale.vector3Value = SplineEditorUtility.LinkedVector3Field(EditorGUIUtility.TrTextContent("Initial Scale"), m_InitialScale.vector3Value, ref linkScale);
                m_UniformScale.boolValue = linkScale;
                EditorGUILayout.PropertyField(m_UseSplineScale);
                EditorGUILayout.PropertyField(m_RandomizeScale);
                if (m_RandomizeScale.boolValue)
                {
                    EditorGUI.indentLevel++;
                    if (linkScale)
                    {
                        SplineEditorUtility.MinMaxSlider(m_RandomScaleRangeX, "Random Scale", 0.001f, 10f);
                    }
                    else
                    {
                        SplineEditorUtility.MinMaxSlider(m_RandomScaleRangeX, "Random Scale X", 0.001f, 10f);
                        SplineEditorUtility.MinMaxSlider(m_RandomScaleRangeY, "Random Scale Y", 0.001f, 10f);
                        SplineEditorUtility.MinMaxSlider(m_RandomScaleRangeZ, "Random Scale Z", 0.001f, 10f);
                    }
                    EditorGUI.indentLevel--;
                }
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

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }
    }
}
