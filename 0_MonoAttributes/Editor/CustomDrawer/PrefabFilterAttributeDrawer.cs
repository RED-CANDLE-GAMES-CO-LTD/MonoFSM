using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _1_MonoFSM_Core.Runtime.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core.Editor
{
    /// <summary>
    /// PrefabFilterAttribute 的 OdinAttributeDrawer
    /// 用於過濾帶有特定Component的Prefab
    /// </summary>
    public class PrefabFilterAttributeDrawer : OdinAttributeDrawer<PrefabFilterAttribute>
    {
        protected override bool CanDrawAttributeProperty(InspectorProperty property)
        {
            return property.ValueEntry != null
                && typeof(MonoBehaviour).IsAssignableFrom(property.ValueEntry.TypeOfValue);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var currentComponent = Property.ValueEntry.WeakSmartValue as MonoBehaviour;

            // var currentPrefab = currentComponent?.gameObject;
            //
            // // 驗證當前選中的Prefab是否符合過濾條件
            // if (currentPrefab != null && !Attribute.ValidatePrefab(currentPrefab))
            // {
            //     var warningMessage = !string.IsNullOrEmpty(Attribute.CustomErrorMessage)
            //         ? Attribute.CustomErrorMessage
            //         : GetDefaultWarningMessage(currentPrefab);
            //
            //     SirenixEditorGUI.WarningMessageBox(warningMessage);
            // }

            // 繪製帶過濾功能的選擇器
            DrawFilteredSelector(label, currentComponent);

            // 設置背景顏色
            GUI.backgroundColor =
                Property.ValueEntry.WeakSmartValue == null
                    ? new Color(0.2f, 0.2f, 0.3f, 0.1f)
                    : new Color(0.1f, 0.3f, 0.2f, 0.2f);

            var newComponent =
                SirenixEditorFields.UnityObjectField(
                    currentComponent,
                    Property.ValueEntry.TypeOfValue,
                    false
                ) as MonoBehaviour;

            Property.ValueEntry.WeakSmartValue = newComponent;
            GUI.backgroundColor = Color.white;
        }

        private void DrawFilteredSelector(GUIContent label, MonoBehaviour currentValue)
        {
            var buttonText = currentValue != null ? currentValue.name : "None";

            using (new GUILayout.HorizontalScope())
            {
                if (label != null)
                    EditorGUILayout.PrefixLabel(label);

                if (
                    SirenixEditorGUI.SDFIconButton(
                        buttonText,
                        16,
                        SdfIconType.CaretDownFill,
                        IconAlignment.RightEdge
                    )
                )
                {
                    var selector = new PrefabFilteredSelector(
                        Attribute,
                        Property.ValueEntry.TypeOfValue
                    );
                    selector.SelectionConfirmed += col =>
                    {
                        var selectedPrefab = col.FirstOrDefault();
                        if (selectedPrefab != null)
                        {
                            // 從選中的 Prefab 中獲取對應類型的 Component
                            var component = selectedPrefab.GetComponent(
                                Property.ValueEntry.TypeOfValue
                            );
                            Property.ValueEntry.WeakSmartValue = component;
                        }
                        else
                        {
                            Property.ValueEntry.WeakSmartValue = null;
                        }
                    };
                    selector.ShowInPopup();
                }
            }
        }

        private string GetDefaultWarningMessage(GameObject prefab)
        {
            if (Attribute.RequiredComponentType == null)
                return $"選中的Prefab '{prefab.name}' 不符合條件";

            var componentName = Attribute.RequiredComponentType.Name;
            return $"選中的Prefab '{prefab.name}' 缺少必要的Component: {componentName}";
        }
    }

    /// <summary>
    /// Prefab過濾選擇器
    /// </summary>
    public class PrefabFilteredSelector : OdinSelector<GameObject>
    {
        private readonly PrefabFilterAttribute _filterAttribute;
        private readonly Type _componentType;

        public PrefabFilteredSelector(
            PrefabFilterAttribute filterAttribute,
            Type componentType = null
        )
        {

            _filterAttribute = filterAttribute;
            _componentType = componentType;
            DrawConfirmSelectionButton = false;
            SelectionTree.Config.SelectMenuItemsOnMouseDown = true;
            SelectionTree.Config.ConfirmSelectionOnDoubleClick = true;
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            tree.Config.DrawSearchToolbar = true;

            // 添加 None 選項
            tree.Add("-- None --", null);

            // 獲取過濾後的Prefab
            var filteredPrefabs = GetFilteredPrefabs().ToList();

            if (!filteredPrefabs.Any())
            {
                tree.Add("無符合條件的Prefab", null);
                return;
            }

            // 按資料夾分組
            var groupedPrefabs = filteredPrefabs
                .Where(prefab => prefab != null)
                .GroupBy(prefab => GetGroupName(prefab))
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in groupedPrefabs)
            {
                var sortedPrefabs = group.OrderBy(prefab => prefab.name).ToList();

                foreach (var prefab in sortedPrefabs)
                {
                    var displayName = prefab.name;
                    var path = group.Key == "Assets" ? displayName : $"{group.Key}/{displayName}";

                    tree.Add(path, prefab);
                }
            }
        }

        private IEnumerable<GameObject> GetFilteredPrefabs()
        {
            var cacheKey = _filterAttribute.RequiredComponentType;

            // 快取命中：只載入已驗證的少數 prefab
            if (PrefabFilterCache.TryGet(cacheKey, out var cachedGuids))
            {
                return cachedGuids
                    .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(p => p != null);
            }

            // 快取未命中：全掃描 + 驗證，結果存入快取
            var searchFolders = new[] { "Assets", "Packages" };
            var allGuids = AssetDatabase.FindAssets("t:Prefab", searchFolders);
            var validGuids = new List<string>();
            var results = new List<GameObject>();

            foreach (var guid in allGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && ValidatePrefab(prefab))
                {
                    validGuids.Add(guid);
                    results.Add(prefab);
                }
            }

            PrefabFilterCache.Set(cacheKey, validGuids);
            return results;
        }

        private bool ValidatePrefab(GameObject prefab)
        {
            if (prefab == null)
                return false;

            // 確保是Prefab而不是場景中的GameObject（已在上層過濾，這裡可以省略）
            // var assetPath = AssetDatabase.GetAssetPath(prefab);
            // if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab"))
            //     return false;

            // 檢查是否包含所需的 Component 類型
            if (_componentType != null)
            {
                var component = prefab.GetComponent(_componentType);
                if (component == null)
                    return false;
            }

            return _filterAttribute.ValidatePrefab(prefab);
        }

        private string GetGroupName(GameObject prefab)
        {
            var assetPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(assetPath))
                return "Unknown";

            var folderPath = Path.GetDirectoryName(assetPath);
            return string.IsNullOrEmpty(folderPath) ? "Assets" : folderPath;
        }
    }
}

