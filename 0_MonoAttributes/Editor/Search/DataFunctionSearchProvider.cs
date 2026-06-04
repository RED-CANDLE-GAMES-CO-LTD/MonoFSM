#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Editor;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace MonoFSM.Core.Editor.Search
{
    /// <summary>
    /// Unity Search Provider，用於搜尋含有特定 DataFunction 的 GameData
    /// 使用方式：在 Unity Search 視窗輸入 "df:PickableData" 或 "df:ScoreData"
    /// </summary>
    public static class DataFunctionSearchProvider
    {
        private const string ProviderId = "datafunction";
        private const string ProviderName = "DataFunction";

        private static Dictionary<string, Type> _dataFunctionTypeCache;
        private static List<GameData> _allGameDataCache;
        private static bool _cacheInitialized;

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(ProviderId, ProviderName)
            {
                active = true,
                priority = 50,
                filterId = "df:",
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions,
                fetchItems = FetchItems,
                fetchDescription = FetchDescription,
                fetchThumbnail = FetchThumbnail,
                fetchPreview = FetchPreview,
                trackSelection = TrackSelection,
                toObject = ToObject,
            };
        }

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            // 解析搜尋關鍵字
            var searchText = context.searchQuery.Trim();

            // 移除 "df:" 前綴（如果有的話）
            if (searchText.StartsWith("df:", StringComparison.OrdinalIgnoreCase))
                searchText = searchText.Substring(3).Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                // 沒有輸入時，顯示所有可用的 DataFunction 類型
                foreach (var typeName in GetAllDataFunctionTypeNames())
                {
                    yield return provider.CreateItem(
                        context,
                        $"type_{typeName}",
                        0,
                        $"輸入 {typeName} 來搜尋",
                        $"所有含有 {typeName} 的 GameData",
                        null,
                        null
                    );
                }
                yield break;
            }

            // 根據輸入找到匹配的 DataFunction 類型
            var matchedTypes = FindMatchingDataFunctionTypes(searchText);

            if (!matchedTypes.Any())
            {
                yield return provider.CreateItem(
                    context,
                    "no_type",
                    0,
                    $"找不到 DataFunction 類型：{searchText}",
                    "請確認類型名稱是否正確",
                    null,
                    null
                );
                yield break;
            }

            // 搜尋含有這些 DataFunction 的 GameData
            var allGameData = GetAllGameData();
            var score = 0;

            foreach (var gameData in allGameData)
            {
                if (gameData == null)
                    continue;

                foreach (var dfType in matchedTypes)
                {
                    if (GameDataEditorUtility.HasDataFunction(gameData, dfType))
                    {
                        var path = AssetDatabase.GetAssetPath(gameData);
                        yield return provider.CreateItem(
                            context,
                            path,
                            score++,
                            gameData.name,
                            $"{dfType.Name} | {path}",
                            null,
                            gameData
                        );
                        break; // 一個 GameData 只加一次
                    }
                }
            }
        }

        private static string FetchDescription(SearchItem item, SearchContext context)
        {
            if (item.data is GameData gameData)
            {
                var dataFunctionNames = GetDataFunctionNames(gameData);
                return $"DataFunctions: {string.Join(", ", dataFunctionNames)}";
            }
            return item.description;
        }

        private static Texture2D FetchThumbnail(SearchItem item, SearchContext context)
        {
            if (item.data is GameData gameData)
            {
                return AssetPreview.GetMiniThumbnail(gameData);
            }
            return null;
        }

        private static Texture2D FetchPreview(SearchItem item, SearchContext context, Vector2 size, FetchPreviewOptions options)
        {
            if (item.data is GameData gameData)
            {
                return AssetPreview.GetAssetPreview(gameData);
            }
            return null;
        }

        private static void TrackSelection(SearchItem item, SearchContext context)
        {
            if (item.data is GameData gameData)
            {
                EditorGUIUtility.PingObject(gameData);
            }
        }

        private static UnityEngine.Object ToObject(SearchItem item, Type type)
        {
            return item.data as UnityEngine.Object;
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> CreateActions()
        {
            yield return new SearchAction(ProviderId, "select", null, "選擇")
            {
                handler = (item) =>
                {
                    if (item.data is GameData gameData)
                    {
                        Selection.activeObject = gameData;
                        EditorGUIUtility.PingObject(gameData);
                    }
                }
            };

            yield return new SearchAction(ProviderId, "open", null, "在 Inspector 中開啟")
            {
                handler = (item) =>
                {
                    if (item.data is GameData gameData)
                    {
                        Selection.activeObject = gameData;
                    }
                }
            };
        }

        #region Helper Methods

        private static void InitializeCache()
        {
            if (_cacheInitialized)
                return;

            _dataFunctionTypeCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            // 找出所有繼承自 AbstractDataFunction 的類型
            var baseType = typeof(AbstractDataFunction);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (!type.IsAbstract && baseType.IsAssignableFrom(type))
                        {
                            _dataFunctionTypeCache[type.Name] = type;
                        }
                    }
                }
                catch
                {
                    // 忽略無法載入的 assembly
                }
            }

            _cacheInitialized = true;
        }

        private static IEnumerable<string> GetAllDataFunctionTypeNames()
        {
            InitializeCache();
            return _dataFunctionTypeCache.Keys.OrderBy(k => k);
        }

        private static IEnumerable<Type> FindMatchingDataFunctionTypes(string searchText)
        {
            InitializeCache();

            // 完全匹配
            if (_dataFunctionTypeCache.TryGetValue(searchText, out var exactMatch))
            {
                yield return exactMatch;
                yield break;
            }

            // 模糊匹配
            foreach (var kvp in _dataFunctionTypeCache)
            {
                if (kvp.Key.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    yield return kvp.Value;
                }
            }
        }

        private static List<GameData> GetAllGameData()
        {
            if (_allGameDataCache != null)
                return _allGameDataCache;

            var guids = AssetDatabase.FindAssets("t:GameData");
            _allGameDataCache = new List<GameData>(guids.Length);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var gameData = AssetDatabase.LoadAssetAtPath<GameData>(path);
                if (gameData != null)
                    _allGameDataCache.Add(gameData);
            }

            return _allGameDataCache;
        }

        private static IEnumerable<string> GetDataFunctionNames(GameData gameData)
        {
            if (gameData.DataFunctions != null)
            {
                foreach (var df in gameData.DataFunctions)
                {
                    if (df != null)
                        yield return df.GetType().Name;
                }
            }
        }

        /// <summary>
        /// 清除快取（當 GameData 有變更時呼叫）
        /// </summary>
        [MenuItem("Tools/MonoFSM/Clear DataFunction Search Cache")]
        public static void ClearCache()
        {
            _allGameDataCache = null;
            _cacheInitialized = false;
            Debug.Log("[DataFunctionSearch] Cache cleared");
        }

        #endregion
    }
}
#endif
