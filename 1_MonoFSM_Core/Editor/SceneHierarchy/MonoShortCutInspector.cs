using MonoFSM.InternalBridge;
using MonoFSM.Core;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace _1_MonoFSM_Core.Editor.SceneHierarchy
{
    [CustomEditor(typeof(MonoShortCut))]
    public class MonoShortCutInspector : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            var e = Event.current;

            // Add a button to the inspector
            if (e.keyCode == KeyCode.Return || GUILayout.Button("Open Mono Shortcuts"))
            {
                var shortCut = target as MonoShortCut;
                if (Selection.activeGameObject == shortCut.gameObject && shortCut.targetGameObject != null)
                {
                    Selection.activeGameObject = shortCut.targetGameObject;
                    EditorGUIUtility.PingObject(shortCut.targetGameObject);
                    //FIXME: 好像沒什麼屁用
                    // SceneHierarchyUtility.ExpandHierarchyItem(shortCut.targetGameObject);
                }
            }
        }
        
    }
}