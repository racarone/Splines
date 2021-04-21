using UnityEditor;
using UnityEngine;

namespace Splines
{
    [CustomEditor(typeof(SplineMesh))]
    class SplineMeshEditor : SplineEvaluatorEditor
    {
        SplineMesh m_SplineMesh;
        SerializedProperty m_Mesh;
        SerializedProperty m_MeshInstance;
        SerializedProperty m_ForwardAxis;
        SerializedProperty m_Scale;
        SerializedProperty m_Offset;
        SerializedProperty m_UVScale;
        SerializedProperty m_UVOffset;
        SerializedProperty m_UVRotation;
        SerializedProperty m_SmoothScaleAndRoll;
        SerializedProperty m_AutoSegments;
        SerializedProperty m_AutoSegmentLengthScale;
        SerializedProperty m_SegmentCount;
        SerializedProperty m_SharedMaterials;
        SerializedProperty m_IndexFormat;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_SplineMesh = (SplineMesh) target;
            
            m_Mesh = serializedObject.FindProperty("m_Mesh");
            m_MeshInstance = serializedObject.FindProperty("m_MeshInstance");
            
            m_ForwardAxis = serializedObject.FindProperty("m_ForwardAxis");
            m_Scale = serializedObject.FindProperty("m_Scale");
            m_Offset = serializedObject.FindProperty("m_Offset");
            m_UVScale = serializedObject.FindProperty("m_UVScale");
            m_UVOffset = serializedObject.FindProperty("m_UVOffset");
            m_UVRotation = serializedObject.FindProperty("m_UVRotation");
                
            m_SmoothScaleAndRoll = serializedObject.FindProperty("m_SmoothScaleAndRoll");
            m_AutoSegments = serializedObject.FindProperty("m_AutoSegments");
            m_AutoSegmentLengthScale = serializedObject.FindProperty("m_AutoSegmentLengthScale");
            m_SegmentCount = serializedObject.FindProperty("m_SegmentCount");
            
            m_SharedMaterials = serializedObject.FindProperty("m_SharedMaterials");
            m_IndexFormat = serializedObject.FindProperty("m_IndexFormat");
        }

        protected override void DoInspectorBodyGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var newMesh = (Mesh) EditorGUILayout.ObjectField("Mesh", m_Mesh.objectReferenceValue, typeof(Mesh), false);
                EditorGUILayout.PropertyField(m_ForwardAxis);
                EditorGUILayout.PropertyField(m_Scale);
                EditorGUILayout.PropertyField(m_Offset);
                EditorGUILayout.PropertyField(m_UVScale);
                EditorGUILayout.PropertyField(m_UVOffset);
                EditorGUILayout.PropertyField(m_UVRotation);
                if (scope.changed)
                {
                    m_Mesh.objectReferenceValue = newMesh;
                    m_MeshInstance.objectReferenceValue = SplineMesh.CloneMesh(newMesh);
                    m_SplineMesh.SetNeedsRebuild();
                }
            }
            EditorGUI.indentLevel--;
            
            EditorGUILayout.LabelField("Segments", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_SmoothScaleAndRoll);
            EditorGUILayout.PropertyField(m_AutoSegments);
            if (m_AutoSegments.boolValue)
            {
                EditorGUILayout.PropertyField(m_AutoSegmentLengthScale);
            }
            else
            {
                EditorGUILayout.PropertyField(m_SegmentCount);
            }
            EditorGUI.indentLevel--;
            
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (m_SharedMaterials.arraySize == 0)
            {
                m_SharedMaterials.arraySize++;
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m_SharedMaterials.GetArrayElementAtIndex(0).objectReferenceValue = cube.GetComponent<MeshRenderer>().sharedMaterial;
                DestroyImmediate(cube);
            }
            EditorGUILayout.PropertyField(m_IndexFormat);
            EditorGUILayout.PropertyField(m_SharedMaterials);
            EditorGUI.indentLevel--;
        }
    }
}