using MonoFSMCore.Runtime.LifeCycle;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.FlagData
{
    /// <summary>
    ///     一次性遷移：把 PickableData._entityPrefab 的值搬到 GameData 的一級欄位 _bindPrefab。
    ///     不刪 PickableData 也不清 _entityPrefab（bindPrefab getter 還留著舊資料 fallback）。
    /// </summary>
    public static class BindPrefabMigration
    {
        private const string LogTag = "[BindPrefabMigration]";

        [MenuItem("Tools/MonoFSM/遷移 PickableData._entityPrefab 到 _bindPrefab")]
        private static void Migrate()
        {
            var guids = AssetDatabase.FindAssets("t:GameData");
            var migrated = 0;
            var skipped = 0;
            var noPickable = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<GameData>(path);
                if (data == null)
                    continue;

                var so = new SerializedObject(data);
                var prop = so.FindProperty("_bindPrefab");
                if (prop == null)
                {
                    Debug.LogError($"{LogTag} 找不到 _bindPrefab 欄位: {path}", data);
                    continue;
                }

                if (prop.objectReferenceValue != null)
                {
                    skipped++;
                    continue;
                }

                //不要用 GetDataFunction<T>()，找不到時會 LogError 洗版
                MonoObj prefab = null;
                var functions = data.DataFunctions;
                if (functions != null)
                    for (var i = 0; i < functions.Length; i++)
                        if (functions[i] is PickableData pickable)
                        {
                            prefab = pickable.EntityPrefab;
                            break;
                        }

                if (prefab == null)
                {
                    noPickable++;
                    continue;
                }

                prop.objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(data);
                migrated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"{LogTag} 遷移 {migrated} 個 / 已有值跳過 {skipped} 個 / 沒有 PickableData 或沒填 prefab {noPickable} 個（掃描 {guids.Length} 個 GameData）"
            );
        }
    }
}
