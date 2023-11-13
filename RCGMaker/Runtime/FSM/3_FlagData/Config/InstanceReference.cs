using System;
using System.IO;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCGMaker.Core
{
    //ScriptableObject, 
    [CreateAssetMenu(menuName = "RCGMaker/InstanceReference")]
    public class InstanceReference : GameFlagBase
    {
        public GameObject prefab;
        private GameObject _instance;

        //flag awake?
        public override void FlagAwake(TestMode mode)
        {
            base.FlagAwake(mode);
            
            //哭了 現在FlagAwake 比大家的Awake還晚（SaveManager非同步的關係Orz 清掉會錯）
            //_instance = null;
        }

        [ShowInPlayMode]
        public GameObject instance
        {
            get => _instance;
        }

        public void UnRegister(GameObject g)
        {
            if (_instance == g)
                _instance = null;
            
            //Debug.Log("UnRegister:"+this.name + ":"+g,g);
        }

        public void Register(GameObject g)
        {
            if (_instance == null)
            {
              //  Debug.Log("Register:"+this.name + ":"+g,g);
                _instance = g;
            }

    
            else
            {
                Debug.LogError("InstanceReference: instance is already set instance:" + _instance, _instance);
                Debug.LogError("InstanceReference: instance is already set registering:" + g, g);
            }
        }


        [Button]
        private void RenameToPrefabName()
        {
#if UNITY_EDITOR
            //rename the asset
            var path = AssetDatabase.GetAssetPath(this);
            var newPath = Path.GetDirectoryName(path) + "/" + prefab.name + ".asset";
            AssetDatabase.RenameAsset(path, prefab.name);
            AssetDatabase.MoveAsset(path, newPath);
            AssetDatabase.SaveAssets();
#endif
        }
    }
}