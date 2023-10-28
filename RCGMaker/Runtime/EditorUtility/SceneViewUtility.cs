using UnityEditor;
using UnityEngine;

namespace RCGMaker.Utility
{
    public static class SceneViewUtility
    {
        public static void FocusOnGameObject(GameObject gameObject)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView)
            {
                Selection.activeGameObject = gameObject;
                //press f to focus

                sceneView.LookAt(gameObject.transform.position);
            }
        }

        //move gameobject to mouse pos
        public static void MoveGameObjectToMousePos(GameObject gameObject)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView)
            {
                var mousePos = Event.current.mousePosition;
                var worldPosition = HandleUtility.GUIPointToWorldRay(mousePos).GetPoint(.1f);
                worldPosition.z = 0;
                gameObject.transform.position = worldPosition;
            }
        }
    }
}