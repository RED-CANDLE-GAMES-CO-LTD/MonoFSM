using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 清除 Assets 底下的空資料夾（只含 .meta 或完全沒東西、也沒有任何子資料夾的資料夾）。
/// 會反覆掃描直到沒有可刪的空資料夾為止，因此刪掉子資料夾後變空的父資料夾也會一併清掉。
/// </summary>
public static class EmptyFolderCleaner
{
    private const string RootPath = "Assets";

    [MenuItem("Tools/清理空資料夾 (Clear Empty Folders)")]
    public static void ClearEmptyFolders()
    {
        var emptyFolders = FindEmptyFolders();

        if (emptyFolders.Count == 0)
        {
            EditorUtility.DisplayDialog("清理空資料夾", "沒有找到空資料夾。", "OK");
            return;
        }

        var preview = string.Join("\n", emptyFolders.Take(30));
        if (emptyFolders.Count > 30)
            preview += $"\n... 以及其他 {emptyFolders.Count - 30} 個";

        var ok = EditorUtility.DisplayDialog(
            "清理空資料夾",
            $"將刪除 {emptyFolders.Count} 個空資料夾：\n\n{preview}",
            "刪除",
            "取消");

        if (!ok)
            return;

        var deleted = 0;
        foreach (var folder in emptyFolders)
        {
            if (AssetDatabase.DeleteAsset(folder))
                deleted++;
            else
                Debug.LogWarning($"[EmptyFolderCleaner] 無法刪除：{folder}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"[EmptyFolderCleaner] 已刪除 {deleted} 個空資料夾。");
    }

    /// <summary>
    /// 反覆掃描，收集所有需要刪除的空資料夾（由深到淺排序，確保刪除順序安全）。
    /// </summary>
    private static List<string> FindEmptyFolders()
    {
        var toDelete = new HashSet<string>();

        bool foundNew;
        do
        {
            foundNew = false;
            var allFolders = AssetDatabase.GetAllAssetPaths()
                .Where(AssetDatabase.IsValidFolder)
                .Where(p => p.StartsWith(RootPath) && !toDelete.Contains(p));

            foreach (var folder in allFolders)
            {
                if (IsEmpty(folder, toDelete))
                {
                    toDelete.Add(folder);
                    foundNew = true;
                }
            }
        } while (foundNew);

        // 由深到淺排序（路徑長的先刪），避免父先於子刪除
        return toDelete.OrderByDescending(p => p.Length).ToList();
    }

    /// <summary>
    /// 判斷資料夾是否為空：沒有任何 asset 檔（.meta 不算），
    /// 且所有子資料夾都已被標記為待刪。
    /// </summary>
    private static bool IsEmpty(string folder, HashSet<string> alreadyMarked)
    {
        var fullPath = Path.GetFullPath(folder);
        if (!Directory.Exists(fullPath))
            return false;

        // 有任何非 .meta 檔案 → 不是空的
        var hasFile = Directory.GetFiles(fullPath)
            .Any(f => !f.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase));
        if (hasFile)
            return false;

        // 有任何尚未被標記為待刪的子資料夾 → 還不算空
        var subFolders = Directory.GetDirectories(fullPath);
        foreach (var sub in subFolders)
        {
            var assetPath = FileUtil.GetProjectRelativePath(sub.Replace('\\', '/'));
            if (string.IsNullOrEmpty(assetPath) || !alreadyMarked.Contains(assetPath))
                return false;
        }

        return true;
    }
}
