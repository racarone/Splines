using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Splines
{
    static class SplineHandles
    {
        [UserSetting("Scene View", "Point Color", "The color of the spline tangent lines in the Scene View.")]
        static readonly Pref<Color> s_PointColor = new Pref<Color>("editor.splinePointColor", Color.white);

        [UserSetting("Scene View", "Point Size", "The color of the spline tangent lines in the Scene View.")]
        static readonly Pref<float> s_PointSize = new Pref<float>("editor.splinePointSize", 1.0f);
        
        [UserSetting("Scene View", "Tangent Color", "The color of the spline tangents in the Scene View.")]
        static readonly Pref<Color> s_TangentColor = new Pref<Color>("editor.splineTangentColor", Color.white);

        [UserSetting("Scene View", "Handle Size", "The color of the spline tangent lines in the Scene View.")]
        static readonly Pref<float> s_HandleSize = new Pref<float>("editor.splineHandleSize", 1.0f);

        class SelectionGroup
        {
            readonly Spline m_Spline;
            readonly Vector3[] m_Position;
            readonly Vector3[] m_Tangent;
            readonly Vector3[] m_Scale;
            readonly Quaternion[] m_Rotation;
            readonly int[] m_Indices;

            public SelectionGroup(Spline spline, int[] indices)
            {
                m_Spline = spline;
                m_Position = new Vector3[spline.pointCount];
                m_Tangent = new Vector3[spline.pointCount];
                m_Scale = new Vector3[spline.pointCount];
                m_Rotation = new Quaternion[spline.pointCount];
                m_Indices = indices;

                for (int i = 0; i < indices.Length; ++i)
                {
                    m_Position[indices[i]] = spline.GetPositionAtIndex(indices[i], Space.World);
                    m_Tangent[indices[i]] = spline.GetOutTangentAtIndex(indices[i], Space.Self);
                    m_Scale[indices[i]] = spline.GetScaleAtIndex(indices[i]);
                    m_Rotation[indices[i]] = spline.GetRotationAtIndex(indices[i], Space.Self);
                }
            }

            public void ApplyTranslation(Vector3 translation)
            {
                var postApplyMatrix = GetPostApplyMatrix();
                var preApplyMatrix = postApplyMatrix.inverse;

                foreach (int selectedIndex in m_Indices)
                {
                    var newPosition = postApplyMatrix.MultiplyPoint3x4(translation + preApplyMatrix.MultiplyPoint3x4(m_Position[selectedIndex]));
                    m_Spline.SetPositionAtIndex(selectedIndex, newPosition, Space.World);
                }
            }

            public void ApplyRotation(Quaternion deltaRotation)
            {
                foreach (int selectedIndex in m_Indices)
                {
                    m_Spline.SetRotationAtIndex(selectedIndex, m_Rotation[selectedIndex] * deltaRotation, Space.Self);
                }
            }

            public void ApplyTangentTransform(Matrix4x4 deltaTransform)
            {
                var postApplyMatrix = GetPostApplyMatrix();
                var preApplyMatrix = postApplyMatrix.inverse;

                foreach (int selectedIndex in m_Indices)
                {
                    var newTangent = postApplyMatrix.MultiplyVector(deltaTransform * preApplyMatrix.MultiplyVector(m_Tangent[selectedIndex]));
                    m_Spline.SetTangentAtIndex(selectedIndex, newTangent, Space.Self);
                }
            }

            public void ApplyPositionTransform(Matrix4x4 deltaTransform)
            {
                var postApplyMatrix = GetPostApplyMatrix();
                var preApplyMatrix = postApplyMatrix.inverse;

                foreach (int selectedIndex in m_Indices)
                {
                    var newPosition = postApplyMatrix.MultiplyPoint3x4(deltaTransform * preApplyMatrix.MultiplyPoint3x4(m_Position[selectedIndex]));
                    m_Spline.SetPositionAtIndex(selectedIndex, newPosition, Space.World);
                }
            }

            public void ApplyTransform(Matrix4x4 deltaTransform)
            {
                var postApplyMatrix = GetPostApplyMatrix();
                var preApplyMatrix = postApplyMatrix.inverse;

                foreach (int selectedIndex in m_Indices)
                {
                    var newPosition = postApplyMatrix.MultiplyPoint3x4(deltaTransform * preApplyMatrix.MultiplyPoint3x4(m_Position[selectedIndex]));
                    m_Spline.SetPositionAtIndex(selectedIndex, newPosition, Space.World);

                    if (m_Spline.GetTangentModeAtIndex(selectedIndex) == CurveTangentMode.Free)
                    {
                        var newTangent = postApplyMatrix.MultiplyVector(deltaTransform * preApplyMatrix.MultiplyVector(m_Tangent[selectedIndex]));
                        m_Spline.SetTangentAtIndex(selectedIndex, newTangent, Space.Self);
                    }
                }
            }

            public void ApplyScale(Vector3 deltaScale)
            {
                foreach (int selectedIndex in m_Indices)
                {
                    m_Spline.SetLocalScaleAtIndex(selectedIndex, Vector3.Scale(m_Scale[selectedIndex], deltaScale));
                }
            }
        }

        static Vector3 s_HandlePosition = Vector3.zero;
        static Vector3 s_HandlePositionOrigin = Vector3.zero;
        static Quaternion s_HandleRotation = Quaternion.identity;
        static Quaternion s_HandleRotationOrigin = Quaternion.identity;
        static bool s_EditingSelection;
        static PivotMode s_PivotMode;
        static PivotRotation s_PivotRotation;
        static SelectionGroup s_SelectionGroup;
        static Vector3 s_CurrentPosition = Vector3.zero;
        static Quaternion s_CurrentRotation = Quaternion.identity;
        static Quaternion s_CurrentRollRotation = Quaternion.identity;
        static Vector3 s_CurrentScale = Vector3.one;

        static SplineHandles()
        {
            SplineSelection.onSplineSelectionChanged += OnSplineSelectionChanged;
        }

        static void OnSplineSelectionChanged()
        {
            InvalidateSelectionGroup();
        }

        static void InvalidateSelectionGroup()
        {
            s_SelectionGroup = null;
        }

        static void SetupHandles(Spline spline, Editor owner)
        {
            if (!s_EditingSelection || s_PivotMode != Tools.pivotMode || s_PivotRotation != Tools.pivotRotation)
            {
                s_HandlePositionOrigin = GetSelectionPivotPosition(spline);
                s_HandleRotationOrigin = GetSelectionPivotRotation(spline);
                s_HandlePosition = s_HandlePositionOrigin;
                s_HandleRotation = s_HandleRotationOrigin;
                s_PivotMode = Tools.pivotMode;
                s_PivotRotation = Tools.pivotRotation;
                s_CurrentPosition = Vector3.zero;
                s_CurrentRotation = Quaternion.identity;
                s_CurrentRollRotation = Quaternion.identity;
                s_CurrentScale = SplineSelection.activeIndex != -1 ? spline.GetScaleAtIndex(SplineSelection.activeIndex) : Vector3.one;
                s_SelectionGroup = null;
            }

            if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.Ignore)
                EndHandleEdit(spline, owner);
        }

        static void BeginHandleEdit(Spline spline, Editor owner, string undoName)
        {
            if (s_EditingSelection)
                return;

            s_EditingSelection = true;
            s_SelectionGroup = new SelectionGroup(spline, SplineSelection.indices);

            Undo.RecordObject(spline, undoName);
        }

        static void EndHandleEdit(Spline spline, Editor owner)
        {
            if (!s_EditingSelection)
                return;

            spline.UpdateSpline();

            s_EditingSelection = false;
        }

        static Vector3 GetSelectionPivotPosition(Spline spline)
        {
            switch (SplineSelection.indices.Length)
            {
                case 0:
                    return Vector3.zero;
                case 1:
                    return spline.GetPositionAtIndex(SplineSelection.activeIndex, Space.World);
                default:
                {
                    if (Tools.pivotMode == PivotMode.Center)
                    {
                        Vector3 averagePosition = SplineSelection.indices.Aggregate(Vector3.zero, (current, index) => current + spline.GetPositionAtIndex(index, Space.World));
                        return averagePosition / Mathf.Max(SplineSelection.indices.Length, 1);
                    }
                    else
                    {
                        return spline.GetPositionAtIndex(SplineSelection.activeIndex, Space.World);
                    }
                }
            }
        }

        static Quaternion GetSelectionPivotRotation(Spline spline)
        {
            if (SplineSelection.activeIndex == -1 || Tools.pivotRotation != PivotRotation.Local)
                return Quaternion.identity;

            return spline.GetRotationAtIndex(SplineSelection.activeIndex, Space.World);
        }

        static Matrix4x4 GetPostApplyMatrix()
        {
            return Matrix4x4.TRS(s_HandlePositionOrigin, s_HandleRotationOrigin, Vector3.one);
        }

        public static void DrawHandles(Spline spline, Editor owner)
        {
            if (!spline)
                return;

            var prevZTest = Handles.zTest;

            SetupHandles(spline, owner);

            for (int index = 0; index < spline.pointCount; ++index)
            {
                if (SplineSelection.ContainsIndex(index))
                    DrawTangentHandles(spline, owner, index);

                DrawPointHandle(spline, owner, index);
            }

            Handles.zTest = CompareFunction.Disabled;

            DrawInsertHandle(spline, owner);

            if (SplineSelection.activeIndex != -1)
            {
                switch (Tools.current)
                {
                    case Tool.Move:
                        PositionHandle(spline, owner);
                        break;
                    case Tool.Rotate:
                        RotationHandle(spline, owner);
                        break;
                    case Tool.Scale:
                        ScaleHandle(spline, owner);
                        break;
                }
            }

            Handles.zTest = prevZTest;
        }

        static void DrawPointHandle(Spline spline, Editor owner, int index)
        {
            Vector3 position = spline.GetPositionAtIndex(index, Space.World);
            float handleSize = HandleUtility.GetHandleSize(position) * s_PointSize * 0.1f;
            int id = GUIUtility.GetControlID(FocusType.Passive);

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    if (SplineSelection.activeIndex != index)
                    {
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position, handleSize * 0.5f));
                    }
                    break;
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;

                        if (evt.button == 0)
                        {
                            if (evt.shift || EditorGUI.actionKey)
                            {
                                if (!SplineSelection.ContainsIndex(index))
                                {
                                    // Selection Add
                                    if (evt.shift)
                                    {
                                        AddSelectionRange(SplineSelection.indices, index, spline.pointCount);
                                    }
                                    else
                                    {
                                        SplineSelection.activeIndex = index;
                                    }
                                }
                                else
                                {
                                    // Selection Subtract
                                    SplineSelection.indices = SplineSelection.indices.Except(new[] {index}).ToArray();
                                }
                            }
                            else
                            {
                                // Select
                                SplineSelection.indices = new[] {index};
                            }
                        }

                        evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    Color color = s_PointColor;

                    if (GUIUtility.hotControl == id)
                        color = Handles.selectedColor * 0.75f;
                    else if (HandleUtility.nearestControl == id)
                        color = Handles.preselectionColor;
                    else if (SplineSelection.ContainsIndex(index))
                        color = Handles.selectedColor;

                    Vector3 direction = -Camera.current.transform.forward;
                    using (new Handles.DrawingScope(color * 0.5f))
                    {
                        Handles.zTest = CompareFunction.Greater;
                        Handles.DrawSolidDisc(position, direction, handleSize);
                    }

                    using (new Handles.DrawingScope(color))
                    {
                        Handles.zTest = CompareFunction.LessEqual;
                        Handles.DrawSolidDisc(position, direction, handleSize);
                    }
                    break;
            }
        }

        static float GetHandleSize(Vector3 position)
        {
            return HandleUtility.GetHandleSize(position) * s_HandleSize * 0.1f;
        }

        static void DrawTangentHandles(Spline spline, Editor owner, int index)
        {
            Vector3 position = spline.GetPositionAtIndex(index, Space.World);
            Vector3 inTanLocal = spline.GetInTangentAtIndex(index, Space.Self);
            Vector3 outTanLocal = spline.GetOutTangentAtIndex(index, Space.Self);
            Vector3 inTanWS = position - inTanLocal;
            Vector3 outTanWS = position + outTanLocal;

            Color frontColor = s_TangentColor;
            Color behindColor = frontColor;
            behindColor.a *= 0.5f;
            
            Handles.zTest = CompareFunction.Greater;
            Handles.color = behindColor;
            Handles.DrawDottedLine(inTanWS, outTanWS, 3f);
            
            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = frontColor;
            Handles.DrawDottedLine(inTanWS, outTanWS, 3f);
            
            float tangentScaleSnap = EditorSnapSettings.gridSnapEnabled ? EditorSnapSettings.scale : 0f;

            frontColor = Handles.zAxisColor; 
            behindColor = frontColor;
            behindColor.a *= 0.5f;

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                Vector3 halfSize = outTanLocal * 0.8f;
                Vector3 outTanH = position + halfSize;
                float handleSize = GetHandleSize(outTanH) * 2f;
                Vector3 newOutTanH;
                
                Handles.zTest = CompareFunction.LessEqual;
                Handles.color = frontColor;
                newOutTanH = Handles.Slider(outTanH, outTanLocal.normalized, handleSize, Handles.ConeHandleCap, tangentScaleSnap);

                Handles.zTest = CompareFunction.Greater;
                Handles.color = behindColor;
                newOutTanH = Handles.Slider(outTanH, outTanLocal.normalized, handleSize, Handles.ConeHandleCap, tangentScaleSnap);

                if (scope.changed)
                {
                    var deltaTan = (newOutTanH - outTanH) * 1.2f;
                    if (deltaTan.magnitude > 0)
                    {
                        BeginHandleEdit(spline, owner, "Scale Tangent");
                        spline.SetTangentsAtIndex(index, inTanLocal + deltaTan, outTanLocal + deltaTan, Space.Self);
                    }
                }
            }

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                Vector3 halfSize = inTanLocal * 0.8f;
                Vector3 inTanH = position - halfSize;
                float handleSize = GetHandleSize(inTanH) * 2f;
                Vector3 newInTanH;
                
                Handles.zTest = CompareFunction.LessEqual;
                Handles.color = frontColor;
                newInTanH = Handles.Slider(inTanH, -inTanLocal.normalized, handleSize, Handles.ConeHandleCap, tangentScaleSnap);

                Handles.zTest = CompareFunction.Greater;
                Handles.color = behindColor;
                newInTanH = Handles.Slider(inTanH, -inTanLocal.normalized, handleSize, Handles.ConeHandleCap, tangentScaleSnap);

                if (scope.changed)
                {
                    var deltaTan = (newInTanH - inTanH) * 1.2f;
                    if (deltaTan.magnitude > 0)
                    {
                        BeginHandleEdit(spline, owner, "Scale Tangent");
                        spline.SetTangentsAtIndex(index, inTanLocal - deltaTan, outTanLocal - deltaTan, Space.Self);
                    }
                }
            }

            Vector3 moveSnap = EditorSnapSettings.gridSnapEnabled ? EditorSnapSettings.move : Vector3.zero;

            frontColor = s_TangentColor;
            behindColor = frontColor;
            behindColor.a *= 0.5f;

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                float handleSize = GetHandleSize(outTanWS);
                Vector3 newOutTanWS;

                Handles.zTest = CompareFunction.LessEqual;
                Handles.color = frontColor;
                newOutTanWS = Handles.FreeMoveHandle(outTanWS, Quaternion.identity, handleSize, moveSnap, Handles.RectangleHandleCap);

                Handles.zTest = CompareFunction.Greater;
                Handles.color = behindColor;
                newOutTanWS = Handles.FreeMoveHandle(outTanWS, Quaternion.identity, handleSize, moveSnap, Handles.RectangleHandleCap);
                
                if (spline.axis != SplineAxis.All)
                {
                    if ((spline.axis & SplineAxis.X) == 0)
                        newOutTanWS.x = outTanWS.x;
                    if ((spline.axis & SplineAxis.Y) == 0)
                        newOutTanWS.y = outTanWS.y;
                    if ((spline.axis & SplineAxis.Z) == 0)
                        newOutTanWS.z = outTanWS.z;
                }

                if (scope.changed)
                {
                    var deltaTan = (newOutTanWS - outTanWS);
                    if (deltaTan.magnitude > 0)
                    {
                        BeginHandleEdit(spline, owner, "Move Tangent");
                        spline.SetTangentsAtIndex(index, inTanLocal + deltaTan, outTanLocal + deltaTan, Space.Self);
                    }
                }
            }

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                float handleSize = GetHandleSize(inTanWS);
                Vector3 newInTanWS;

                Handles.zTest = CompareFunction.LessEqual;
                Handles.color = frontColor;
                newInTanWS = Handles.FreeMoveHandle(inTanWS, Quaternion.identity, handleSize, moveSnap, Handles.RectangleHandleCap);

                Handles.zTest = CompareFunction.Greater;
                Handles.color = behindColor;
                newInTanWS = Handles.FreeMoveHandle(inTanWS, Quaternion.identity, handleSize, moveSnap, Handles.RectangleHandleCap);

                if (spline.axis != SplineAxis.All)
                {
                    if ((spline.axis & SplineAxis.X) == 0)
                        newInTanWS.x = inTanWS.x;
                    if ((spline.axis & SplineAxis.Y) == 0)
                        newInTanWS.y = inTanWS.y;
                    if ((spline.axis & SplineAxis.Z) == 0)
                        newInTanWS.z = inTanWS.z;
                }
                
                if (scope.changed)
                {
                    var deltaTan = (newInTanWS - inTanWS);
                    if (deltaTan.magnitude > 0f)
                    {
                        BeginHandleEdit(spline, owner, "Move Tangent");
                        spline.SetTangentsAtIndex(index, inTanLocal - deltaTan, outTanLocal - deltaTan, Space.Self);
                    }
                }
            }
        }

        struct SplineGUIPoint
        {
            public Vector2 pos;
            public float spineKey;
            public int splineIndex;
        }

        static readonly List<SplineGUIPoint> s_SplineGUIPoints = new List<SplineGUIPoint>();

        public static Vector2 ClosestPointOnLineSegment2D(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 pa = p - a;

            float abSqrMagnitude = ab.sqrMagnitude;
            if (abSqrMagnitude == 0)
                return a;

            float t = Mathf.Clamp01(Vector2.Dot(pa, ab) / abSqrMagnitude);
            return a + ab * t;
        }

        static void DrawInsertHandle(Spline spline, Editor owner)
        {
            Event evt = Event.current;

            // Insert spline point
            if (evt.shift && EditorGUI.actionKey)
            {
                s_SplineGUIPoints.Clear();

                const int stepsPerSegment = 10;

                for (int i = 0; i < spline.pointCount; ++i)
                {
                    for (int d = 0; d < stepsPerSegment; ++d)
                    {
                        float key = i + (d / (float) stepsPerSegment);
                        s_SplineGUIPoints.Add(new SplineGUIPoint
                        {
                            pos = HandleUtility.WorldToGUIPoint(spline.GetPositionAtKey(key, Space.World)),
                            spineKey = key,
                            splineIndex = i
                        });
                    }
                }

                float minDistance = float.MaxValue;
                int closestPointIndex = 0;
                int closestSegmentIndex = 0;

                for (int i = 0; i < s_SplineGUIPoints.Count - 1; i++)
                {
                    float dst = HandleUtility.DistancePointToLineSegment(evt.mousePosition, s_SplineGUIPoints[i].pos, s_SplineGUIPoints[i + 1].pos);

                    if (dst < minDistance)
                    {
                        minDistance = dst;
                        closestPointIndex = i;
                        closestSegmentIndex = s_SplineGUIPoints[i].splineIndex;
                    }
                }

                Vector2 closestPointOnLine = ClosestPointOnLineSegment2D(evt.mousePosition, s_SplineGUIPoints[closestPointIndex].pos, s_SplineGUIPoints[closestPointIndex + 1].pos);
                float distance = (s_SplineGUIPoints[closestPointIndex].pos - closestPointOnLine).magnitude;
                float ratio = distance / (s_SplineGUIPoints[closestPointIndex].pos - s_SplineGUIPoints[closestPointIndex + 1].pos).magnitude;
                Vector3 a = spline.GetPositionAtKey(s_SplineGUIPoints[closestPointIndex].spineKey, Space.World);
                Vector3 b = spline.GetPositionAtKey(s_SplineGUIPoints[closestPointIndex + 1].spineKey, Space.World);
                Vector3 closestWorld = Vector3.Lerp(a, b, ratio);

                var handleSize = GetHandleSize(closestWorld) * 1.5f;
                var id = GUIUtility.GetControlID(FocusType.Passive);

                switch (evt.GetTypeForControl(id))
                {
                    case EventType.Layout:
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(closestWorld, handleSize * 3f));
                        break;
                    case EventType.MouseDown:
                        if (HandleUtility.nearestControl == id && evt.button == 0)
                        {
                            GUIUtility.hotControl = id;
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id)
                        {
                            GUIUtility.hotControl = 0;

                            if (evt.button == 0 && (evt.shift && EditorGUI.actionKey))
                            {
                                Undo.RecordObject(spline, "Insert Point");
                                spline.InsertPointAtIndex(closestSegmentIndex + 1, closestWorld, Space.World);
                            }

                            evt.Use();
                        }
                        break;
                    case EventType.Repaint:
                        if (GUIUtility.hotControl == id || GUIUtility.hotControl == 0)
                        {
                            var direction = -Camera.current.transform.forward;
                            var color = new Color(1f, 1f, 1f, 0.8f);
                            using (new Handles.DrawingScope(color))
                            {
                                Handles.DrawWireDisc(closestWorld, direction, handleSize);
                            }
                        }
                        break;
                }
            }
        }

        static void PositionHandle(Spline spline, Editor owner)
        {
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var prevHandlePosition = s_HandlePosition;
                s_HandlePosition = Handles.PositionHandle(s_HandlePosition, s_HandleRotation);

                if (spline.axis != SplineAxis.All)
                {
                    if ((spline.axis & SplineAxis.X) == 0)
                        s_HandlePosition.x = prevHandlePosition.x;
                    if ((spline.axis & SplineAxis.Y) == 0)
                        s_HandlePosition.y = prevHandlePosition.y;
                    if ((spline.axis & SplineAxis.Z) == 0)
                        s_HandlePosition.z = prevHandlePosition.z;
                }

                if (scope.changed)
                {
                    BeginHandleEdit(spline, owner, "Move Selection");
                    
                    Vector3 delta = s_HandlePosition - s_HandlePositionOrigin;
                    s_SelectionGroup.ApplyTranslation(Quaternion.Inverse(s_HandleRotationOrigin) * delta);
                    
                    spline.UpdateSpline();
                    owner.Repaint();
                }
            }
        }

        static void RotationHandle(Spline spline, Editor owner)
        {
            var prevHandleMatrix = Handles.matrix;

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                Handles.matrix = Matrix4x4.TRS(s_HandlePosition, s_HandleRotation, Vector3.one);
                bool isRollMode = false;

                if (SplineSelection.indices.Length > 1)
                {
                    s_CurrentRotation = DoCustomRotationHandle(s_CurrentRotation, Vector3.zero, 3);
                }
                else
                {
                    if (Tools.pivotRotation == PivotRotation.Local)
                    {
                        using (var rollScope = new EditorGUI.ChangeCheckScope())
                        {
                            s_CurrentRollRotation = DoCustomRollHandle(s_CurrentRollRotation, Vector3.zero);
                            isRollMode = rollScope.changed;
                        }
                        
                        if (GUIUtility.hotControl != CustomRotationHandleIds.@default.roll)
                        {
                            s_CurrentRotation = DoCustomRotationHandle(s_CurrentRotation, Vector3.zero, 2);
                        }
                    }
                    else
                    {
                        s_CurrentRotation = DoCustomRotationHandle(s_CurrentRotation, Vector3.zero, 3);
                    }
                }

                if (scope.changed)
                {
                    BeginHandleEdit(spline, owner, "Rotate Selection");

                    var transform = Matrix4x4.Rotate(s_CurrentRotation);
                    if (SplineSelection.indices.Length > 1)
                    {
                        s_SelectionGroup.ApplyTransform(transform);
                    }
                    else
                    {
                        if (isRollMode)
                        {
                            s_SelectionGroup.ApplyRotation(s_CurrentRollRotation);
                        }
                        else
                        {
                            s_SelectionGroup.ApplyTangentTransform(transform);
                        }
                    }

                    spline.UpdateSpline();
                    owner.Repaint();
                }
            }

            Handles.matrix = prevHandleMatrix;
        }

        static void ScaleHandle(Spline spline, Editor owner)
        {
            var prevHandleMatrix = Handles.matrix;

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var size = HandleUtility.GetHandleSize(s_HandlePosition);
                Handles.matrix = Matrix4x4.TRS(s_HandlePosition, s_HandleRotation, Vector3.one);
                s_CurrentScale = Handles.ScaleHandle(Vector3.one, Vector3.zero, Quaternion.identity, size);

                if (scope.changed)
                {
                    BeginHandleEdit(spline, owner, "Scale Selection");

                    if (Event.current.shift)
                    {
                        if (SplineSelection.indices.Length > 1)
                            s_SelectionGroup.ApplyPositionTransform(Matrix4x4.Scale(s_CurrentScale));
                        else
                            s_SelectionGroup.ApplyTangentTransform(Matrix4x4.Scale(s_CurrentScale));
                    }
                    else
                    {
                        s_SelectionGroup.ApplyScale(s_CurrentScale);
                    }

                    spline.UpdateSpline();
                    owner.Repaint();
                }
            }

            Handles.matrix = prevHandleMatrix;
        }

        static void AddSelectionRange(int[] existingSelection, int index, int numIndices)
        {
            if (existingSelection.Length == 0)
            {
                SplineSelection.indices = new[] {index};
                return;
            }

            int minSelected = numIndices - 1, maxSelected = 0;
            foreach (int i in existingSelection)
            {
                if (minSelected > i) minSelected = i;
                if (maxSelected < i) maxSelected = i;
            }

            var newIndices = new List<int>(minSelected + maxSelected + 1);
            newIndices.AddRange(existingSelection);

            if (index > maxSelected)
            {
                for (int i = maxSelected + 1; i <= index; i++)
                    newIndices.Add(i);
            }
            else if (index < minSelected)
            {
                for (int i = minSelected - 1; i >= index; i--)
                    newIndices.Add(i);
            }
            else
            {
                for (int i = minSelected + 1; i <= index; i++)
                    newIndices.Add(i);
            }

            SplineSelection.indices = newIndices.ToArray();
        }

        readonly struct CustomRotationHandleIds
        {
            public readonly int x;
            public readonly int y;
            public readonly int roll;
            public readonly int cameraAxis;
            public readonly int xyz;

            public static readonly CustomRotationHandleIds @default = new CustomRotationHandleIds(
                GUIUtility.GetControlID("splineRotTanX".GetHashCode(), FocusType.Passive),
                GUIUtility.GetControlID("splineRotTanY".GetHashCode(), FocusType.Passive),
                GUIUtility.GetControlID("splineRoll".GetHashCode(), FocusType.Passive),
                GUIUtility.GetControlID("splineRotCamAxis".GetHashCode(), FocusType.Passive),
                GUIUtility.GetControlID("splineRotXYZ".GetHashCode(), FocusType.Passive));

            public bool Has(int id) => x == id || y == id || (roll == id || cameraAxis == id) || xyz == id;

            public CustomRotationHandleIds(int x, int y, int roll, int cameraAxis, int xyz)
            {
                this.x = x;
                this.y = y;
                this.roll = roll;
                this.cameraAxis = cameraAxis;
                this.xyz = xyz;
            }

            public override int GetHashCode() => x ^ y ^ roll ^ cameraAxis ^ xyz;

            public int this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return x;
                        case 1:
                            return y;
                        case 2:
                            return roll;
                        case 3:
                            return cameraAxis;
                        case 4:
                            return xyz;
                        default:
                            return -1;
                    }
                }
            }
        }

        static readonly Color[] s_CustomAxisColor = new Color[]
        {
            Handles.xAxisColor,
            Handles.yAxisColor,
            Handles.zAxisColor
        };
        
        static readonly Vector3[] s_CustomAxisVector = new Vector3[]
        {
            Vector3.right,
            Vector3.up,
            Vector3.forward
        };

        internal static Quaternion DoCustomRotationHandle(Quaternion rotation, Vector3 position, int count)
        {
            CustomRotationHandleIds ids = CustomRotationHandleIds.@default;
            
            Event evt = Event.current;
            Vector3 forward = Handles.inverseMatrix.MultiplyVector(Camera.current != null ? Camera.current.transform.forward : Vector3.forward);
            
            float handleSize = HandleUtility.GetHandleSize(position);
            Color prevColor = Handles.color;
            
            bool guiEnabled = !GUI.enabled;
            bool isHot = ids.Has(GUIUtility.hotControl);
            
            if (!guiEnabled && (ids.xyz == GUIUtility.hotControl || !isHot))
            {
                Handles.color = new Color(0.0f, 0.0f, 0.0f, 0.3f);
                rotation = Handles.FreeRotateHandle(ids.xyz, rotation, position, handleSize * 1.1f);
            }

            for (int index = 0; index < count; ++index)
            {
                Color colorByAxis = s_CustomAxisColor[index];
                Handles.color = colorByAxis;
                Vector3 axisVector = s_CustomAxisVector[index];
                rotation = Handles.Disc(ids[index], rotation, position, rotation * axisVector, handleSize, true, EditorSnapSettings.rotate);
            }

            if (isHot && evt.type == EventType.Repaint)
            {
                Handles.color = Handles.secondaryColor;
                Handles.DrawWireDisc(position, forward, handleSize, Handles.lineThickness);
            }

            Handles.color = prevColor;
            return rotation;
        }

        internal static Quaternion DoCustomRollHandle(Quaternion rotation, Vector3 position)
        {
            CustomRotationHandleIds ids = CustomRotationHandleIds.@default;
            float handleSize = HandleUtility.GetHandleSize(position);
            Color prevColor = Handles.color;
            Handles.color = Handles.zAxisColor;
            Vector3 axisVector = Vector3.forward;
            
            rotation = Handles.Disc(ids.roll, rotation, position, rotation * axisVector, handleSize, true, EditorSnapSettings.rotate);

            Handles.color = prevColor;
            return rotation;
        }
    }
}