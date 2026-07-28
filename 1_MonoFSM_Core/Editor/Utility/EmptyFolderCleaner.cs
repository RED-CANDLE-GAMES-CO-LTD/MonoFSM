using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 清除空資料夾（只含 .meta / .DS_Store 這類系統垃圾檔、也沒有任何非空子資料夾的資料夾）。
/// 會反覆掃描直到沒有可刪的空資料夾為止，因此刪掉子資料夾後變空的父資料夾也會一併清掉。
///
/// 掃描直接走檔案系統（不靠 AssetDatabase.GetAllAssetPaths），所以連 Unity 不 import 的
/// 隱藏資料夾（`~` 結尾、`.` 開頭）內部的空資料夾也抓得到。
/// 「包含本機 Packages」toggle 打開時，會一併清理 manifest 裡 local / embedded package
/// 的實體目錄（例如 MonoFSM、MonoFSM-Pro，注意這會動到 submodule 的 git 狀態）。
/// </summary>
public static class EmptyFolderCleaner
{
    private const string CleanMenu = "Tools/清理空資料夾/清理空資料夾 (Clear Empty Folders)";
    private const string IncludePackagesMenu = "Tools/清理空資料夾/包含本機 Packages (會動到 submodule)";
    private const string IncludePackagesPrefKey = "EmptyFolderCleaner.IncludePackages";

    /// <summary>不算檔案的垃圾檔名（Unity 也不當 asset）</summary>
    private static readonly string[] IgnoredFileNames = { ".DS_Store", "Thumbs.db", "desktop.ini" };

    /// <summary>絕對不進去掃的資料夾名稱</summary>
    private static readonly string[] SkippedFolderNames = { ".git", ".svn", ".idea", ".vs", "node_modules" };

    private static bool IncludePackages
    {
        get => EditorPrefs.GetBool(IncludePackagesPrefKey, false);
        set => EditorPrefs.SetBool(IncludePackagesPrefKey, value);
    }

    [MenuItem(IncludePackagesMenu, false, 10)]
    private static void ToggleIncludePackages()
    {
        IncludePackages = !IncludePackages;
        Menu.SetChecked(IncludePackagesMenu, IncludePackages);
    }

    [MenuItem(IncludePackagesMenu, true, 10)]
    private static bool ToggleIncludePackagesValidate()
    {
        Menu.SetChecked(IncludePackagesMenu, IncludePackages);
        return true;
    }

