using UnityEditor;
using UnityEngine;

namespace MonoFSM.Utility
{
    public static class SceneViewUtility
    {
#if UNITY_EDITOR
        public static void FocusOnGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.LogError("FocusOnGameObject: gameObject is null");
                return;
            }
            
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
#endif
    }
}