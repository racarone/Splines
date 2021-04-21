// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Splines
{
    // Adapted from:
    // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/SceneView/RectSelection.cs
    static class SplineRectSelection
    {
        enum SelectionType { Normal, Additive, Subtractive }

        static Vector2 s_SelectStartPoint;
        static Vector2 s_SelectMousePoint;
        static int[] s_SelectionStart;
        static bool s_RectSelecting;
        static HashSet<int> s_LastSelectionSet;
        static int[] s_CurrentSelection;

        static readonly int s_RectSelectionHash = "SplineSelectionRectHash".GetHashCode();
        static readonly GUIStyle s_SelectionRectStyle = new GUIStyle(GUI.skin.GetStyle("selectionRect"));

        public static void OnSceneGUI(Spline spline, Editor owner)
        {
            Event evt = Event.current;

            Handles.BeginGUI();

            Vector2 mousePos = evt.mousePosition;
            int id = GUIUtility.GetControlID(s_RectSelectionHash, FocusType.Passive);

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    if (Tools.current != Tool.View)
                        HandleUtility.AddDefaultControl(id);
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0 && !evt.alt)
                    {
                        GUIUtility.hotControl = id;
                        s_SelectStartPoint = mousePos;
                        s_SelectionStart = SplineSelection.indices;
                        s_RectSelecting = false;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        if (!s_RectSelecting && (mousePos - s_SelectStartPoint).magnitude > 6f)
                        {
                            EditorApplication.modifierKeysChanged += SendCommandsOnModifierKeys;
                            s_RectSelecting = true;
                            s_LastSelectionSet = null;
                            s_CurrentSelection = null;
                        }
                        if (s_RectSelecting)
                        {
                            s_SelectMousePoint = new Vector2(Mathf.Max(mousePos.x, 0), Mathf.Max(mousePos.y, 0));
                            s_CurrentSelection = GetPointIndicesInRect(spline, FromToRect(s_SelectStartPoint, s_SelectMousePoint));

                            bool selectionChanged = false;
                            if (s_LastSelectionSet == null)
                            {
                                s_LastSelectionSet = new HashSet<int>();
                                selectionChanged = true;
                            }
                            selectionChanged |= s_LastSelectionSet.Count != s_CurrentSelection.Length;
                            if (!selectionChanged)
                            {
                                HashSet<int> currentSet = new HashSet<int>(s_CurrentSelection);
                                foreach (int lastIndex in s_LastSelectionSet)
                                {
                                    if (!currentSet.Contains(lastIndex))
                                    {
                                        selectionChanged = true;
                                        break;
                                    }
                                }
                            }
                            if (selectionChanged)
                            {
                                s_LastSelectionSet = new HashSet<int>(s_CurrentSelection);
                                if (evt.shift)
                                    UpdateSelection(s_SelectionStart, s_CurrentSelection, SelectionType.Additive, s_RectSelecting);
                                else if (EditorGUI.actionKey)
                                    UpdateSelection(s_SelectionStart, s_CurrentSelection, SelectionType.Subtractive, s_RectSelecting);
                                else
                                    UpdateSelection(s_SelectionStart, s_CurrentSelection, SelectionType.Normal, s_RectSelecting);
                            }
                        }
                        evt.Use();
                    }
                    break;

                case EventType.Repaint:
                    if (GUIUtility.hotControl == id && s_RectSelecting)
                        s_SelectionRectStyle.Draw(FromToRect(s_SelectStartPoint, s_SelectMousePoint), GUIContent.none, false, false, false, false);
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        if (s_RectSelecting)
                        {
                            EditorApplication.modifierKeysChanged -= SendCommandsOnModifierKeys;
                            s_RectSelecting = false;
                            s_SelectionStart = new int[0];
                        }
                        else
                        {
                            if (evt.shift || EditorGUI.actionKey)
                            {
                                // For shift, we check if EXACTLY the active GO is hovered by mouse and then subtract. Otherwise additive.
                                // For control/cmd, we check if ANY of the selected GO is hovered by mouse and then subtract. Otherwise additive.
                                // Control/cmd takes priority over shift.
                                int hovered = FindClosestPointIndexNearGUIPoint(spline, evt.mousePosition);
                                if (EditorGUI.actionKey ? SplineSelection.ContainsIndex(hovered) : SplineSelection.activeIndex == hovered)
                                    UpdateSelection(s_SelectionStart, hovered, SelectionType.Subtractive, s_RectSelecting);
                                else
                                    UpdateSelection(s_SelectionStart, FindClosestPointIndexNearGUIPoint(spline, evt.mousePosition), SelectionType.Additive, s_RectSelecting);
                            }
                            else // With no modifier keys, we do the "cycle through overlapped" picking logic in SceneViewPicking.cs
                            {
                                int picked = FindClosestPointIndexNearGUIPoint(spline, evt.mousePosition);
                                UpdateSelection(s_SelectionStart, picked, SelectionType.Normal, s_RectSelecting);
                            }
                        }

                        evt.Use();
                    }
                    break;
                case EventType.ExecuteCommand:
                    if (id == GUIUtility.hotControl && evt.commandName == "ModifierKeysChanged")
                    {
                        if (evt.shift)
                            UpdateSelection(s_SelectionStart, s_CurrentSelection, SelectionType.Additive, s_RectSelecting);
                        else if (EditorGUI.actionKey)
                            UpdateSelection(s_SelectionStart, s_CurrentSelection, SelectionType.Subtractive, s_RectSelecting);
                        else
                            UpdateSelection(s_SelectionStart, s_CurrentSelection, SelectionType.Normal, s_RectSelecting);
                        evt.Use();
                    }
                    break;
            }

            Handles.EndGUI();
        }

        static int FindClosestPointIndexNearGUIPoint(Spline splne, Vector2 point)
        {
            int index = -1;
            float closestDistance = float.MaxValue;

            Transform sceneCameraTransform = SceneView.currentDrawingSceneView.camera.transform;
            for (int i = 0; i < splne.pointCount; ++i)
            {
                Vector3 position = splne.GetPositionAtIndex(i, Space.World);
                Vector2 guiPoint = HandleUtility.WorldToGUIPoint(position);

                float distance = (guiPoint - point).magnitude;
                float size = HandleUtility.GetHandleSize(position) * 0.125f;

                if (distance < size && distance < closestDistance)
                {
                    index = i;
                    closestDistance = distance;
                }
            }

            return index;
        }

        static int[] GetPointIndicesInRect(Spline splne, Rect selectionRect)
        {
            Transform sceneCameraTransform = SceneView.currentDrawingSceneView.camera.transform;
            List<int> rectIndices = new List<int>();

            for (int i = 0; i < splne.pointCount; ++i)
            {
                Vector3 position = splne.GetPositionAtIndex(i, Space.World);
                Vector2 guiPoint = HandleUtility.WorldToGUIPoint(position);

                if (selectionRect.Contains(guiPoint))
                {
                    Vector3 local = sceneCameraTransform.InverseTransformPoint(position);
                    if (local.z >= 0f)
                        rectIndices.Add(i);
                }
            }

            return rectIndices.ToArray();
        }

        static void UpdateSelection(int[] existingSelection, int newIndex, SelectionType type, bool isRectSelection)
        {
            int[] indices;
            if (newIndex == -1)
            {
                indices = new int[0];
            }
            else
            {
                indices = new int[1];
                indices[0] = newIndex;
            }
            UpdateSelection(existingSelection, indices, type, isRectSelection);
        }

        static void UpdateSelection(int[] existingSelection, int[] newIndices, SelectionType type, bool isRectSelection)
        {
            int[] newSelection;

            switch (type)
            {
                case SelectionType.Additive:
                    if (newIndices.Length > 0)
                    {
                        newSelection = new int[existingSelection.Length + newIndices.Length];
                        System.Array.Copy(existingSelection, newSelection, existingSelection.Length);
                        for (int i = 0; i < newIndices.Length; i++)
                            newSelection[existingSelection.Length + i] = newIndices[i];

                        if (!isRectSelection)
                            SplineSelection.activeIndex = newIndices[0];
                        else
                            SplineSelection.activeIndex = newSelection[0];

                        SplineSelection.indices = newSelection;
                    }
                    else
                    {
                        SplineSelection.indices = existingSelection;
                    }
                    break;
                case SelectionType.Subtractive:
                    if (newIndices.Length > 0)
                    {
                        //Create set with existing minus new indices
                        HashSet<int> subtractSet = new HashSet<int>(existingSelection);
                        subtractSet.ExceptWith(newIndices);
                        //Copy to array and assign selection
                        newSelection = new int[subtractSet.Count];
                        subtractSet.CopyTo(newSelection);
                        SplineSelection.indices = newSelection;
                    }
                    break;
                case SelectionType.Normal:
                default:
                    SplineSelection.indices = newIndices;
                    break;
            }
        }

        // When rect selecting, we update the selected objects based on which modifier keys are currently held down,
        // so the window needs to repaint.
        static void SendCommandsOnModifierKeys()
        {
            EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("ModifierKeysChanged"));
        }

        // Make a rect from Min Max values and make sure they're positive sizes
        static Rect FromToRect(Vector2 start, Vector2 end)
        {
            Rect r = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
            if (r.width < 0)
            {
                r.x += r.width;
                r.width = -r.width;
            }
            if (r.height < 0)
            {
                r.y += r.height;
                r.height = -r.height;
            }
            return r;
        }
    }
}
