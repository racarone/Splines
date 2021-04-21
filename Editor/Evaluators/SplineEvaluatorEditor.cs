using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Splines
{
    [CustomEditor(typeof(SplineEvaluator)), CanEditMultipleObjects]
    class SplineEvaluatorEditor : Editor
    {
        SerializedProperty m_Spline;
        SerializedProperty m_ClipMode;
        SerializedProperty m_ClipRange;
        SerializedSpline m_SerializedSpline;

        SplineEvaluator m_Evaluator;
        EditorPrefBool m_ExpandSpline;
        EditorPrefBool m_ExpandSplineEvaluator;
        EditorPrefBool m_ExpandSplineEvaluatorBody;

        protected SplineEvaluator evaluator => m_Evaluator;
        protected bool gameObjectHasSpline => evaluator.GetComponent<Spline>() != null;
        protected string GetDisplayTitle() => $"{target.GetType().Name}".Replace("Spline", "");

        static class Styles
        {
            public static readonly GUIContent clipRange = new GUIContent("Clip Range");
        }

        protected virtual void OnEnable()
        {
            m_Evaluator = (SplineEvaluator) target;

            m_Spline = serializedObject.FindProperty("m_Spline");
            m_ClipMode = serializedObject.FindProperty("m_ClipMode");
            m_ClipRange = serializedObject.FindProperty("m_ClipRange");

            m_ExpandSpline = new EditorPrefBool("Splines.SplineEvaluator.Spline", true);
            m_ExpandSplineEvaluator = new EditorPrefBool("Splines.SplineEvaluator.This", true);
            m_ExpandSplineEvaluatorBody = new EditorPrefBool("Splines.SplinUser.Subclass", true);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            if (!gameObjectHasSpline)
            {
                m_ExpandSpline.value = SplineEditorUtility.DrawFoldout(m_ExpandSpline.value, new GUIContent("Spline"), DrawSplineInspector);
            }

            m_ExpandSplineEvaluator.value = SplineEditorUtility.DrawFoldout(m_ExpandSplineEvaluator.value, new GUIContent("Evaluator"), DrawSplineEvaluatorInspector);
            m_ExpandSplineEvaluatorBody.value = SplineEditorUtility.DrawFoldout(m_ExpandSplineEvaluatorBody.value, new GUIContent(GetDisplayTitle()), DrawSplineEvaluatorInspectorBody);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_Evaluator, "Modified Properties");
                m_Evaluator.SetNeedsRebuild();
            }
        }

        protected virtual void DoInspectorBodyGUI()
        {
            DrawDefaultInspector();
        }

        protected virtual void OnSceneGUI()
        {
            m_SerializedSpline?.Update();
            EditorGUI.BeginChangeCheck();

            if (!evaluator.spline)
                return;

            if (!gameObjectHasSpline)
                SplineEditMode.OnSceneGUI(evaluator.spline, this);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_Evaluator, "Spline Updated");
                m_Evaluator.spline.UpdateSpline();
            }
        }

        void DrawSplineInspector()
        {
            using (new GUILayout.VerticalScope(new GUIStyle(EditorStyles.helpBox)))
            {
                EditorGUI.BeginChangeCheck();

                Spline spline = (Spline) EditorGUILayout.ObjectField("Spline", m_Evaluator.spline, typeof(Spline), true);
                if (spline != m_Evaluator.spline)
                {
                    m_Evaluator.spline = spline;
                    m_Spline.objectReferenceValue = spline;
                }

                if (EditorGUI.EndChangeCheck() || m_SerializedSpline == null)
                {
                    m_SerializedSpline = m_Spline.objectReferenceValue != null ? m_SerializedSpline = new SerializedSpline(new SerializedObject(m_Spline.objectReferenceValue)) : null;
                }

                if (m_SerializedSpline != null)
                {
                    SplineEditor.DrawInspector(this, m_Spline.objectReferenceValue as Spline, m_SerializedSpline);
                }
            }
        }

        void DrawSplineEvaluatorInspector()
        {
            using (new GUILayout.VerticalScope(new GUIStyle(EditorStyles.helpBox)))
            {
                EditorGUILayout.PropertyField(m_ClipMode);

                Spline spline = m_Spline.objectReferenceValue as Spline;
                SplineClipMode clipMode = (SplineClipMode) m_ClipMode.enumValueIndex;

                using (new EditorGUI.DisabledScope(!spline || clipMode == SplineClipMode.None))
                {
                    Vector2 clipRange = m_ClipRange.vector2Value;
                    float maxValue = clipMode == SplineClipMode.Distance ? (spline?.splineLength ?? 1f) : 1f;

                    clipRange *= maxValue;
                    clipRange = SplineEditorUtility.MinMaxSlider(m_ClipRange, clipRange, Styles.clipRange, 0f, maxValue);
                    clipRange /= maxValue;

                    m_ClipRange.vector2Value = clipRange;
                }
            }
        }

        void DrawSplineEvaluatorInspectorBody()
        {
            using (new GUILayout.VerticalScope(new GUIStyle(EditorStyles.helpBox)))
            {
                DoInspectorBodyGUI();
            }
        }

        [DrawGizmo(GizmoType.Pickable | GizmoType.Selected | GizmoType.InSelectionHierarchy | GizmoType.Active, typeof(SplineEvaluator))]
        static void DrawSplineUserGizmos(SplineEvaluator splineUser, GizmoType gizmoType)
        {
            SplineEditor.DrawSpline(splineUser.spline, gizmoType);
        }
    }

    public static class EditorExtensionMethods
    {
        public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty property)
        {
            property = property.Copy();
            var nextElement = property.Copy();
            bool hasNextElement = nextElement.NextVisible(false);
            if (!hasNextElement)
            {
                nextElement = null;
            }

            property.NextVisible(true);
            while (true)
            {
                if ((SerializedProperty.EqualContents(property, nextElement)))
                {
                    yield break;
                }

                yield return property;

                bool hasNext = property.NextVisible(false);
                if (!hasNext)
                {
                    break;
                }
            }
        }
    }
}