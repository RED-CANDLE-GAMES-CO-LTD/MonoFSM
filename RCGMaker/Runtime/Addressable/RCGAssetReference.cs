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
    //Serialize Reference是什麼去了？
    [System.Serializable]
    public class RCGAssetReference
    {
        //FIXME: 這個還要拆出去？ 會有 UnityEditor.addressableAssets  的assembly reference
#if UNITY_EDITOR
        [OnValueChanged(nameof(CreateAssetReference))]
        public Object editorAsset;

        private bool IsAddressableAsset => assetReference != null;

        [HideIf(nameof(IsAddressableAsset))]
        [Button]
        public void CreateAssetReference()
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(editorAsset, out var guid, out long localId);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            assetReference = settings.CreateAssetReference(guid);
        }
        //TODO: 可以寫property drawer自動生成assetReference
#endif

        public AssetReference assetReference;

        public Object Asset => assetReference.Asset;

        public T GetAsset<T>() where T : Object
        {
            return assetReference.Asset as T;
        }

        public async Task<Object> LoadAsset()
        {
            var validateKeyAsync = Addressables.LoadResourceLocationsAsync(assetReference.RuntimeKey);
            await validateKeyAsync.Task;
            var handle = assetReference.LoadAssetAsync<Object>();
            var obj = await handle.Task;
            return obj;
        }
    }
}