/// <summary>
/// Cache file 的 JSON 結構，包含 type 資訊以便 domain reload 後仍可正確更新
/// </summary>
[Serializable]
internal class PrefabGuidCacheFile
{
    public string _typeAssemblyQualifiedName; // null 代表「不過濾」
    public List<string> _guids = new();
}

/// <summary>
/// PrefabFilter 快取：以 Component Type 為 key，存放已驗證通過的 prefab GUID 清單。
/// 同時維護記憶體快取（session 內）與 Library/ 下的 JSON 檔案快取（跨 domain reload）。
/// </summary>
internal static class PrefabFilterCache
{
    private static readonly Dictionary<Type, List<string>> Cache = new();
    private static readonly string CacheDir = Path.Combine("Library", "PrefabFilterCache");

    // ------- 公開 API -------

    /// <summary>先查記憶體，未命中再讀檔案</summary>
    public static bool TryGet(Type type, out List<string> guids)
    {
        if (Cache.TryGetValue(type, out guids))
            return true;

        guids = LoadFromFile(type);
        if (guids != null)
        {
            Cache[type] = guids;
            return true;
        }

        return false;
    }

    /// <summary>掃描完成後存入記憶體與檔案</summary>
    public static void Set(Type type, List<string> guids)
    {
        Cache[type] = guids;
        SaveToFile(type, guids);
    }

    /// <summary>prefab 被刪除時，從記憶體與所有 cache 檔案中移除該 GUID</summary>
    public static void RemoveGuid(string guid)
    {
        foreach (var list in Cache.Values)
            list.Remove(guid);

        // 同步更新所有 cache 檔案（domain reload 後記憶體為空時仍能正確處理）
        UpdateAllFilesRemoveGuid(guid);
    }

