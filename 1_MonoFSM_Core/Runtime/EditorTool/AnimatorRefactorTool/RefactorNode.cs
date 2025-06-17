using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core.Editor
{
    #if UNITY_EDITOR
    [ExecuteInEditMode]
    public sealed class RefactorNode : MonoBehaviour, ISceneSavingCallbackReceiver, IBeforePrefabSaveCallbackReceiver
    {
        //TODO: 難道要把所有Variant的prefab都重構一次？
        [InfoBox("這個腳本是用來重構Animator的路徑，小心，如果有其他prefab也共享這個節點，可能造成其他的動畫爛掉，記得做完後要移掉唷唷！")]
        [NonSerialized]
        [ShowInInspector]
        public string currentName;

        private void OnValidate()
        {
            // if (gameObject.name != currentName)
            // {
            //     Debug.Log("GameObject was renamed from " + previousName + " to " + gameObject.name);
            //     previousName = currentName;
            //     currentName = gameObject.name;
            // }
            currentName = gameObject.name;
            AnimatorRefactor.Activate();
        }


        private void Start()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Editor用而已，把我拿掉才可以玩！",gameObject);
                Debug.Break();
            }
        }

        [NonSerialized]
        string oldPath;
        
        private void OnBeforeTransformParentChanged()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Editor用而已，把我拿掉才可以玩！", gameObject);
                Debug.Break();
                return;
            }
            
            oldPath = AnimatorRefactor.GetRelativePath(gameObject);
            //log oldpath
            // Debug.Log("old:"+oldPath);
        }

        private void OnTransformParentChanged()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Editor用而已，把我拿掉才可以玩！", gameObject);
                Debug.Break();
                return;
            }
            var newPath = AnimatorRefactor.GetRelativePath(gameObject);
            AnimatorRefactor.RefactorClips(gameObject, oldPath, newPath);
            //log newpath
            // Debug.Log("new:"+newPath);
            
        }

        public void OnBeforeSceneSave()
        {
            Debug.LogError("NONONONO把我拔掉啦！！！");
            UnityEditor.EditorUtility.DisplayDialog("NONONONO把我拔掉啦！！！", this + " RefactorNode沒有拔掉", "OK");
            DestroyImmediate(this);
            Selection.activeGameObject = gameObject;
        }

        public void OnBeforePrefabSave()
        {
            Debug.LogError("NONONONO把我拔掉啦！！！");
            UnityEditor.EditorUtility.DisplayDialog("NONONONO把我拔掉啦！！！", this + " RefactorNode沒有拔掉", "OK");
            DestroyImmediate(this);
            Selection.activeGameObject = gameObject;
        }
    }
    #endif
    
}