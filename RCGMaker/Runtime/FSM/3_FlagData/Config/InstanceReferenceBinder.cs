using System;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace RCGMaker.Core
{
    public class InstanceReferenceBinder : MonoBehaviour
    {
        [Required] public InstanceReference instanceReference;

        private void OnValidate()
        {
#if UNITY_EDITOR
            //check is belong to the prefab
            if (instanceReference == null)
                return;
            var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
            if (prefabStage == null)
                return;

            var path = prefabStage.assetPath;

            Debug.Log("InstanceBinder path: " + path);
            
            if (path == AssetDatabase.GetAssetPath(instanceReference.prefab))
            {
                Debug.Log("InstanceBinder OnValidate: " + gameObject.name + " is belong to " +
                          instanceReference.prefab.name, instanceReference.prefab);
            }
            else
            {
                //TODO: 什麼意思？
                //FIXME: Player inGameUI 會跑這個
                Debug.LogError("InstanceBinder OnValidate: " + gameObject.name + " is not belong to " +
                               instanceReference.prefab.name);
            }
            // var obj = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
            // Debug.Log("InstanceBinder OnValidate: ", obj);
            // if (PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject) == instanceReference.prefab)
            // {
            //     Debug.Log("InstanceBinder OnValidate: " + gameObject.name + " is belong to " +
            //               instanceReference.prefab.name);
            // }
            // else
            // {
            //     Debug.LogError("InstanceBinder OnValidate: " + gameObject.name + " is not belong to " +
            //                    instanceReference.prefab.name);
            // }
            #endif
        }

        private void Awake()
        {
            instanceReference.instance = gameObject;
        }
    }
}