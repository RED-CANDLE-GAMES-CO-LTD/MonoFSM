using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 「這個欄位是這顆 prefab 自己改的，還是從 base / nested prefab 繼承來的」的單一判準。
    ///
    /// 為什麼要獨立一支：同一條判斷原本只寫在 HierarchyTextExporter.BuildComponentEntry 裡
    /// （private static，拿不出來），而 peek / locate / 寫後驗證都需要同一套語意。
    /// 兩份實作漂移的代價是「一邊有星號一邊沒有」，比沒有星號更難查。
    ///
    /// 實測（2026-09-03，2768 個 (node,component) snapshot）：
    /// AssetDatabase.LoadAssetAtPath 與 PrefabUtility.LoadPrefabContents 兩種視角下，
    /// prefabOverride / isDefaultOverride / IsAddedComponentOverride 的結果**完全一致** ——
    /// 包含 variant 的 root 層與繼承自 nested prefab instance 的節點。所以星號不需要
    /// 按視角 gate（也**不該** gate 在 GetPrefabInstanceStatus，那是兩種視角唯一會不同的 API）。
    /// </summary>
    public static class PrefabOverrideMark
    {
        /// <summary>
        /// 「值得標星號的 override」。
        /// isDefaultOverride 是 Unity 強制掛在 instance 上的欄位（m_Name / m_IsActive、
        /// instance root 的 m_LocalPosition / m_LocalRotation 等），幾乎每顆都會中，
        /// 標了等於沒標。實測 RectTransform 的 anchor/pivot **不是** defaultOverride，會正常標星號。
        /// </summary>
        public static bool IsMeaningfulOverride(SerializedProperty prop) =>
            prop != null && prop.prefabOverride && !prop.isDefaultOverride;

        /// <summary>
        /// 這顆 object 上「頂層欄位名 → 是 override」的集合。反射端（EditProbe.Dump 用欄位名
        /// 取值）要靠這個對照，因為 override 判定只有 SerializedProperty 有。
        /// 走訪方式跟 HierarchyTextExporter 一致（NextVisible 進第一層後只走同層）。
        /// </summary>
        /// <summary>
        /// 反射拿到的成員名字對不上序列化名字時的備援：Unity 內建 component 的欄位是
        /// `m_AnchoredPosition`，而 peek 的 `--members` 通常寫 C# 屬性名 `anchoredPosition`。
        /// 專案自己的 `_field` 兩邊同名，走不到這裡。
        /// </summary>
        public static bool Contains(HashSet<string> overrides, string memberName)
        {
            if (overrides == null || overrides.Count == 0 || string.IsNullOrEmpty(memberName))
                return false;
            if (overrides.Contains(memberName)) return true;
            return overrides.Contains("m_" + char.ToUpperInvariant(memberName[0]) +
                                      memberName.Substring(1));
        }

        public static HashSet<string> TopLevelOverrides(Object target)
        {
            var set = new HashSet<string>();
            if (target == null) return set;

            var prop = new SerializedObject(target).GetIterator();
            if (!prop.NextVisible(true)) return set;
            do
            {
                if (IsMeaningfulOverride(prop)) set.Add(prop.name);
            } while (prop.NextVisible(false));

            return set;
        }

        /// <summary>
        /// 這顆 object 的繼承來源描述；不是任何 prefab 的 instance 就回 null（呼叫端不要印那一行）。
        /// 回的是**檔名**不是完整路徑 —— 這一行的用途是「認出來源是誰」，路徑會把輸出撐爆。
        /// </summary>
        public static string SourceLabel(Object target)
        {
            if (target == null) return null;

            var source = PrefabUtility.GetCorrespondingObjectFromSource(target);
            if (source == null) return null;

            var assetPath = AssetDatabase.GetAssetPath(source);
            var label = string.IsNullOrEmpty(assetPath)
                ? source.name
                : Path.GetFileNameWithoutExtension(assetPath);

            // nested prefab instance 的話要講出是哪一顆節點在當 instance root，
            // 不然「來源是 X」看起來像整顆 prefab 都繼承自 X。
            var go = target as GameObject ?? (target as Component)?.gameObject;
            if (go != null)
            {
                var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                if (instanceRoot != null && instanceRoot != go.transform.root.gameObject)
                    return $"{label}（nested instance root: {instanceRoot.name}）";
            }

            return label;
        }
    }
}
