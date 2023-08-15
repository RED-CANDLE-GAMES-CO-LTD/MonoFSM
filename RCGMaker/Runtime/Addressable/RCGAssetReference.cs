using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RCGMaker.AddressableAssets
{
    [System.Serializable]
    public class RCGAssetReference : MonoBehaviour
    {
        //FIXME: 這個還要拆出去？
#if UNITY_EDITOR
        public Object editorAsset;
        [Button]
        public void CreateAssetReference()
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(editorAsset, out var guid, out long localId);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            assetReference = settings.CreateAssetReference(guid);
        }
#endif

        public AssetReference assetReference;

        public Object Asset => assetReference.Asset;

        public T GetAsset<T>() where T : Object
        {
            return assetReference.Asset as T;
        }

        public async Task<Object> LoadAsset()
        {
            var handle = assetReference.LoadAssetAsync<Object>();
            var obj = await handle.Task;
            return obj;
        }
    }
}