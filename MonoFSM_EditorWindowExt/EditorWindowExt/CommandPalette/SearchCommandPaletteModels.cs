#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CommandPalette
{
    /// <summary>
    ///     搜尋模式枚舉
    /// </summary>
    public enum SearchMode
    {
        Prefabs,
        ScriptableObjects,
        Scenes,
        MenuItems,
        Windows
    }

    /// <summary>
    ///     排序模式：ScoreBased = 權重分數排序；Alphabetical = 過濾後字典排序（Unity Project 風格）
    /// </summary>
    public enum SearchSortMode
    {
        ScoreBased,
        Alphabetical
    }

    /// <summary>
    ///     資源類型定義
    /// </summary>
    public class AssetTypeDefinition
    {
        public SearchMode Mode { get; set; }
        public string DisplayName { get; set; }
        public string FileExtension { get; set; }
        public string AssetDatabaseFilter { get; set; }
        public System.Type UnityType { get; set; }
        public string CacheFileName { get; set; }

        public AssetTypeDefinition(SearchMode mode, string displayName, string fileExtension, 
            string assetDatabaseFilter, System.Type unityType, string cacheFileName)
        {
            Mode = mode;
            DisplayName = displayName;
            FileExtension = fileExtension;
            AssetDatabaseFilter = assetDatabaseFilter;
            UnityType = unityType;
            CacheFileName = cacheFileName;
        }
    }

    /// <summary>
    ///     資源快取資料結構
    /// </summary>
    [Serializable]
    public class AssetCacheData
    {
        public string name;
        public string path;
        public string guid;
        public string typeName;
        public long lastModified;

        public AssetCacheData(string name, string path, string guid, string typeName, long lastModified)
        {
            this.name = name;
            this.path = path;
            this.guid = guid;
            this.typeName = typeName;
            this.lastModified = lastModified;
        }
    }

    /// <summary>
    ///     快取容器結構
    /// </summary>
    [Serializable]
    public class CacheContainer
    {
        public List<AssetCacheData> assets = new();
        public long cacheTimestamp;
    }

    /// <summary>
    ///     資源條目結構
    /// </summary>
    [Serializable]
    public class AssetEntry
    {
        public string name;
        public string path;
        public string guid;
        public bool iconLoaded;

        // 延遲載入的欄位
        private Object _asset;
        private Texture _icon;

        // 建構子：輕量建立，不載入 asset
        public AssetEntry(string name, string path, string guid)
        {
            this.name = name;
            this.path = path;
            this.guid = guid;
            this.iconLoaded = false;
            this._asset = null;
        }

        // 建構子：從快取資料建立
        public AssetEntry(AssetCacheData cacheData)
        {
            this.name = cacheData.name;
            this.path = cacheData.path;
            this.guid = cacheData.guid;
            this.iconLoaded = false;
            this._asset = null;
        }

        // 延遲載入資源（只在真正需要時才載入）
        public Object asset
        {
            get
            {
                if (_asset == null)
                {
                    _asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                }
                return _asset;
            }
        }

        // 使用 GetCachedIcon 取得圖標，不需要載入 asset
        public Texture icon
        {
            get
            {
                if (!iconLoaded)
                {
                    _icon = AssetDatabase.GetCachedIcon(path);
                    iconLoaded = true;
                }
                return _icon;
            }
        }
    }

    /// <summary>
    ///     MenuItem快取資料結構
    /// </summary>
    [Serializable]
    public class MenuItemCacheData
    {
        public string menuPath;
        public string displayName;
        public string category;
        public bool isValidated;
        public bool isEnabled;

        public MenuItemCacheData(string menuPath, string displayName, string category, bool isValidated, bool isEnabled)
        {
            this.menuPath = menuPath;
            this.displayName = displayName;
            this.category = category;
            this.isValidated = isValidated;
            this.isEnabled = isEnabled;
        }
    }

    /// <summary>
    ///     MenuItem條目結構
    /// </summary>
    [Serializable]
    public class MenuItemEntry
    {
        public string menuPath;
        public string displayName;
        public string category;
        public bool isValidated;
        public bool isEnabled;
        
        public MenuItemEntry(string menuPath, string displayName, string category, bool isValidated = true, bool isEnabled = true)
        {
            this.menuPath = menuPath;
            this.displayName = displayName;
            this.category = category;
            this.isValidated = isValidated;
            this.isEnabled = isEnabled;
        }

        public MenuItemEntry(MenuItemCacheData cacheData)
        {
            this.menuPath = cacheData.menuPath;
            this.displayName = cacheData.displayName;
            this.category = cacheData.category;
            this.isValidated = cacheData.isValidated;
            this.isEnabled = cacheData.isEnabled;
        }

        public void Execute()
        {
            if (!string.IsNullOrEmpty(menuPath) && isEnabled)
            {
                EditorApplication.ExecuteMenuItem(menuPath);
            }
        }
    }

    /// <summary>
    ///     MenuItem快取容器結構
    /// </summary>
    [Serializable]
    public class MenuItemCacheContainer
    {
        public List<MenuItemCacheData> menuItems = new();
        public long cacheTimestamp;
    }

    /// <summary>
    ///     EditorWindow 條目結構
    /// </summary>
    public class EditorWindowEntry
    {
        public Type Type { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }  // Unity / MonoFSM / Third Party

        public EditorWindowEntry(Type type, string displayName, string category)
        {
            Type = type;
            DisplayName = displayName;
            Category = category;
        }
    }
}
#endif