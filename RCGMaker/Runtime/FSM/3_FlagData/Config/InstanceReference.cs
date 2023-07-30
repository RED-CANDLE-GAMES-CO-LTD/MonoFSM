using System;
using System.IO;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
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
            set
            {
                if (_instance == null)
                    _instance = value;
                else
                {
                    Debug.LogError("InstanceReference: instance is already set");
                }
            }
        }


        [Button]
        private void RenameToPrefabName()
        {
            //rename the asset
            var path = AssetDatabase.GetAssetPath(this);
            var newPath = Path.GetDirectoryName(path) + "/" + prefab.name + ".asset";
            AssetDatabase.RenameAsset(path, prefab.name);
            AssetDatabase.MoveAsset(path, newPath);
            AssetDatabase.SaveAssets();
        }
    }
}