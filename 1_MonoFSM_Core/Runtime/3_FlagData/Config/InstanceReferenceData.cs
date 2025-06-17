using System;
using System.IO;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace MonoFSM.Core
{
    //ScriptableObject, 
    [CreateAssetMenu(menuName = "RCGMaker/InstanceReference")]
    public class InstanceReferenceData : GameFlagBase
    {
        //FIXME: 蠻糟的有reference污染，會讓scene指到這個再指到prefab

        // public GameObject prefab;
        //兩者要都是動態資料
        
        private GameObject _runTimeInstance;

        //flag awake?
        public override void FlagAwake(TestMode mode)
        {
            base.FlagAwake(mode);
            
            //哭了 現在FlagAwake 比大家的Awake還晚（SaveManager非同步的關係Orz 清掉會錯）
            //_instance = null;
        }

        [ShowInPlayMode] public GameObject RunTimeInstance => _runTimeInstance;

        public void UnRegister(GameObject g)
        {
            if (_runTimeInstance == g)
                _runTimeInstance = null;
            
            //Debug.Log("UnRegister:"+this.name + ":"+g,g);
        }

        public void Register(GameObject g)
        {
            if (_runTimeInstance == null)
            {
              //  Debug.Log("Register:"+this.name + ":"+g,g);
              _runTimeInstance = g;
            }

    
            else
            {
                Debug.LogError("InstanceReference: instance is already set instance:" + _runTimeInstance,
                    _runTimeInstance);
                Debug.LogError("InstanceReference: instance is already set registering:" + g, g);
            }
        }


//         [Button]
//         private void RenameToPrefabName()
//         {
// #if UNITY_EDITOR
//             //rename the asset
//             var path = AssetDatabase.GetAssetPath(this);
//             var newPath = Path.GetDirectoryName(path) + "/" + prefab.name + ".asset";
//             AssetDatabase.RenameAsset(path, prefab.name);
//             AssetDatabase.MoveAsset(path, newPath);
//             AssetDatabase.SaveAssets();
// #endif
//         }
    }
}