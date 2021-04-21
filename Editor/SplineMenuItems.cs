using System;
using UnityEditor;
using UnityEngine;

namespace Splines
{
    static class SplineMenuItems
    {
        [MenuItem("GameObject/Splines/Spline", false)]
        static void CreateSpline(MenuCommand menuCommand)
        {
            var go = CreateGameObject("Spline", menuCommand.context);
            go.AddComponent<Spline>();
            // Ensure it gets re-parented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
        
        [MenuItem("GameObject/Splines/Spline Array", false)]
        static void CreateSplineSpawner(MenuCommand menuCommand)
        {
            var go = CreateGameObject("Spline Array", menuCommand.context);
            go.AddComponent<Spline>();
            go.AddComponent<SplineArray>();
        }
        
        [MenuItem("GameObject/Splines/Spline Mesh", false)]
        static void CreateSplineMesh(MenuCommand menuCommand)
        {
            var go = CreateGameObject("Spline Mesh", menuCommand.context);
            go.AddComponent<Spline>();
            go.AddComponent<SplineMesh>();
        }

        public static GameObject CreateGameObject(string name, UnityEngine.Object context)
        {
            var parent = context as GameObject;
            var go = CreateGameObject(parent, name);
            GameObjectUtility.SetParentAndAlign(go, context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
            EditorApplication.ExecuteMenuItem("GameObject/Move To View");
            return go;
        }
        
        public static GameObject CreateGameObject(GameObject parent, string name, params Type[] types)
            => ObjectFactory.CreateGameObject(GameObjectUtility.GetUniqueNameForSibling(parent != null ? parent.transform : null, name), types);
    }
}
