using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
            #if UNITY_EDITOR
            Debug.LogError("CreateAssetReference:" + editorAsset, editorAsset);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(editorAsset, out var guid, out long localId);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            assetReference = settings.CreateAssetReference(guid);
            assetReference.SetEditorSubObject(editorAsset);
            #endif
        }
        //TODO: 可以寫property drawer自動生成assetReference
#endif

        // [PreviewInInspector]
        [SerializeField] private AssetReference assetReference;

        public Object Asset => assetReference.Asset;
        public bool IsAssetLoaded => assetReference.Asset != null;

        public bool IsRuntimeKeyValid => assetReference.RuntimeKeyIsValid();
        public T GetAsset<T>() where T : Object
        {
            return assetReference.Asset as T;
        }

        private async Task<T> LoadAsset<T>() where T : Object
        {
            var validateKeyAsync = Addressables.LoadResourceLocationsAsync(assetReference.RuntimeKey);
            await validateKeyAsync.Task;
            Debug.Log("LoadAssetAsync: 1:" + assetReference.SubObjectName);
            var op = assetReference.OperationHandle;
            if (op.IsValid())
            {
#if UNITY_EDITOR
                Debug.Log("LoadAssetAsync: before old OP:" + assetReference.SubObjectName + " wait load",
                    assetReference.editorAsset);
#endif
                var obj = await op.Task;
#if UNITY_EDITOR
                Debug.Log("LoadAssetAsync: old OP:" + assetReference.SubObjectName + " is loaded" + obj,
                    assetReference.editorAsset);
#endif
                return obj as T;
            }
            else
            {
                var handle = assetReference.LoadAssetAsync<T>();
                // var obj = handle.WaitForCompletion();
#if UNITY_EDITOR
                if (handle.Status == AsyncOperationStatus.Failed)
                    Debug.LogError("LoadAssetAsync Failed:" + assetReference.SubObjectName, assetReference.editorAsset);
                Debug.Log("LoadAssetAsync: new OP:" + assetReference.SubObjectName + " is loaded" + handle.Task,
                    assetReference.editorAsset);
#endif
                var obj = await handle.Task;
                return obj as T;
            }
        }

        public async Task<T> GetAssetAsync<T>() where T : Object
        {
#if UNITY_EDITOR
            if (assetReference == null)
            {
                Debug.LogWarning("AddressableAssetReference is null 暫時用EditorAsset:" + editorAsset, editorAsset);
                return editorAsset as T;
            }
#endif

            // if (IsAssetLoaded)
            // {
            //     Debug.Log(
            //         "GetAssetAsync:" + assetReference.SubObjectName + " already loaded" + assetReference.Asset as T,
            //         assetReference.editorAsset);
            //
            //     return assetReference.Asset as T;
            // }

#if UNITY_EDITOR
            Debug.Log("GetAssetAsync:" + assetReference.SubObjectName + " is not loaded", assetReference.editorAsset);
 #endif
            var obj = await LoadAsset<T>();
#if UNITY_EDITOR
            Debug.Log("GetAssetAsync:" + assetReference.SubObjectName + " is loaded" + obj, assetReference.editorAsset);
#endif
            return obj;
        }

        public void Release()
        {
            assetReference.ReleaseAsset();
        }
    }
}