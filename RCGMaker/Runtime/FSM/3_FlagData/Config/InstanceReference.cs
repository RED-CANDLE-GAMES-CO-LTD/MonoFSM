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
    [CreateAssetMenu(menuName = "RCGMaker/InstanceReference")]
    public class InstanceReference : GameFlagBase
    {
        public GameObject prefab;
        private GameObject _instance;

        //flag awake?
        public override void FlagAwake(TestMode mode)
        {
            base.FlagAwake(mode);
            _instance = null;
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
        }

        public void Register(GameObject g)
        {
            if (_instance == null)
                _instance = g;
            else
            {
                Debug.LogError("InstanceReference: instance is already set");
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