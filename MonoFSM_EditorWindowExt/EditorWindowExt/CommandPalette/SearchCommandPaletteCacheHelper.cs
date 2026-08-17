#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CommandPalette
{
    /// <summary>
    ///     搜尋命令面板快取輔助工具
    /// </summary>
    public static class SearchCommandPaletteCacheHelper
    {
        // 檔案快取路徑
        private static readonly string CacheDirectory = Path.Combine("Library", "CommandPalette");

        public static void SaveCacheToFile(List<AssetEntry> assets, string cacheFilePath)
        {
            // try
            // {
            if (!Directory.Exists(CacheDirectory))
                Directory.CreateDirectory(CacheDirectory);

            var cacheData = new CacheContainer
            {
                cacheTimestamp = DateTime.Now.Ticks,
                assets = assets
                    .Select(asset =>
                    {
                        var fileInfo = new FileInfo(asset.path);
                        return new AssetCacheData(
                            asset.name,
                            asset.path,
                            asset.guid,
                            asset.GetType().AssemblyQualifiedName,
                            fileInfo.Exists ? fileInfo.LastWriteTime.Ticks : 0
                        );
                    })
                    .ToList(),
            };

            var json = JsonUtility.ToJson(cacheData, true);
            File.WriteAllText(cacheFilePath, json);
            Debug.Log($"[CommandPalette] 快取已儲存至 {cacheFilePath}，包含 {assets.Count} 個資源");
            // }
            // catch (Exception e)
            // {
            //     Debug.LogError($"[CommandPalette] 儲存快取失敗: {e.StackTrace}");
            // }
        }

        public static List<AssetEntry> LoadCacheFromFile(string cacheFilePath, SearchMode mode)
        {
            // try
            // {

            Debug.Log("[CommandPalette] 嘗試載入快取檔案: " + cacheFilePath);
            Application.OpenURL("file://" + cacheFilePath);
            if (!File.Exists(cacheFilePath))
            {
                Debug.Log($"[CommandPalette] 快取檔案不存在: {cacheFilePath}");
                return null;
            }

            var stopwatch = Stopwatch.StartNew();
            var json = File.ReadAllText(cacheFilePath);
            var cacheData = JsonUtility.FromJson<CacheContainer>(json);

            if (cacheData?.assets == null)
            {
                Debug.Log($"[CommandPalette] 快取資料無效或為空: {cacheFilePath}");
                return null;
            }

            var assets = new List<AssetEntry>();
            var outdatedAssets = new List<AssetCacheData>();
            var outdatedCount = 0;
            var loadedCount = 0;
            var updatedAssets = new List<AssetEntry>();
            var cacheNeedsUpdate = false;

            foreach (var cachedAsset in cacheData.assets)
            {
                // 檢查檔案是否仍存在
                if (!File.Exists(cachedAsset.path))
                {
                    outdatedCount++;
                    cacheNeedsUpdate = true;
                    continue;
                }

                // 檢查檔案修改時間
                var fileInfo = new FileInfo(cachedAsset.path);
                if (fileInfo.LastWriteTime.Ticks != cachedAsset.lastModified)
                {
                    // 檔案已更新，但我們不需要立即載入資源，只需要更新快取資料
                    var updatedCacheData = new AssetCacheData(
                        cachedAsset.name,
                        cachedAsset.path,
                        cachedAsset.guid,
                        cachedAsset.typeName,
                        fileInfo.LastWriteTime.Ticks
                    );

                    var updatedEntry = new AssetEntry(updatedCacheData);
                    assets.Add(updatedEntry);
                    updatedAssets.Add(updatedEntry);
                    loadedCount++;
                    cacheNeedsUpdate = true;
                    Debug.Log($"[CommandPalette] 已更新過期資源快取資料: {cachedAsset.path}");
                    continue;
                }

                // 直接從快取資料建立 AssetEntry，不載入實際資源
                assets.Add(new AssetEntry(cachedAsset));
                loadedCount++;
            }

            // 如果有過期的資源被更新，重新儲存快取
            if (cacheNeedsUpdate && updatedAssets.Count > 0)
                try
                {
                    SaveCacheToFile(assets, cacheFilePath);
                    Debug.Log(
                        $"[CommandPalette] 快取已自動更新，包含 {updatedAssets.Count} 個更新的資源"
                    );
                }
                catch (Exception updateEx)
                {
                    Debug.LogWarning($"[CommandPalette] 自動更新快取失敗: {updateEx.Message}");
                }

            stopwatch.Stop();
            var statusMessage =
                updatedAssets.Count > 0
                    ? $"[CommandPalette] 快取載入完成，有效: {loadedCount} 個，過期: {outdatedCount} 個，已更新: {updatedAssets.Count} 個，耗時 {stopwatch.ElapsedMilliseconds}ms"
                    : $"[CommandPalette] 快取載入完成，有效: {loadedCount} 個，過期: {outdatedCount} 個，耗時 {stopwatch.ElapsedMilliseconds}ms";
            Debug.Log(statusMessage);

            return assets.Count > 0 ? assets : null;
            // }
            // catch (Exception e)
            // {
            //     Debug.LogError($"[CommandPalette] 載入快取失敗: {e.Message}");
            //     return null;
            // }
        }

        /// <summary>
        /// 收集所有Unity MenuItem
        /// </summary>
        public static List<MenuItemEntry> CollectAllMenuItems()
        {
            var menuItems = new List<MenuItemEntry>();

            try
            {
                var stopwatch = Stopwatch.StartNew();

                // 收集多個選單的MenuItem
                string[] menuCategories = { "Tools", "Window", "GameObject" };

                foreach (var menuCategory in menuCategories)
                {
                    var menuItemsArray = Menu.GetMenuItems(menuCategory, false, false);

                    if (menuItemsArray != null)
                    {
                        foreach (var item in menuItemsArray)
                        {
                            // Debug.Log($"[CommandPalette] 收集MenuItem: {item.path}");
                            // if (string.IsNullOrEmpty(menuPath)) continue;
                            var menuPath = item.path;

                            // 解析MenuItem資訊
                            var displayName = GetDisplayNameFromMenuPath(menuPath);
                            var category = GetCategoryFromMenuPath(menuPath);

                            // 驗證MenuItem是否可執行
                            var isValidated = ValidateMenuItem(menuPath);

                            menuItems.Add(
                                new MenuItemEntry(menuPath, displayName, category, isValidated)
                            );
                        }
                    }
                }

                stopwatch.Stop();
                // Debug.Log(
                //     $"[CommandPalette] MenuItem收集完成，共 {menuItems.Count} 個，耗時 {stopwatch.ElapsedMilliseconds}ms"
                // );
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommandPalette] MenuItem收集失敗: {e.Message}");
            }

            return menuItems;
        }

        /// <summary>
        /// 從MenuPath提取顯示名稱
        /// </summary>
        private static string GetDisplayNameFromMenuPath(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
                return "";

            // 移除快捷鍵部分 (例如: "File/New Scene %n" -> "File/New Scene")
            var cleanPath = menuPath;
            var shortcutIndex = cleanPath.LastIndexOf(' ');
            if (shortcutIndex > 0 && cleanPath.Length > shortcutIndex + 1)
            {
                var possibleShortcut = cleanPath.Substring(shortcutIndex + 1);
                if (
                    possibleShortcut.Contains('%')
                    || possibleShortcut.Contains('#')
                    || possibleShortcut.Contains('&')
                )
                {
                    cleanPath = cleanPath.Substring(0, shortcutIndex);
                }
            }

            // 取得最後一段作為顯示名稱
            var lastSlash = cleanPath.LastIndexOf('/');
            return lastSlash >= 0 ? cleanPath.Substring(lastSlash + 1) : cleanPath;
        }

        /// <summary>
        /// 從MenuPath提取類別
        /// </summary>
        private static string GetCategoryFromMenuPath(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
                return "Unknown";

            var firstSlash = menuPath.IndexOf('/');
            return firstSlash >= 0 ? menuPath.Substring(0, firstSlash) : "Root";
        }

        /// <summary>
        /// 驗證MenuItem是否可執行
        /// </summary>
        private static bool ValidateMenuItem(string menuPath)
        {
            try
            {
                // 某些MenuItem可能無法被ExecuteMenuItem執行，這裡做基本檢查
                if (string.IsNullOrEmpty(menuPath))
                    return false;

                // 已知的無法執行的MenuItem前綴
                var blacklistedPrefixes = new[]
                {
                    "CONTEXT/",
                    "Edit/Preferences",
                    "Edit/Project Settings",
                };

                foreach (var prefix in blacklistedPrefixes)
                {
                    if (menuPath.StartsWith(prefix))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 儲存MenuItem快取到檔案
        /// </summary>
        public static void SaveMenuItemCacheToFile(
            List<MenuItemEntry> menuItems,
            string cacheFilePath
        )
        {
            try
            {
                if (!Directory.Exists(CacheDirectory))
                    Directory.CreateDirectory(CacheDirectory);

                var cacheData = new MenuItemCacheContainer
                {
                    cacheTimestamp = DateTime.Now.Ticks,
                    menuItems = menuItems
                        .Select(item => new MenuItemCacheData(
                            item.menuPath,
                            item.displayName,
                            item.category,
                            item.isValidated,
                            item.isEnabled
                        ))
                        .ToList(),
                };

                var json = JsonUtility.ToJson(cacheData, true);
                File.WriteAllText(cacheFilePath, json);
                Debug.Log(
                    $"[CommandPalette] MenuItem快取已儲存至 {cacheFilePath}，包含 {menuItems.Count} 個項目"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommandPalette] 儲存MenuItem快取失敗: {e.Message}");
            }
        }

        /// <summary>
        /// 從檔案載入MenuItem快取
        /// </summary>
        public static List<MenuItemEntry> LoadMenuItemCacheFromFile(string cacheFilePath)
        {
            try
            {
                if (!File.Exists(cacheFilePath))
                    return null;

                var stopwatch = Stopwatch.StartNew();
                var json = File.ReadAllText(cacheFilePath);
                var cacheData = JsonUtility.FromJson<MenuItemCacheContainer>(json);

                if (cacheData?.menuItems == null)
                    return null;

                var menuItems = cacheData
                    .menuItems.Select(cached => new MenuItemEntry(cached))
                    .ToList();

                stopwatch.Stop();
                Debug.Log(
                    $"[CommandPalette] MenuItem快取載入完成，共 {menuItems.Count} 個項目，耗時 {stopwatch.ElapsedMilliseconds}ms"
                );

                return menuItems;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommandPalette] 載入MenuItem快取失敗: {e.Message}");
                return null;
            }
        }
    }
}
#endif
