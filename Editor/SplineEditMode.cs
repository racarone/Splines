using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace Splines
{
    static class SplineEditMode
    {
        public static class Command
        {
            public const string selectAll = "SelectAll";
            public const string deselectAll = "DeselectAll";
            public const string invertSelection = "InvertSelection";
            public const string duplicate = "Duplicate";
            public const string extrude = "Extrude";
            public const string delete = "Delete";
            public const string softDelete = "SoftDelete";
            public const string frameSelected = "FrameSelected";
            public const string addPoint = "Add";
            public const string reverse = "Reverse";
        }

        [UserSetting("Scene View", "Show On Selection Only", "The color of the spline in the Scene View.")]
        static readonly Pref<bool> s_ShowOnSelection = new Pref<bool>("editor.splineShowOnSelection", false);

        [UserSetting("Scene View", "Spline Color", "The color of the spline in the Scene View.")]
        static readonly Pref<Color> s_SplineColor = new Pref<Color>("editor.splineColor", new Color(0.7882353f, 0.7843137f, 0.5647059f, 0.9f));

        [UserSetting("Scene View", "Spline Color Selected", "The selection color of the spline in the Scene View.")]
        static readonly Pref<Color> s_SplineColorSelected = new Pref<Color>("editor.splineColorSelected", new Color(0.9f, 0.9f, 0.8f, 0.9f));

        [UserSetting("Scene View", "Spline Thickness", "The thickness of the spline in the Scene View.")]
        static readonly Pref<float> s_SplineThickness = new Pref<float>("editor.splineThickness", 1.0f);

        [UserSetting("Scene View", "Spline Thickness Selected", "The thickness of the spline while selected in the Scene View.")]
        static readonly Pref<float> s_SplineThicknessSelected = new Pref<float>("editor.splineThicknessSelected", 2.0f);

        [UserSetting("Scene View", "Normal Color", "The color of the spline normals in the Scene View.")]
        static readonly Pref<Color> s_NormalColor = new Pref<Color>("editor.splineNormalColor", new Color(0.227451f, 0.8503067f, 0.972549f, 0.9f));

        [UserSetting("Scene View", "Normal Length", "The length of the spline normals in the Scene View.")]
        static readonly Pref<float> s_NormalScreenSize = new Pref<float>("editor.splineNormalScreenSize", 0.2f);

        [UserSetting("Scene View", "Normal Thickness", "The thickness of the spline normals in the Scene View.")]
        static readonly Pref<float> s_NormalThickness = new Pref<float>("editor.splineNormalThickness", 1.0f);

        [UserSetting("Scene View", "Bounds Color", "The color of the spline bounds in the Scene View.")]
        internal static readonly Pref<Color> s_BoundsColor = new Pref<Color>("editor.boundsColor", new Color(1f, 0.9000457f, 0.4941176f, 0.9f));

        static Spline s_ActiveEditSpline;
        static bool s_NeedsRepaint;

        public static EditMode.SceneViewEditMode GetSceneViewEditMode()
        {
            return EditMode.SceneViewEditMode.LightProbeGroup;
        }

        public static bool EnterEditMode()
        {
            if (Selection.count > 1 || !Selection.activeGameObject)
                return false;

            if (!Selection.activeGameObject.TryGetComponent(out Spline spline))
            {
                if (!Selection.activeGameObject.TryGetComponent(out SplineEvaluator evaluator))
                    return false;

                spline = evaluator.spline;
            }

            if (!spline || spline == s_ActiveEditSpline)
                return false;
            
            s_ActiveEditSpline = spline;
            s_NeedsRepaint = true;
            Tools.current = Tool.Move;
            SplineSelection.indices = new int[0];
            EditMode.ChangeEditMode(GetSceneViewEditMode(), GetBounds(spline, 1f), null);
            return true;
        }

        public static void ExitEditMode()
        {
            if (!s_ActiveEditSpline)
                return;
            
            EditMode.ChangeEditMode(EditMode.SceneViewEditMode.None, GetBounds(s_ActiveEditSpline, 1f), null);
            s_NeedsRepaint = true;
            s_ActiveEditSpline = null;
            Tools.hidden = false;
            SplineSelection.indices = new int[0];
        }

        public static void ToggleEditMode()
        {
            if (!s_ActiveEditSpline)
            {
                EnterEditMode();
            }
            else
            {
                ExitEditMode();
            }
        }

        public static bool IsEditing()
        {
            return s_ActiveEditSpline;
        }

        public static void OnSceneGUI(Spline spline, Editor owner)
        {
            if (!spline)
                return;
            
            HandleGlobalCommands(spline, owner);

            if (s_ActiveEditSpline)
            {
                Tools.hidden = true;
                HandleEditCommands(s_ActiveEditSpline, owner);
                DrawSpline(s_ActiveEditSpline);
                SplineRectSelection.OnSceneGUI(s_ActiveEditSpline, owner);
                SplineHandles.DrawHandles(s_ActiveEditSpline, owner);
            }
            else
            {
                Tools.hidden = false;
            }

            if (s_NeedsRepaint)
            {
                s_NeedsRepaint = false;
                owner.Repaint();
            }
        }

        public static void SendCommand(string command)
        {
            EditorWindow.FocusWindowIfItsOpen<SceneView>();
            EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent(command));
        }

        static readonly List<Vector3> s_Lines = new List<Vector3>();
        
        public static void DrawSpline(Spline spline, GizmoType gizmoType = GizmoType.Selected)
        {
            if (!spline)
                return;

            bool selected = gizmoType.HasFlag(GizmoType.Selected) || gizmoType.HasFlag(GizmoType.InSelectionHierarchy);
            if (s_ShowOnSelection && !selected)
                return;

            var prevZTest = Handles.zTest;

            const int gizmoStepsPerSegment = 20;

            s_Lines.Clear();
            for (int i = 0; i < spline.pointCount; ++i)
            {
                for (int segmentStep = 0; segmentStep < gizmoStepsPerSegment; ++segmentStep)
                {
                    float k0 = i + ((segmentStep + 0f) / gizmoStepsPerSegment);
                    float k1 = i + ((segmentStep + 1f) / gizmoStepsPerSegment);
                    Vector3 a = spline.GetPositionAtKey(k0, Space.World);
                    Vector3 b = spline.GetPositionAtKey(k1, Space.World);
                    s_Lines.Add(a);
                    s_Lines.Add(b);
                }
            }

            if (spline.closed)
            {
                s_Lines.Add(s_Lines[s_Lines.Count-1]);
                s_Lines.Add(s_Lines[0]);
            }

            float thickness = selected ? s_SplineThicknessSelected : s_SplineThickness;
            Color drawColor = selected ? s_SplineColorSelected : s_SplineColor;
            Color drawBehindColor = drawColor;
            drawBehindColor.a *= 0.5f;
            
            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = drawColor;
            for (int i = 0; i < s_Lines.Count - 2; i += 2)
            {
                Handles.DrawLine(s_Lines[i], s_Lines[i+1], thickness);
            }

            Handles.zTest = CompareFunction.Greater;
            Handles.color = drawBehindColor;
            for (int i = 0; i < s_Lines.Count - 2; i += 2)
            {
                Handles.DrawLine(s_Lines[i], s_Lines[i+1], thickness);
            }
            
            // Only draw normals while editing
            if (!IsEditing())
                return;
            
            float normalLength = s_NormalScreenSize;

            s_Lines.Clear();
            for (int i = 0; i < spline.pointCount; ++i)
            {
                for (int segmentStep = 0; segmentStep < gizmoStepsPerSegment; ++segmentStep)
                {
                    float k = i + ((float)segmentStep / gizmoStepsPerSegment);
                    Vector3 p = spline.GetPositionAtKey(k, Space.World);
                    float s = HandleUtility.GetHandleSize(p) * normalLength;
                    Vector3 u = spline.GetUpAtKey(k, Space.World);
                    s_Lines.Add(p);
                    s_Lines.Add(p + u * s);
                }
            }

            thickness = s_NormalThickness;
            drawColor = s_NormalColor;
            drawBehindColor = drawColor;
            drawBehindColor.a *= 0.5f;
            
            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = drawColor;
            for (int i = 0; i < s_Lines.Count - 2; i += 2)
            {
                Handles.DrawLine(s_Lines[i], s_Lines[i+1], thickness);
            }
            
            Handles.zTest = CompareFunction.Greater;
            Handles.color = drawBehindColor;
            for (int i = 0; i < s_Lines.Count - 2; i += 2)
            {
                Handles.DrawLine(s_Lines[i], s_Lines[i+1], thickness);
            }

            Handles.zTest = prevZTest;
        }

        static void HandleGlobalCommands(Spline spline, Editor owner)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.ExecuteCommand:
                case EventType.ValidateCommand:
                    if (Event.current.commandName == Command.frameSelected)
                    {
                        if (evt.type == EventType.ExecuteCommand)
                        {
                            SceneView.currentDrawingSceneView.Frame(GetBounds(spline, 10f), false);
                        }
                        
                        evt.Use();
                    }

                    break;
            }
        }

        static void HandleEditCommands(Spline spline, Editor owner)
        {
            Event evt = Event.current;
            var selectedIndices = SplineSelection.indices;

            switch (evt.type)
            {
                case EventType.ExecuteCommand:
                case EventType.ValidateCommand:
                    bool execute = evt.type == EventType.ExecuteCommand;
                    switch (Event.current.commandName)
                    {
                        case Command.delete:
                        case Command.softDelete:
                        {
                            if (execute) 
                                DestroySelected(spline);
                            
                            evt.Use();
                            break;
                        }
                        case Command.duplicate:
                        {
                            if (execute) 
                                DuplicateSelected(spline);
                            
                            evt.Use();
                            break;
                        }
                        case Command.selectAll:
                        {
                            if (execute)
                            {
                                if (SplineSelection.indices.Length == spline.pointCount)
                                {
                                    ClearSelection();
                                }
                                else
                                {
                                    SelectAll(spline);
                                }
                            }

                            evt.Use();
                            break;
                        }
                        case Command.deselectAll:
                        {
                            if (execute) 
                                ClearSelection();
                            
                            evt.Use();
                            break;
                        }
                        case Command.invertSelection:
                        {
                            if (execute) 
                                InvertSelection(spline);
                            
                            evt.Use();
                            break;
                        }
                    }
                    break;
                case EventType.KeyUp:
                    if (evt.keyCode == KeyCode.Escape)
                    {
                        if (selectedIndices.Length > 0)
                        {
                            ClearSelection();
                            evt.Use();
                        }
                        else
                        {
                            ExitEditMode();
                            evt.Use();
                        }
                    }
                    break;
            }
        }

        static void DestroySelected(Spline spline)
        {
            Undo.RegisterCompleteObjectUndo(spline, "Spline Delete Selected");

            var selectedIndices = SplineSelection.indices;
            for (int i = 0; i < selectedIndices.Length; ++i)
                spline.RemovePointAtIndex(selectedIndices[i] - i);

            ClearSelection();
        }

        static void DuplicateSelected(Spline spline)
        {
            Undo.RegisterCompleteObjectUndo(spline, "Spline Duplicate Selected");

            var lastIndex = spline.pointCount - 1;
            int activeIndex = SplineSelection.activeIndex == -1 ? lastIndex : SplineSelection.activeIndex;
            int insertIndex = activeIndex + 1;
            
            if (activeIndex == -1)
            {
                var position = spline.GetPositionAtIndex(lastIndex, Space.World);
                var direction = spline.GetForwardAtIndex(lastIndex, Space.World);
                var tangent = spline.GetTangentAtIndex(lastIndex, Space.Self);
                spline.AddPoint(position + direction * tangent.magnitude * 0.5f, Space.World);
            }
            else if (activeIndex == 0)
            {
                var position = spline.GetPositionAtIndex(0, Space.World);
                var direction = -spline.GetForwardAtIndex(0, Space.World);
                var tangent = spline.GetTangentAtIndex(0, Space.Self);
                spline.InsertPointAtIndex(0, position + direction * tangent.magnitude * 0.5f, Space.World);
            }
            else
            {
                Vector3 insertPosition = insertIndex < lastIndex
                    ? spline.GetPositionAtKey(activeIndex + 0.5f, Space.Self)
                    : spline.GetPositionAtIndex(insertIndex, Space.Self);

                if (!spline.closed && activeIndex == lastIndex)
                {
                    insertPosition += spline.GetTangentAtIndex(activeIndex, Space.Self);
                }

                spline.InsertPointAtIndex(insertIndex, insertPosition, Space.Self);
            }

            SplineSelection.indices = new[] {Mathf.Clamp(insertIndex, 0, spline.pointCount-1)};
            GUI.changed = true;
        }

        static void SelectAll(Spline spline)
        {
            var allSelection = new int[spline.pointCount];
            for (int i = 0; i < allSelection.Length; ++i)
                allSelection[i] = i;

            SplineSelection.indices = allSelection;
        }

        static void ClearSelection()
        {
            SplineSelection.indices = new int[0];
        }

        static void InvertSelection(Spline spline)
        {
            var existingSelection = SplineSelection.indices;
            var allSelection = new int[spline.pointCount];
            for (int i = 0; i < allSelection.Length; ++i)
                allSelection[i] = i;

            var inverseSet = new HashSet<int>(allSelection);
            inverseSet.ExceptWith(existingSelection);

            var inverseSelection = new int[inverseSet.Count];
            inverseSet.CopyTo(inverseSelection);
            SplineSelection.indices = inverseSelection;
        }

        static Bounds GetBounds(Spline spline, float zoomDistance)
        {
            Bounds selectionBounds;

            if (SplineSelection.activeIndex != -1)
            {
                selectionBounds = new Bounds(spline.GetPositionAtIndex(SplineSelection.activeIndex, Space.World), Vector3.one * zoomDistance);

                var selectedIndices = SplineSelection.indices;
                if (selectedIndices == null)
                    return selectionBounds;
                
                foreach (int index in selectedIndices)
                    selectionBounds.Encapsulate(spline.GetPositionAtIndex(index, Space.World));
            }
            else if (spline.pointCount > 0)
            {
                selectionBounds = new Bounds(spline.GetPositionAtIndex(0, Space.World), Vector3.one * zoomDistance);
                for (int index = 0; index < spline.pointCount; ++index)
                    selectionBounds.Encapsulate(spline.GetPositionAtIndex(index, Space.World));
            }
            else
            {
                selectionBounds = new Bounds(spline.transform.position, Vector3.one * zoomDistance);
            }

            return selectionBounds;
        }
    }
}
