using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Splines
{
    static class SplineEditorUtility
    {
        delegate Vector3 LinkedVector3FieldDelegate(GUIContent label, Vector3 value, ref bool linked, params GUILayoutOption[] options);
        static readonly LinkedVector3FieldDelegate s_LinkedVector3FieldDelegate;

        static SplineEditorUtility()
        {
            var linkedVector3FieldMi = typeof(EditorGUILayout).GetMethod("LinkedVector3Field", BindingFlags.Static | BindingFlags.NonPublic);
            Debug.Assert(linkedVector3FieldMi != null);
            s_LinkedVector3FieldDelegate = linkedVector3FieldMi.CreateDelegate(typeof(LinkedVector3FieldDelegate), null) as LinkedVector3FieldDelegate;
        }

        public static Vector3 LinkedVector3Field(GUIContent label, Vector3 value, ref bool linked, params GUILayoutOption[] options)
        {
            return s_LinkedVector3FieldDelegate(label, value, ref linked, options);
        }

        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            float xMin = rect.xMin;
            
            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;

            if (isBoxed)
            {
                rect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                                   ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                                   : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        public static bool DrawToggleHeaderFoldout(GUIContent title, bool state, ref bool enabled)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 32f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.xMin += 13f;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            var toggleRect = foldoutRect;
            toggleRect.x = foldoutRect.xMax + 4f;

            // Background rect should be full-width
            backgroundRect.xMin = 16f * EditorGUI.indentLevel;
            backgroundRect.xMin = 0;

            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            // Enabled toggle
            enabled = GUI.Toggle(toggleRect, enabled, GUIContent.none, EditorStyles.toggle);

            var e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (toggleRect.Contains(e.mousePosition))
                {
                    enabled = !enabled;
                    e.Use();
                }
                else if (backgroundRect.Contains(e.mousePosition))
                {
                    state = !state;
                    e.Use();
                }
            }

            return state;
        }

        public static bool DrawToggleHeaderFoldout(GUIContent title, bool state, ref bool enabled, float padding)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 32f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.xMin += padding;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            var toggleRect = foldoutRect;
            toggleRect.x = foldoutRect.xMax + 4f;

            // Background rect should be full-width
            backgroundRect.xMin = padding;
            backgroundRect.xMin = 0;

            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            // Enabled toggle
            enabled = GUI.Toggle(toggleRect, enabled, GUIContent.none, EditorStyles.toggle);

            var e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (toggleRect.Contains(e.mousePosition))
                {
                    enabled = !enabled;
                    e.Use();
                }
                else if (backgroundRect.Contains(e.mousePosition))
                {
                    state = !state;
                    e.Use();
                }
            }

            return state;
        }

        public static bool DrawHeaderFoldout(GUIContent title, bool state)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;
            
            // Background rect should be full-width
            backgroundRect.xMin = 0;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;

            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }

            return state;
        }

        public static bool DrawToggleFoldout(SerializedProperty prop, GUIContent title, Action func, bool toggled)
        {
            bool state = prop.isExpanded;
            state = DrawToggleHeaderFoldout(title, state, ref toggled);

            if (state)
            {
                EditorGUI.indentLevel++;
                func?.Invoke();
                --EditorGUI.indentLevel;
            }

            prop.isExpanded = state;

            return toggled;
        }

        public static bool DrawFoldout(bool expanded, GUIContent title, Action func)
        {
            bool state = expanded;
            state = DrawHeaderFoldout(title, state);

            if (state)
            {
                EditorGUI.indentLevel++;
                func?.Invoke();
                EditorGUI.indentLevel--;
            }

            return state;
        }

        public static void DrawFoldout(SerializedProperty prop, GUIContent title, Action func)
        {
            prop.isExpanded = DrawFoldout(prop.isExpanded, title, func);
        }

        public static void MinMaxSlider(SerializedProperty property, string label, float min, float max)
        {
            var v = property.vector2Value;

            // The layout system breaks alignement when mixing inspector fields with custom layouted
            // fields as soon as a scrollbar is needed in the inspector, so we'll do the layout
            // manually instead
            const int kFloatFieldWidth = 50;
            const int kSeparatorWidth = 5;
            
            float indentOffset = EditorGUI.indentLevel * 15f;
            var lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            lineRect.xMin += 4f;
            lineRect.y += 2f;
            
            var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
            var floatFieldLeft = new Rect(labelRect.xMax, lineRect.y, kFloatFieldWidth + indentOffset, lineRect.height);
            var sliderRect = new Rect(floatFieldLeft.xMax + kSeparatorWidth - indentOffset, lineRect.y, lineRect.width - labelRect.width - kFloatFieldWidth * 2 - kSeparatorWidth * 2, lineRect.height);
            var floatFieldRight = new Rect(sliderRect.xMax + kSeparatorWidth - indentOffset, lineRect.y, kFloatFieldWidth + indentOffset, lineRect.height);

            EditorGUI.PrefixLabel(labelRect, EditorGUIUtility.TrTextContent(label));
            v.x = EditorGUI.FloatField(floatFieldLeft, v.x);
            EditorGUI.MinMaxSlider(sliderRect, ref v.x, ref v.y, min, max);
            v.y = EditorGUI.FloatField(floatFieldRight, v.y);

            property.vector2Value = v;
        }

        public static int MinMaxSliderInt(GUIContent label, int value, ref int minValue, ref int maxValue)
        {
            float fieldWidth = EditorGUIUtility.fieldWidth;
            float indentOffset = EditorGUI.indentLevel * 15f;
            Rect totalRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            Rect labelRect = new Rect(totalRect.x, totalRect.y, EditorGUIUtility.labelWidth - indentOffset, totalRect.height);

            Rect sliderRect = new Rect(labelRect.xMax, labelRect.y, totalRect.width - labelRect.width - 2 * fieldWidth - 4, totalRect.height);

            Rect minLabelRect = new Rect(sliderRect.xMax + 4 - indentOffset, labelRect.y, fieldWidth, totalRect.height);
            Rect minRect = new Rect(minLabelRect.xMax, labelRect.y, fieldWidth / 2 + indentOffset, totalRect.height);

            Rect maxRect = new Rect(minRect.xMax - indentOffset, sliderRect.y, fieldWidth / 2 + indentOffset, totalRect.height);

            EditorGUI.PrefixLabel(labelRect, label);
            value = EditorGUI.IntSlider(sliderRect, value, minValue, maxValue);
            EditorGUI.PrefixLabel(minLabelRect, new GUIContent("Range:"));
            minValue = EditorGUI.IntField(minRect, minValue);
            maxValue = EditorGUI.IntField(maxRect, maxValue);

            return value;
        }

        public static float MinMaxSlider(GUIContent label, float value, ref float minValue, ref float maxValue)
        {
            float fieldWidth = EditorGUIUtility.fieldWidth;
            float indentOffset = EditorGUI.indentLevel * 15f;
            Rect totalRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            Rect labelRect = new Rect(totalRect.x, totalRect.y, EditorGUIUtility.labelWidth - indentOffset, totalRect.height);

            Rect sliderRect = new Rect(labelRect.xMax, labelRect.y, totalRect.width - labelRect.width - 2 * fieldWidth - 4, totalRect.height);

            Rect minLabelRect = new Rect(sliderRect.xMax + 4 - indentOffset, labelRect.y, fieldWidth, totalRect.height);
            Rect minRect = new Rect(minLabelRect.xMax, labelRect.y, fieldWidth / 2 + indentOffset, totalRect.height);

            Rect maxRect = new Rect(minRect.xMax - indentOffset, sliderRect.y, fieldWidth / 2 + indentOffset, totalRect.height);

            EditorGUI.PrefixLabel(labelRect, label);
            value = EditorGUI.Slider(sliderRect, value, minValue, maxValue);
            EditorGUI.PrefixLabel(minLabelRect, new GUIContent("Range:"));
            minValue = EditorGUI.FloatField(minRect, minValue);
            maxValue = EditorGUI.FloatField(maxRect, maxValue);

            return value;
        }

        public static Vector2 MinMaxSlider(SerializedProperty property, Vector2 vector, GUIContent content, float minValue, float maxValue)
        {
            using var horizontal = new EditorGUILayout.HorizontalScope();
            using var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, content, property);

            // The layout system breaks alignment when mixing inspector fields with custom layout'd
            // fields as soon as a scrollbar is needed in the inspector, so we'll do the layout
            // manually instead
            const int kFloatFieldWidth = 50;
            const int kSeparatorWidth = 5;
            float indentOffset = EditorGUI.indentLevel * 15f;
            var lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
            var floatFieldLeft = new Rect(labelRect.xMax, lineRect.y, kFloatFieldWidth + indentOffset, lineRect.height);
            var sliderRect = new Rect(floatFieldLeft.xMax + kSeparatorWidth - indentOffset, lineRect.y, lineRect.width - labelRect.width - kFloatFieldWidth * 2 - kSeparatorWidth * 2, lineRect.height);
            var floatFieldRight = new Rect(sliderRect.xMax + kSeparatorWidth - indentOffset, lineRect.y, kFloatFieldWidth + indentOffset, lineRect.height);

            EditorGUI.PrefixLabel(labelRect, propertyScope.content);
            vector.x = EditorGUI.FloatField(floatFieldLeft, vector.x);
            EditorGUI.MinMaxSlider(sliderRect, ref vector.x, ref vector.y, minValue, maxValue);
            vector.y = EditorGUI.FloatField(floatFieldRight, vector.y);

            return vector;
        }
    }
}
