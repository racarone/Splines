using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Splines
{
    static class SplinePointUI
    {
        static class Styles
        {
            public static readonly GUIContent position = EditorGUIUtility.TrTextContent("Position", "The local position of this point relative to the spline.");
            public static readonly GUIContent tangent = EditorGUIUtility.TrTextContent("Tangent", "The local tangent of this point relative to the spline");
            public static readonly GUIContent inTangent = EditorGUIUtility.TrTextContent("In Tangent", "The local in tangent of this point relative to the spline");
            public static readonly GUIContent outTangent = EditorGUIUtility.TrTextContent("Out Tangent", "The local out tangent of this point relative to the spline");
            public static readonly GUIContent tangentMode = EditorGUIUtility.TrTextContent("Tangent Mode", "The tangent mode used for this point.");
            public static readonly GUIContent roll = EditorGUIUtility.TrTextContent("Roll",  "The roll of this point relative to the spline.");
            public static readonly GUIContent scale = EditorGUIUtility.TrTextContent("Scale", "The local scaling of this point relative to the spline.");
            public static readonly GUIContent headerIcon = new GUIContent(AssetPreview.GetMiniTypeThumbnail(typeof(Transform)));
        }

        const int k_SpacingSubLabel = 4;
        const int k_Vector3Count = 3;
        const float k_SingleLineHeight = 18f;
        static readonly GUIContent[] s_XYZLabels = {EditorGUIUtility.TrTextContent("X"), EditorGUIUtility.TrTextContent("Y"), EditorGUIUtility.TrTextContent("Z")};

        public static void Draw(string title, SerializedSpline serializedSpline)
        {
            DoTitle(title);
            DoPositionTangent(serializedSpline.positionCurve);
            DoRotation(serializedSpline.rotationCurve);
            DoScale(serializedSpline.scaleCurve);
        }

        static void DoTitle(string header)
        {
            Rect fullRect = EditorGUILayout.GetControlRect(false);
            
            Rect iconRect = new Rect(fullRect.x, fullRect.y + 2, 16, 16);
            EditorGUI.LabelField(iconRect, Styles.headerIcon);
            
            Rect titleRect = new Rect(iconRect.x + 18, fullRect.y, fullRect.width - 16, fullRect.height);
            EditorGUI.LabelField(titleRect, header, EditorStyles.boldLabel);
            
            SplineEditorUtility.DrawSplitter(true);
            GUILayout.Space(1);
        }

        static void DoPositionTangent(SerializedCurve serializedPositionCurve)
        {
            var selectedIndices = SplineSelection.indices;
            if (selectedIndices.Length == 0)
                return;

            int activeSerializePointIndex = 0;
            var serializedSelectedPoints = new SerializedKeyframe[selectedIndices.Length];
            for (int i = 0; i < selectedIndices.Length; ++ i)
            {
                serializedSelectedPoints[i] = new SerializedKeyframe(serializedPositionCurve.keyframes.GetArrayElementAtIndex(selectedIndices[i]));

                if (SplineSelection.activeIndex == selectedIndices[i])
                    activeSerializePointIndex = i;
            }

            var activePoint = serializedSelectedPoints[activeSerializePointIndex];
            PositionField(serializedSelectedPoints, activePoint);
            TangentFields(serializedSelectedPoints, activePoint);
        }

        static void DoRotation(SerializedCurve serializedRollCurve)
        {
            var selectedIndices = SplineSelection.indices;
            if (selectedIndices.Length == 0)
                return;

            int activeSerializePointIndex = 0;
            var serializedSelectedPoints = new SerializedKeyframe[selectedIndices.Length];
            for (int i = 0; i < selectedIndices.Length; ++ i)
            {
                serializedSelectedPoints[i] = new SerializedKeyframe(serializedRollCurve.keyframes.GetArrayElementAtIndex(selectedIndices[i]));

                if (SplineSelection.activeIndex == selectedIndices[i])
                    activeSerializePointIndex = i;
            }

            var activePoint = serializedSelectedPoints[activeSerializePointIndex];

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.showMixedValue = serializedSelectedPoints.Any(point => point.value.quaternionValue != activePoint.value.quaternionValue);
                EditorGUILayout.PropertyField(activePoint.value, Styles.roll);

                if (scope.changed)
                {
                    foreach (SerializedKeyframe otherPoint in serializedSelectedPoints)
                        otherPoint.value.quaternionValue = activePoint.value.quaternionValue;
                }
            }

            EditorGUI.showMixedValue = false;
        }

        static void DoScale(SerializedCurve serializedScaleCurve)
        {
            var selectedIndices = SplineSelection.indices;
            if (selectedIndices.Length == 0)
                return;

            var activeSerializePointIndex = 0;
            var serializedSelectedPoints = new SerializedKeyframe[selectedIndices.Length];
            for (int i = 0; i < selectedIndices.Length; ++ i)
            {
                serializedSelectedPoints[i] = new SerializedKeyframe(serializedScaleCurve.keyframes.GetArrayElementAtIndex(selectedIndices[i]));

                if (SplineSelection.activeIndex == selectedIndices[i])
                    activeSerializePointIndex = i;
            }

            var activePoint = serializedSelectedPoints[activeSerializePointIndex];

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.showMixedValue = serializedSelectedPoints.Any(point => point.value.vector3Value != activePoint.value.vector3Value);
                
                bool linkScale = !activePoint.root.isExpanded;
                activePoint.value.vector3Value = SplineEditorUtility.LinkedVector3Field(Styles.scale, activePoint.value.vector3Value, ref linkScale);
                activePoint.root.isExpanded = linkScale;
                
                if (scope.changed)
                {
                    foreach (SerializedKeyframe otherPoint in serializedSelectedPoints)
                        otherPoint.value.vector3Value = activePoint.value.vector3Value;
                }
            }

            EditorGUI.showMixedValue = false;
        }

        static void PositionField(SerializedKeyframe[] serializedPoints, SerializedKeyframe activePoint)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // In order to correctly support individual mixed values (like the transform inspector), we draw each field seperately...
                EditorGUILayout.PrefixLabel(Styles.position);
                
                float prevLabelWidth = EditorGUIUtility.labelWidth;
                Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
                controlRect.height = k_SingleLineHeight;

                float width = (controlRect.width - (k_Vector3Count-1) * k_SpacingSubLabel) / k_Vector3Count;
                Rect subLabelRect = new Rect(controlRect) {width = width};
                Vector3 position = activePoint.value.vector3Value;

                // X:
                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(s_XYZLabels[0]).x;
                    EditorGUI.showMixedValue = serializedPoints.Any(point => point.value.vector3Value.x != activePoint.value.vector3Value.x);
                    position.x = EditorGUI.FloatField(subLabelRect, s_XYZLabels[0], position.x);
                    subLabelRect.x += width + k_SpacingSubLabel;
                    
                    if (scope.changed)
                    {
                        foreach (SerializedKeyframe otherPoint in serializedPoints)
                        {
                            var v = otherPoint.value.vector3Value;
                            v.x = position.x;
                            otherPoint.value.vector3Value = v;
                        }
                    }
                }
                // Y:
                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(s_XYZLabels[1]).x;
                    EditorGUI.showMixedValue = serializedPoints.Any(p => activePoint.value.vector3Value.y != p.value.vector3Value.y);
                    position.y = EditorGUI.FloatField(subLabelRect, s_XYZLabels[1], position.y);
                    subLabelRect.x += width + k_SpacingSubLabel;
                    
                    if (scope.changed)
                    {
                        foreach (SerializedKeyframe otherPoint in serializedPoints)
                        {
                            var v = otherPoint.value.vector3Value;
                            v.y = position.y;
                            otherPoint.value.vector3Value = v;
                        }
                    }
                }
                // Z:
                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(s_XYZLabels[2]).x;
                    EditorGUI.showMixedValue = serializedPoints.Any(p => activePoint.value.vector3Value.z != p.value.vector3Value.z);
                    position.z = EditorGUI.FloatField(subLabelRect, s_XYZLabels[2], position.z);
                    
                    if (scope.changed)
                    {
                        foreach (SerializedKeyframe otherPoint in serializedPoints)
                        {
                            var v = otherPoint.value.vector3Value;
                            v.z = position.z;
                            otherPoint.value.vector3Value = v;
                        }
                    }
                }

                EditorGUIUtility.labelWidth = prevLabelWidth;
            }
        }

        static void TangentFields(SerializedKeyframe[] serializedPoints, SerializedKeyframe activePoint)
        {
            CurveTangentMode activeTangentMode = (CurveTangentMode) activePoint.tangentMode.enumValueIndex;

            using (new EditorGUI.DisabledScope(activeTangentMode == CurveTangentMode.Auto))
            {
                if (activeTangentMode == CurveTangentMode.Auto || 
                    activeTangentMode == CurveTangentMode.ClampedAuto)
                {
                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUI.showMixedValue = serializedPoints.Any(serializedPoint => activePoint.outTangent.vector3Value != serializedPoint.outTangent.vector3Value);
                        var tangent = activePoint.outTangent.vector3Value;
                        tangent = EditorGUILayout.Vector3Field(Styles.tangent, tangent);

                        if (scope.changed)
                        {
                            foreach (SerializedKeyframe otherPoint in serializedPoints)
                            {
                                otherPoint.inTangent.vector3Value = tangent;
                                otherPoint.outTangent.vector3Value = tangent;
                            }
                        }
                    }
                }
                else
                {
                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUI.showMixedValue = serializedPoints.Any(serializedPoint => activePoint.inTangent.vector3Value != serializedPoint.inTangent.vector3Value);
                        var inTangent = EditorGUILayout.Vector3Field(Styles.inTangent, activePoint.inTangent.vector3Value);

                        EditorGUI.showMixedValue = serializedPoints.Any(serializedPoint => activePoint.outTangent.vector3Value != serializedPoint.outTangent.vector3Value);
                        var outTangent = EditorGUILayout.Vector3Field(Styles.outTangent, activePoint.outTangent.vector3Value);

                        if (scope.changed)
                        {
                            foreach (SerializedKeyframe otherPoint in serializedPoints)
                            {
                                otherPoint.inTangent.vector3Value = inTangent;
                                otherPoint.outTangent.vector3Value = outTangent;
                            }
                        }
                    }
                }
            }
            
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.showMixedValue = serializedPoints.Any(serializedPoint => activePoint.tangentMode.enumValueIndex != serializedPoint.tangentMode.enumValueIndex);
                activeTangentMode = (CurveTangentMode) EditorGUILayout.Popup(Styles.tangentMode, (int) activeTangentMode, activePoint.tangentMode.enumDisplayNames);
                
                if (scope.changed)
                {
                    foreach (SerializedKeyframe otherPoint in serializedPoints)
                        otherPoint.tangentMode.enumValueIndex = (int)activeTangentMode;
                }
            }
        }
    }
}