    /// <summary>prefab 被新增或修改時，重新驗證並同步記憶體與檔案</summary>
    public static void RevalidatePrefab(string guid, GameObject prefab)
    {
        // 更新記憶體中已載入的 entry
        foreach (var (type, list) in Cache)
            ApplyRevalidation(type, guid, prefab, list);

        // 更新所有 cache 檔案（處理 domain reload 後記憶體為空的情況）
        UpdateAllFilesRevalidate(guid, prefab);
    }

    // ------- 私有：記憶體操作 -------

    private static void ApplyRevalidation(Type type, string guid, GameObject prefab, List<string> list)
    {
        bool shouldBeInCache = type == null || prefab.GetComponent(type) != null;
        bool isInCache = list.Contains(guid);

        if (shouldBeInCache && !isInCache) list.Add(guid);
        else if (!shouldBeInCache && isInCache) list.Remove(guid);
    }

    // ------- 私有：檔案操作 -------

    private static string GetFilePath(Type type)
    {
        // 用 type 的全名做檔名，null type 用 "__all__"
        var name = type == null ? "__all__" : type.FullName?.Replace('.', '_').Replace('+', '_') ?? type.Name;
        return Path.Combine(CacheDir, $"{name}.json");
    }

    private static void SaveToFile(Type type, List<string> guids)
    {
        try
        {
            if (!Directory.Exists(CacheDir))
                Directory.CreateDirectory(CacheDir);

            var data = new PrefabGuidCacheFile
            {
                _typeAssemblyQualifiedName = type?.AssemblyQualifiedName,
                _guids = guids,
            };
            File.WriteAllText(GetFilePath(type), JsonUtility.ToJson(data));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PrefabFilterCache] 儲存快取失敗: {e.Message}");
        }
    }

    private static List<string> LoadFromFile(Type type)
    {
        try
        {
            var path = GetFilePath(type);
            if (!File.Exists(path)) return null;

            var data = JsonUtility.FromJson<PrefabGuidCacheFile>(File.ReadAllText(path));
            if (data?._guids == null) return null;

            // 過濾掉已刪除的 prefab
            var valid = data._guids
                .Where(g => !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(g)))
                .ToList();

            return valid.Count > 0 ? valid : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PrefabFilterCache] 載入快取失敗: {e.Message}");
            return null;
        }
    }

    private static void UpdateAllFilesRemoveGuid(string guid)
    {
        if (!Directory.Exists(CacheDir)) return;
        foreach (var file in Directory.GetFiles(CacheDir, "*.json"))
        {
            try
            {
                var data = JsonUtility.FromJson<PrefabGuidCacheFile>(File.ReadAllText(file));
                if (data?._guids == null) continue;
                if (data._guids.Remove(guid))
                    File.WriteAllText(file, JsonUtility.ToJson(data));
            }
            catch { /* ignore individual file errors */ }
        }
    }

    private static void UpdateAllFilesRevalidate(string guid, GameObject prefab)
    {
        if (!Directory.Exists(CacheDir)) return;
        foreach (var file in Directory.GetFiles(CacheDir, "*.json"))
        {
            try
            {
                var data = JsonUtility.FromJson<PrefabGuidCacheFile>(File.ReadAllText(file));
                if (data?._guids == null) continue;

                // 從檔案中還原 type
                Type type = data._typeAssemblyQualifiedName != null
                    ? Type.GetType(data._typeAssemblyQualifiedName)
                    : null;

                var tempList = new List<string>(data._guids);
                ApplyRevalidation(type, guid, prefab, tempList);

                if (tempList.Count != data._guids.Count || !tempList.SequenceEqual(data._guids))
                {
                    data._guids = tempList;
                    File.WriteAllText(file, JsonUtility.ToJson(data));

                    // 同步記憶體（若該 type 有載入）
                    if (Cache.ContainsKey(type!))
                        Cache[type!] = tempList;
                }
            }
            catch { /* ignore individual file errors */ }
        }
    }
}

/// <summary>
/// 監聽 prefab 資源異動，精細更新 PrefabFilterCache
/// </summary>
internal class PrefabCacheInvalidator : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (var path in deletedAssets)
        {
            if (!path.EndsWith(".prefab")) continue;
            var guid = AssetDatabase.AssetPathToGUID(path);
            PrefabFilterCache.RemoveGuid(guid);
        }

        foreach (var path in importedAssets)
        {
            if (!path.EndsWith(".prefab")) continue;
            var guid = AssetDatabase.AssetPathToGUID(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                PrefabFilterCache.RevalidatePrefab(guid, prefab);
        }
    }
}