    [MenuItem(CleanMenu, false, 20)]
    public static void ClearEmptyFolders()
    {
        var roots = GetScanRoots();
        var emptyFolders = FindEmptyFolders(roots);

        if (emptyFolders.Count == 0)
        {
            EditorUtility.DisplayDialog("清理空資料夾", "沒有找到空資料夾。", "OK");
            return;
        }

        var preview = string.Join("\n", emptyFolders.Take(30).Select(f => f.DisplayPath));
        if (emptyFolders.Count > 30)
            preview += $"\n... 以及其他 {emptyFolders.Count - 30} 個";

        var scope = IncludePackages ? "Assets + 本機 Packages" : "只有 Assets";
        var ok = EditorUtility.DisplayDialog(
            "清理空資料夾",
            $"掃描範圍：{scope}\n將刪除 {emptyFolders.Count} 個空資料夾：\n\n{preview}",
            "刪除",
            "取消");

        if (!ok)
            return;

        var deleted = 0;
        foreach (var folder in emptyFolders)
        {
            if (Delete(folder))
                deleted++;
            else
                Debug.LogWarning($"[EmptyFolderCleaner] 無法刪除：{folder.DisplayPath}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"[EmptyFolderCleaner] 已刪除 {deleted} 個空資料夾。");
    }

    /// <summary>掃描根：Assets，以及（toggle 開啟時）manifest 裡的 local / embedded package 實體目錄。</summary>
    private static List<ScanRoot> GetScanRoots()
    {
        var roots = new List<ScanRoot>
        {
            new(Path.GetFullPath("Assets"), "Assets")
        };

        if (!IncludePackages)
            return roots;

        foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
        {
            // registry / git package 在 Library/PackageCache 底下是唯讀快取，不要動
            if (package.source != UnityEditor.PackageManager.PackageSource.Local &&
                package.source != UnityEditor.PackageManager.PackageSource.Embedded)
                continue;

            var resolved = package.resolvedPath;
            if (string.IsNullOrEmpty(resolved) || !Directory.Exists(resolved))
                continue;

            roots.Add(new ScanRoot(Path.GetFullPath(resolved), $"Packages/{package.name}"));
        }

        return roots;
    }

    /// <summary>
    /// 反覆掃描，收集所有需要刪除的空資料夾（由深到淺排序，確保刪除順序安全）。
    /// </summary>
    private static List<FolderEntry> FindEmptyFolders(List<ScanRoot> roots)
    {
        var candidates = new List<FolderEntry>();
        foreach (var root in roots)
            Collect(root, root._fullPath, candidates);

        var toDelete = new HashSet<string>();
        bool foundNew;
        do
        {
            foundNew = false;
            foreach (var folder in candidates)
            {
                if (toDelete.Contains(folder._fullPath))
                    continue;
                if (!IsEmpty(folder._fullPath, toDelete))
                    continue;

                toDelete.Add(folder._fullPath);
                foundNew = true;
            }
        } while (foundNew);

        // 由深到淺排序（路徑長的先刪），避免父先於子刪除
        return candidates
            .Where(f => toDelete.Contains(f._fullPath))
            .OrderByDescending(f => f._fullPath.Length)
            .ToList();
    }

    /// <summary>遞迴收集 root 底下所有資料夾（root 本身不列入刪除候選）。</summary>
    private static void Collect(ScanRoot root, string currentFullPath, List<FolderEntry> result)
    {
        string[] subFolders;
        try
        {
            subFolders = Directory.GetDirectories(currentFullPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[EmptyFolderCleaner] 無法讀取 {currentFullPath}：{e.Message}");
            return;
        }

        foreach (var sub in subFolders)
        {
            var name = Path.GetFileName(sub);
            if (SkippedFolderNames.Contains(name))
                continue;

            // 以 . 開頭的資料夾本身不刪（版控 / 工具用），但仍進去掃裡面
            var deletable = !name.StartsWith(".", StringComparison.Ordinal);
            if (deletable)
                result.Add(new FolderEntry(sub.Replace('\\', '/'), root));

            Collect(root, sub, result);
        }
    }

    /// <summary>
    /// 判斷資料夾是否為空：沒有任何 asset 檔（.meta 與系統垃圾檔不算），
    /// 且所有子資料夾都已被標記為待刪。
    /// </summary>
    private static bool IsEmpty(string fullPath, HashSet<string> alreadyMarked)
    {
        if (!Directory.Exists(fullPath))
            return false;

        if (Directory.GetFiles(fullPath).Any(f => !IsIgnorableFile(f)))
            return false;

        foreach (var sub in Directory.GetDirectories(fullPath))
        {
            if (!alreadyMarked.Contains(sub.Replace('\\', '/')))
                return false;
        }

        return true;
    }

    private static bool IsIgnorableFile(string filePath)
    {
        var name = Path.GetFileName(filePath);
        if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            return true;
        if (name.StartsWith(".", StringComparison.Ordinal))
            return true;
        return IgnoredFileNames.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AssetDatabase 認得的路徑走 DeleteAsset（保留 undo / VCS 整合），
    /// Unity 不 import 的（`~` 結尾等）直接刪目錄與對應 .meta。
    /// </summary>
    private static bool Delete(FolderEntry folder)
    {
        var assetPath = folder.AssetPath;
        if (assetPath != null && AssetDatabase.IsValidFolder(assetPath))
            return AssetDatabase.DeleteAsset(assetPath);

        try
        {
            Directory.Delete(folder._fullPath, false);
            var meta = folder._fullPath + ".meta";
            if (File.Exists(meta))
                File.Delete(meta);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[EmptyFolderCleaner] 刪除失敗 {folder.DisplayPath}：{e.Message}");
            return false;
        }
    }

    private readonly struct ScanRoot
    {
        public readonly string _fullPath;

        /// <summary>對應的 AssetDatabase 路徑前綴，例如 "Assets" 或 "Packages/com.monofsm.core"</summary>
        public readonly string _assetPathPrefix;

        public ScanRoot(string fullPath, string assetPathPrefix)
        {
            _fullPath = fullPath.Replace('\\', '/');
            _assetPathPrefix = assetPathPrefix;
        }
    }

    private readonly struct FolderEntry
    {
        public readonly string _fullPath;
        private readonly ScanRoot _root;

        public FolderEntry(string fullPath, ScanRoot root)
        {
            _fullPath = fullPath;
            _root = root;
        }

        /// <summary>AssetDatabase 路徑；若路徑中有 Unity 不 import 的段（`~` 結尾 / `.` 開頭）則為 null。</summary>
        public string AssetPath
        {
            get
            {
                var relative = _fullPath.Substring(_root._fullPath.Length).TrimStart('/');
                var segments = relative.Split('/');
                if (segments.Any(s => s.EndsWith("~", StringComparison.Ordinal) ||
                                      s.StartsWith(".", StringComparison.Ordinal)))
                    return null;

                return $"{_root._assetPathPrefix}/{relative}";
            }
        }

        public string DisplayPath => AssetPath ?? _fullPath;
    }
}
