using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Splines
{
    static class SplineSelection
    {
        public delegate void OnSplineSelectionChanged();
        public static OnSplineSelectionChanged onSplineSelectionChanged;

        [System.Serializable]
        class SelectionState : ScriptableObject
        {
            public int active = -1;
            public int[] indices = new int[0];

            public void Reset()
            {
                active = -1;
                indices = new int[0];
            }
        }

        static SelectionState s_SelectedState;
        static HashSet<int> s_CurrentSet = new HashSet<int>();

        static SplineSelection()
        {
            s_SelectedState = ScriptableObject.CreateInstance<SelectionState>();
            s_SelectedState.hideFlags = HideFlags.HideAndDontSave;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        public static int[] indices
        {
            get => s_SelectedState.indices;
            set
            {
                HashSet<int> newSet = new HashSet<int>(value);
                if (newSet.SetEquals(s_CurrentSet))
                    return;

                RecordUndo();

                s_CurrentSet.Clear();

                if (value != null && value.Length > 0)
                {
                    for (int i = 0; i < value.Length; ++i)
                        s_CurrentSet.Add(value[i]);

                    if (s_SelectedState.active == -1 || !s_CurrentSet.Contains(s_SelectedState.active))
                        s_SelectedState.active = value[0];

                    CopySetToCurrentStateIndices(s_CurrentSet);
                }
                else
                {
                    s_SelectedState.Reset();
                }
            }
        }

        public static int activeIndex
        {
            get => s_SelectedState.active;
            set
            {
                value = Mathf.Max(value, -1);

                if (value == s_SelectedState.active)
                    return;

                RecordUndo();

                bool selectionChanged = false;

                //Not in current selection
                if (value >= 0 && !s_CurrentSet.Contains(value))
                {
                    s_CurrentSet.Add(value);
                    selectionChanged = true;
                }
                //Clear selection
                else if (value < 0 && s_CurrentSet.Contains(value))
                {
                    s_CurrentSet.Clear();
                    selectionChanged = true;
                }

                s_SelectedState.active = value;

                if (selectionChanged)
                    CopySetToCurrentStateIndices(s_CurrentSet);
            }
        }

        public static bool ContainsIndex(int index)
        {
            return s_CurrentSet.Contains(index);
        }

        static void CopySetToCurrentStateIndices(HashSet<int> set)
        {
            s_SelectedState.indices = new int[set.Count];
            set.CopyTo(s_SelectedState.indices);
        }

        static void RecordUndo()
        {
            Undo.RecordObject(s_SelectedState, "Selection Changed");
            onSplineSelectionChanged?.Invoke();
        }

        static void UndoRedoPerformed()
        {
            s_CurrentSet = new HashSet<int>(s_SelectedState.indices);
            onSplineSelectionChanged?.Invoke();
        }
    }
}
