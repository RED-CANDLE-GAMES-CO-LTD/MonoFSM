#if UNITY_EDITOR
using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime._3_FlagData;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core.Editor.FlagData
{
    /// <summary>
    ///     新建 / 匯入 GameFlagBase 時自動收進 AllFlagCollection。
    ///     存在理由：漏收的後果是靜默的（runtime FlagAwake 不會跑、存檔查不到），
    ///     而 Shift+S 的全掃重建只在 Scene 模式生效，在 Prefab 編輯模式按不到。
    ///     這裡走增量 AddFlag，不做 FindAllFlagsInProject 全掃。
    /// </summary>
    public class GameFlagAutoCollectPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            if (importedAssets == null || importedAssets.Length == 0)
                return;

            List<string> pendingPaths = null;
            foreach (var path in importedAssets)
            {
                if (!path.EndsWith(".asset"))
                    continue;

                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType == null || !typeof(GameFlagBase).IsAssignableFrom(assetType))
                    continue;

                pendingPaths ??= new List<string>();
                pendingPaths.Add(path);
            }

            if (pendingPaths == null)
                return;

            //import 進行中不要動別的 asset，delay 到這批 import 結束再收
            EditorApplication.delayCall += () => CollectFlags(pendingPaths);
        }

        private static void CollectFlags(List<string> paths)
        {
            var collection = AllFlagCollection.Instance;
            if (collection == null)
            {
                Debug.LogError("[GameFlagAutoCollect] AllFlagCollection.Instance 為 null，略過收錄");
                return;
            }

            foreach (var path in paths)
            {
                var flag = AssetDatabase.LoadAssetAtPath<GameFlagBase>(path);
                if (flag == null)
                {
                    Debug.LogWarning("[GameFlagAutoCollect] 載入失敗，略過:" + path);
                    continue;
                }

                if (collection.Flags.Contains(flag))
                    continue;

                collection.AddFlag(flag);
                Debug.Log("[GameFlagAutoCollect] 已收進 AllFlagCollection:" + flag.name, flag);
            }
        }
    }
}
#endif
