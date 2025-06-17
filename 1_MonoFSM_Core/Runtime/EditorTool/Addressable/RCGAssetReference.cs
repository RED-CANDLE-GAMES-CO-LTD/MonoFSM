using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MonoFSM.AddressableAssets
{
    //Serialize Reference是什麼去了？
    [System.Serializable]
    public class RCGAssetReference
    {
        //FIXME: 這個還要拆出去？ 會有 UnityEditor.addressableAssets  的assembly reference
#if UNITY_EDITOR
        [Header("把圖拉上來這")]
        [OnValueChanged(nameof(CreateAssetReference))]
        public Object editorAsset;

        private bool IsAddressableAsset => assetReference != null;

        [HideIf(nameof(IsAddressableAsset))]
        [Button]
        public void CreateAssetReference()
        {
            // #if UNITY_EDITOR
            // Debug.LogError("CreateAssetReference:" + editorAsset, editorAsset);
            // if (assetReference.editorAsset != null)
            //     return;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(editorAsset, out var guid, out long localId);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                assetReference = settings.CreateAssetReference(guid);
                assetReference.SetEditorSubObject(editorAsset);
                Debug.Log("CreateAssetReference:" + editorAsset, editorAsset);
            }
            else
            {
                Debug.Log("CreateAssetReference: Already Exist" + editorAsset, editorAsset);
            }
            // #endif
        }
        //TODO: 可以寫property drawer自動生成assetReference
#endif

        // [PreviewInInspector]
        [SerializeField] private AssetReference assetReference;

        public AssetReference AssetReference => assetReference;

        public string AssetName => assetReference.SubObjectName;

        // public Object Asset => assetReference.Asset;
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
            // Debug.Log("[RCGAsset] LoadAssetAsync: " + assetReference.SubObjectName);
            // Debug.Log("LoadAssetAsync: 1:" + assetReference.SubObjectName);
            var op = assetReference.OperationHandle;
            if (op.IsValid())
            {
                var obj = await op.Task;
                return obj as T;
            }

            try
            {
                //一定要用Ｔ，不然會回傳null
                if (!assetReference.RuntimeKeyIsValid())
                    return null;
                var handle = assetReference.LoadAssetAsync<T>();
                // var obj = handle.WaitForCompletion();
                var obj = await handle.Task;
                return obj;
            }
            catch (System.Exception e)
            {
                Debug.LogError("LoadAssetAsync: Fail" + e.Message);
                Debug.LogError("LoadAssetAsync: Fail" + e.StackTrace);
                return null;
            }
        }

        public async Task<T> GetAssetAsync<T>() where T : Object
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                if (assetReference == null)
                {
                    Debug.LogWarning("AddressableAssetReference is null 暫時用EditorAsset:" + editorAsset, editorAsset);
                    return editorAsset as T;
                }    
            }
#endif

            if (IsAssetLoaded)
            {
                // Debug.Log("GetAssetAsync: IsAssetLoaded:" + assetReference.SubObjectName);
                return assetReference.Asset as T;
            }
            
            var obj = await LoadAsset<T>();
            return obj;
        }

        public void Release()
        {
            if (assetReference.IsValid())
                assetReference.ReleaseAsset();
            // Debug.Log("[RCGAsset] ReleaseAsset:" + assetReference.SubObjectName);
        }
    }
}