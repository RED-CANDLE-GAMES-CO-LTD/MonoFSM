using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MonoFSM.AddressableAssets
{
    public class AddressablePrefabLoader : MonoBehaviour
    {
        public AssetReference assetReference;

        private void Awake()
        {
            LoadAsset();
        }

        private async void LoadAsset()
        {
            if (!assetReference.RuntimeKeyIsValid())
                return;
            var handle = assetReference.LoadAssetAsync<GameObject>();
            await handle.Task;
            var prefab = handle.Result;
            Instantiate(prefab, transform);
        }

        private void OnDestroy()
        {
            assetReference.ReleaseAsset();
        }
    }
}