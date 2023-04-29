using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core.Editor
{
    [ExecuteInEditMode]
    public sealed class RefactorNode : MonoBehaviour
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
            oldPath = AnimatorRefactor.GetRelativePath(gameObject);
            //log oldpath
            // Debug.Log("old:"+oldPath);
        }

        private void OnTransformParentChanged()
        {
            var newPath = AnimatorRefactor.GetRelativePath(gameObject);
            AnimatorRefactor.RefactorClips(gameObject, oldPath, newPath);
            //log newpath
            // Debug.Log("new:"+newPath);
            
        }
    }
    
}