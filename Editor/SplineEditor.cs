using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Splines
{
    [CustomEditor(typeof(Spline))]
    [CanEditMultipleObjects]
    class SplineEditor : Editor
    {
        const string k_EditShorcutId = "Splines/Edit Spline";

        [Shortcut(k_EditShorcutId, typeof(SceneView), KeyCode.Tab, ShortcutModifiers.Shift)]
        public static void ToggleEditMode()
        {
            SplineEditMode.ToggleEditMode();
        }

        static class Styles
        {
            public static readonly GUIContent addPoint = EditorGUIUtility.TrTextContent("Add Point");
            public static readonly GUIContent deleteSelected = EditorGUIUtility.TrTextContent("Delete Selected");
            public static readonly GUIContent recenterPivot = EditorGUIUtility.TrTextContent("Recenter Pivot");
            public static readonly GUIContent reversePoints = EditorGUIUtility.TrTextContent("Reverse Point Order");

            public static readonly GUIStyle multiButtonStyle = (GUIStyle) "Button";

            public static readonly EditMode.SceneViewEditMode[] sceneViewEditModes = new[]
            {
                EditMode.SceneViewEditMode.LightProbeGroup
            };

            public static readonly string baseSceneEditingToolText = "<color=grey>Spline Scene Editing Mode:</color> ";

            public static readonly GUIStyle richTextMiniLabel = new GUIStyle(EditorStyles.miniLabel);

            static Styles()
            {
                richTextMiniLabel.richText = true;
            }
        }

        Spline m_Spline;
        SerializedSpline m_SerializedSpline;

        void OnEnable()
        {
            m_Spline = target as Spline;
            m_SerializedSpline = new SerializedSpline(serializedObject);
            SplineSelection.onSplineSelectionChanged += OnSplineSelectionChanged;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            SplineSelection.onSplineSelectionChanged -= OnSplineSelectionChanged;
            SplineEditMode.ExitEditMode();
            if (m_Spline)
                m_Spline.UpdateSpline();
        }

        void UndoRedoPerformed()
        {
            if (m_Spline)
                m_Spline.UpdateSpline();
        }

        void OnSplineSelectionChanged()
        {
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            m_SerializedSpline.Update();
            EditorGUI.BeginChangeCheck();

            DrawInspector(this, m_Spline, m_SerializedSpline);

            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedSpline.Apply();
                m_Spline.UpdateSpline();
            }
        }

        void OnSceneGUI()
        {
            m_SerializedSpline.Update();
            EditorGUI.BeginChangeCheck();

            SplineEditMode.OnSceneGUI(m_Spline, this);

            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedSpline.Apply();
                m_Spline.UpdateSpline();
            }
        }

        public static void DrawInspector(Editor editor, Spline spline, SerializedSpline serializedSpline)
        {
            if (!editor || !spline || serializedSpline == null)
                return;

            if (editor.targets.Length == 1)
                DrawToolbar(spline, editor);

            EditorGUILayout.PropertyField(serializedSpline.closed);
            EditorGUILayout.PropertyField(serializedSpline.defaultUpDirection);
            EditorGUILayout.PropertyField(serializedSpline.duration);
            EditorGUILayout.PropertyField(serializedSpline.axis);
            EditorGUILayout.PropertyField(serializedSpline.cacheStepsPerSegment);
            
            GUILayout.Space(1);
            
            EditorGUILayout.PropertyField(serializedSpline.showBounds);

            GUILayout.Space(3);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var id = GUIUtility.GetControlID(FocusType.Passive);
                var rect = EditorGUILayout.GetControlRect(true, 18f);
                rect = EditorGUI.PrefixLabel(rect, new GUIContent("<color=grey>Point Count:</color>"), Styles.richTextMiniLabel);
                if (Event.current.type == EventType.Repaint)
                    Styles.richTextMiniLabel.Draw(rect, new GUIContent($"<color=grey>{spline.pointCount}</color>"), id);

                id = GUIUtility.GetControlID(FocusType.Passive);
                rect = EditorGUILayout.GetControlRect(true, 18f);
                rect = EditorGUI.PrefixLabel(rect, new GUIContent("<color=grey>Spline Length:</color>"), Styles.richTextMiniLabel);
                if (Event.current.type == EventType.Repaint)
                    Styles.richTextMiniLabel.Draw(rect, new GUIContent($"<color=grey>{spline.splineLength}</color>"), id);
            }

            GUILayout.Space(1);

            if (SplineEditMode.IsEditing() && SplineSelection.indices.Length > 0)
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    string displayTitle = SplineSelection.indices.Length > 1 ? $"— ({SplineSelection.indices.Length})" : $"Point {SplineSelection.activeIndex}";
                    SplinePointUI.Draw(displayTitle, serializedSpline);
                }
            }

            GUILayout.Space(3);

            using (new EditorGUI.DisabledScope(!SplineEditMode.IsEditing()))
            {
                using (new GUILayout.HorizontalScope())
                {
                    using (new GUILayout.VerticalScope())
                    {
                        if (GUILayout.Button(Styles.addPoint))
                        {
                            Undo.RecordObject(spline, "Add Point");
                            var lastIndex = spline.pointCount - 1;
                            var position = spline.GetPositionAtIndex(lastIndex, Space.World);
                            var direction = spline.GetForwardAtIndex(lastIndex, Space.World);
                            var tangent = spline.GetTangentAtIndex(lastIndex, Space.Self);
                            spline.AddPoint(position + direction * tangent.magnitude, Space.World);
                        }

                        if (GUILayout.Button(Styles.deleteSelected))
                        {
                            SplineEditMode.SendCommand(SplineEditMode.Command.delete);
                        }
                    }

                    using (new GUILayout.VerticalScope())
                    {
                        if (GUILayout.Button(Styles.recenterPivot))
                        {
                            Undo.RecordObjects(new Object[] {spline, spline.transform}, "Recenter Pivot");

                            var bounds = spline.ComputeBounds(Space.Self);

                            for (int i = 0; i < spline.pointCount; ++i)
                            {
                                ref var position = ref spline.positionCurve[i];
                                position.value -= bounds.center;
                            }

                            var minWS = spline.transform.TransformPoint(bounds.min);
                            var maxWS = spline.transform.TransformPoint(bounds.max);
                            var boundsWS = bounds;
                            boundsWS.SetMinMax(minWS, maxWS);

                            spline.transform.position = boundsWS.center;
                            spline.UpdateSpline();
                        }

                        if (GUILayout.Button(Styles.reversePoints))
                        {
                            Undo.RecordObject(spline, "Reverse Points");

                            spline.positionCurve.Reverse();
                            spline.rotationCurve.Reverse();
                            spline.scaleCurve.Reverse();
                            spline.UpdateSpline();
                        }
                    }
                }
            }
        }

        static string GetShorcutString()
        {
            return $"[{ShortcutManager.instance.GetShortcutBinding(k_EditShorcutId)}] - Enters or exits edit mode.\n" +
                   $"[Control + D] - Adds or inserts a point.\n" +
                   $"[Shift + Control + Click] - Inserts a point at the cursor.\n" +
                   $"[Control + A] - Selects all points.";
        }

        static GUIContent[] GetToolContents()
        {
            return new GUIContent[]
            {
                new GUIContent(EditorGUIUtility.IconContent("EditCollider").image,
                               EditorGUIUtility.TrTextContent("Edit Spline.\n\n" + GetShorcutString()).text)
            };
        }

        static GUIContent GetToolNameContent()
        {
            return new GUIContent(Styles.baseSceneEditingToolText + "Points\n\n" + GetShorcutString(), "");
        }

        static void DrawToolbar(Spline spline, Editor editor)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var oldEditMode = EditMode.editMode;
                var selected = SplineEditMode.IsEditing() ? 0 : 1;
                var index = selected;

                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    // EditMode.DoInspectorToolbar(Styles.sceneViewEditModes, Styles.toolContents, this);
                    using (new EditorGUI.DisabledScope(editor.targets.Length > 1))
                        index = GUILayout.Toolbar(selected, GetToolContents(), Styles.multiButtonStyle);

                    if (scope.changed)
                    {
                        EditMode.ChangeEditMode(index == selected ? EditMode.SceneViewEditMode.None : SplineEditMode.GetSceneViewEditMode(), new Bounds(), null);
                    }
                }

                if (oldEditMode != EditMode.editMode)
                {
                    switch (EditMode.editMode)
                    {
                        case EditMode.SceneViewEditMode.LightProbeGroup:
                            SplineEditMode.EnterEditMode();
                            break;
                        default:
                            SplineEditMode.ExitEditMode();
                            break;
                    }
                }

                GUILayout.FlexibleSpace();
            }

            // Info box for tools

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string helpText = Styles.baseSceneEditingToolText;
                if (SplineEditMode.IsEditing())
                {
                    int index = ArrayUtility.IndexOf(Styles.sceneViewEditModes, EditMode.editMode);
                    if (index >= 0)
                        helpText = GetToolNameContent().text;
                }

                GUILayout.Label(helpText, Styles.richTextMiniLabel);
            }

            EditorGUILayout.Space();
        }

        [DrawGizmo(GizmoType.Pickable | GizmoType.NotInSelectionHierarchy | GizmoType.Selected | GizmoType.InSelectionHierarchy | GizmoType.Active, typeof(Spline))]
        static void DrawSplineGizmos(Spline spline, GizmoType gizmoType)
        {
            if (gizmoType.HasFlag(GizmoType.Selected) && spline.showBounds)
            {
                Gizmos.matrix = spline.transform.localToWorldMatrix;
                Gizmos.color = SplineEditMode.s_BoundsColor;
                Bounds bounds = spline.ComputeBounds(Space.Self);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
            
            DrawSpline(spline, gizmoType);
        }

        public static void DrawSpline(Spline spline, GizmoType gizmoType)
        {
            if (!spline || SplineEditMode.IsEditing())
                return;

            SplineEditMode.DrawSpline(spline, gizmoType);
        }
    }
}