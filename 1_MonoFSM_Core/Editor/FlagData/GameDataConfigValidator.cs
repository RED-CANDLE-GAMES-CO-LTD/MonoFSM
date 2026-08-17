using System.Collections.Generic;
using MonoFSM.Variable;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.FlagData
{
    /// <summary>
    ///     GameData config 表的死欄位檢查：
    ///     schema 不存在於任何 asset，真相是 prefab 結構——「哪些 VarFloat 的 tag 被宣告出來」。
    ///     一顆 GameData 的家族 schema ＝ 所有「用 VarGameData 綁到它」的 prefab 的 VarFloat tag 聯集。
    ///     config 表裡（含 _baseConfig 疊層）不在聯集內的 tag ＝ 死欄位（打錯字或已移除的欄位）。
    ///     入口：Project 視窗選一顆 GameData → 右鍵 Assets/MonoFSM/檢查 GameData Config 死欄位。
    /// </summary>
    public static class GameDataConfigValidator
    {
        private const string LogTag = "[GameDataConfigValidate]";

        [MenuItem("Assets/MonoFSM/檢查 GameData Config 死欄位", false, 2000)]
        private static void ValidateSelected()
        {
            var data = Selection.activeObject as GameData;
            if (data == null)
            {
                Debug.LogWarning($"{LogTag} 請先在 Project 視窗選一顆 GameData asset");
                return;
            }

            Validate(data);
        }

        [MenuItem("Assets/MonoFSM/檢查 GameData Config 死欄位", true)]
        private static bool ValidateSelectedEnabled()
        {
            return Selection.activeObject is GameData;
        }

        /// <summary>
        ///     回傳死欄位清單（同時 LogWarning）。沒有死欄位時回傳空 list。
        /// </summary>
        public static List<VariableTag> Validate(GameData data)
        {
            var deadTags = new List<VariableTag>();
            if (data == null)
                return deadTags;

            var configTags = new List<VariableTag>();
            data.CollectConfigTags(configTags);
            if (configTags.Count == 0)
            {
                Debug.Log($"{LogTag} {data.name} 沒有任何 config entry", data);
                return deadTags;
            }

            var usedPrefabs = FindPrefabsBindingGameData(data);
            if (usedPrefabs.Count == 0)
            {
                Debug.LogWarning(
                    $"{LogTag} 找不到任何 prefab 的 VarGameData 綁到 {data.name}，無法 derive schema（config 全部無人消費？）",
                    data
                );
                return deadTags;
            }

            //家族 schema ＝ 所有消費端 prefab 的 VarFloat tag 聯集
            var schema = new HashSet<VariableTag>();
            foreach (var prefab in usedPrefabs)
            foreach (var varFloat in prefab.GetComponentsInChildren<VarFloat>(true))
                if (varFloat._varTag != null)
                    schema.Add(varFloat._varTag);

            foreach (var tag in configTags)
                if (tag != null && !schema.Contains(tag))
                    deadTags.Add(tag);

            if (deadTags.Count == 0)
            {
                Debug.Log(
                    $"{LogTag} {data.name}：{configTags.Count} 個 config 欄位都有被消費（掃了 {usedPrefabs.Count} 顆 prefab）",
                    data
                );
                return deadTags;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(
                $"{LogTag} {data.name} 有 {deadTags.Count} 個死欄位（沒有任何家族 prefab 宣告對應的 VarFloat）："
            );
            foreach (var tag in deadTags)
                sb.AppendLine($"  - {tag.name}");
            sb.AppendLine($"消費端 prefab（{usedPrefabs.Count} 顆）：");
            foreach (var prefab in usedPrefabs)
                sb.AppendLine($"  - {AssetDatabase.GetAssetPath(prefab)}");
            Debug.LogWarning(sb.ToString(), data);
            return deadTags;
        }

        /// <summary>
        ///     找出全專案哪些 prefab 的 VarGameData 序列化預設值綁了這顆 GameData。
        ///     先用 GetDependencies 過濾，再載入 prefab 確認。
        /// </summary>
        public static List<GameObject> FindPrefabsBindingGameData(GameData data)
        {
            var result = new List<GameObject>();
            var dataPath = AssetDatabase.GetAssetPath(data);
            if (string.IsNullOrEmpty(dataPath))
                return result;

            var guids = AssetDatabase.FindAssets("t:Prefab");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                    continue;

                //非遞迴依賴：prefab 直接引用到這顆 GameData 才算
                var deps = AssetDatabase.GetDependencies(path, false);
                var hit = false;
                for (var d = 0; d < deps.Length; d++)
                    if (deps[d] == dataPath)
                    {
                        hit = true;
                        break;
                    }

                if (!hit)
                    continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                //確認是 VarGameData 綁的（editor time Value 讀的是 _defaultValue）
                foreach (var varGameData in prefab.GetComponentsInChildren<VarGameData>(true))
                    if (varGameData.Value == data)
                    {
                        result.Add(prefab);
                        break;
                    }
            }

            return result;
        }
    }
}
