#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SearchField = UnityEditor.IMGUI.Controls.SearchField;

namespace CommandPalette
{
    /// <summary>
    /// 命令面板 - 統一搜尋，分組顯示（VS Code / Raycast 風格）
    /// 快捷鍵: Cmd+T (Mac) / Ctrl+T (Windows)
    /// 支援 Prefabs, ScriptableObjects, Scenes, MenuItems, Windows
    /// </summary>
    public class SearchCommandPaletteWindow : EditorWindow
    {
        private SearchField _searchField;
        private string _searchString = "";
        private Vector2 _scrollPos;
        private int _selectedIndex = -1;
        private static SearchCommandPaletteWindow _instance;

        // 各分類搜尋結果
        private List<SearchResult<AssetEntry>> _prefabResults = new();
        private List<SearchResult<AssetEntry>> _scriptableObjectResults = new();
        private List<SearchResult<AssetEntry>> _sceneResults = new();
        private List<SearchResult<MenuItemEntry>> _menuItemResults = new();
        private List<SearchResult<EditorWindowEntry>> _windowResults = new();

        // 資源快取
        private Dictionary<SearchMode, List<AssetEntry>> _assetCache = new();
        private List<MenuItemEntry> _menuItemCache;
        private List<EditorWindowEntry> _windowCache;

        // 扁平列表（分組顯示用）
        private List<ResultRow> _flatRows = new();
        private int _selectableCount;

        // IME 組字追蹤
        private bool _wasComposing;

        // 拖拉相關
        private int _dragStartIndex = -1; // selectable index
        private Vector2 _dragStartPos;
        private bool _isDragging;

        // 排序模式
        private SearchSortMode _sortMode = SearchSortMode.ScoreBased;
        private const string SortModePrefKey = "CommandPalette_SortMode";
        private const string SearchStringPrefKey = "CommandPalette_SearchString";

        private const float RowHeight = 22f;
        private const float GroupHeaderHeight = 18f;
        private const float PathBarHeight = 20f;
        private const float DragThreshold = 5f;
        private const int GroupMaxResults = 5;

        private static readonly Dictionary<SearchMode, string> AssetDatabaseFilters =
            new()
            {
                { SearchMode.Prefabs, "t:GameObject" },
                { SearchMode.ScriptableObjects, "t:ScriptableObject" },
                { SearchMode.Scenes, "t:SceneAsset" },
            };

        private static readonly Dictionary<SearchMode, string> ModeDisplayNames =
            new()
            {
                { SearchMode.Prefabs, "PREFABS" },
                { SearchMode.ScriptableObjects, "SCRIPTABLE OBJECTS" },
                { SearchMode.Scenes, "SCENES" },
                { SearchMode.MenuItems, "MENU ITEMS" },
                { SearchMode.Windows, "WINDOWS" },
            };

        private class ResultRow
        {
            public bool _isHeader;
            public SearchMode _category;
            public string _headerLabel;
            public int _itemIndex;       // 指向對應分類 list 的 index
            public int _selectableIndex; // 全域可選取索引（header 為 -1）
        }

        [MenuItem("Tools/Search Command Palette %t")]
        public static void OpenWindow()
        {
            if (_instance != null)
            {
                _instance.Close();
                return;
            }

            _instance = CreateInstance<SearchCommandPaletteWindow>();
            _instance.titleContent = new GUIContent("Command Palette");
            _instance.ShowUtility();
            _instance.Focus();

            var rect = new Rect(200, 200, 500, 400);
            _instance.position = rect;
        }

        private void OnEnable()
        {
            _sortMode = (SearchSortMode)EditorPrefs.GetInt(SortModePrefKey, (int)SearchSortMode.ScoreBased);
            _searchString = EditorPrefs.GetString(SearchStringPrefKey, "");
            // 立刻載入 Prefabs（最重要），其他非同步補齊
            _assetCache[SearchMode.Prefabs] = LoadAssetsForMode(SearchMode.Prefabs);
            PerformUnifiedSearch();

            EditorApplication.delayCall += () =>
            {
                EnsureAllCachesLoaded();
                PerformUnifiedSearch();
            };
        }

        private void EnsureAllCachesLoaded()
        {
            foreach (var mode in new[] { SearchMode.ScriptableObjects, SearchMode.Scenes })
            {
                if (!_assetCache.ContainsKey(mode))
                    _assetCache[mode] = LoadAssetsForMode(mode);
            }

            _menuItemCache ??= SearchCommandPaletteCacheHelper.CollectAllMenuItems();
            _windowCache ??= EditorWindowSearchHelper.GetAllEditorWindowTypes();
        }

        private List<AssetEntry> LoadAssetsForMode(SearchMode mode)
        {
            var assets = new List<AssetEntry>();
            if (!AssetDatabaseFilters.TryGetValue(mode, out var filter))
                return assets;

            var guids = AssetDatabase.FindAssets(filter);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/com.unity."))
                    continue;
                var assetName = System.IO.Path.GetFileNameWithoutExtension(path);
                assets.Add(new AssetEntry(assetName, path, guid));
            }

            return assets;
        }

        private void PerformUnifiedSearch()
        {
            EnsureAllCachesLoaded();

            _prefabResults = SearchAssets(SearchMode.Prefabs);
            _scriptableObjectResults = SearchAssets(SearchMode.ScriptableObjects);
            _sceneResults = SearchAssets(SearchMode.Scenes);
            _menuItemResults = _menuItemCache?.Count > 0
                ? SearchEngine.Search(_searchString, _menuItemCache, GroupMaxResults, _sortMode)
                : new List<SearchResult<MenuItemEntry>>();
            _windowResults = _windowCache?.Count > 0
                ? SearchEngine.Search(_searchString, _windowCache, GroupMaxResults, _sortMode)
                : new List<SearchResult<EditorWindowEntry>>();

            BuildFlatRows();
            _selectedIndex = _selectableCount > 0 ? 0 : -1;
            Repaint();
        }

        private List<SearchResult<AssetEntry>> SearchAssets(SearchMode mode)
        {
            if (!_assetCache.TryGetValue(mode, out var assets) || assets == null || assets.Count == 0)
                return new List<SearchResult<AssetEntry>>();
            return SearchEngine.Search(_searchString, assets, GroupMaxResults, _sortMode);
        }

        private void BuildFlatRows()
        {
            _flatRows.Clear();
            _selectableCount = 0;

            // 空 query 時維持固定順序，有搜尋詞時依各分組最高分降序排列
            float TopScore<T>(List<SearchResult<T>> r) => r.Count > 0 ? r[0].Score : -1f;

            var groups = new (float score, System.Action append)[]
            {
                (TopScore(_prefabResults),           () => AppendAssetGroup(SearchMode.Prefabs, _prefabResults)),
                (TopScore(_scriptableObjectResults), () => AppendAssetGroup(SearchMode.ScriptableObjects, _scriptableObjectResults)),
                (TopScore(_sceneResults),            () => AppendAssetGroup(SearchMode.Scenes, _sceneResults)),
                (TopScore(_menuItemResults),         () => AppendSimpleGroup(SearchMode.MenuItems, _menuItemResults.Count)),
                (TopScore(_windowResults),           () => AppendSimpleGroup(SearchMode.Windows, _windowResults.Count)),
            };

            var ordered = string.IsNullOrEmpty(_searchString)
                ? groups
                : groups.OrderByDescending(g => g.score).ToArray();

            foreach (var (_, append) in ordered)
                append();
        }

        private void AppendAssetGroup(SearchMode mode, List<SearchResult<AssetEntry>> results)
        {
            if (results.Count == 0) return;
            _flatRows.Add(new ResultRow
            {
                _isHeader = true, _category = mode,
                _headerLabel = $"── {ModeDisplayNames[mode]} ({results.Count}) ──",
                _selectableIndex = -1
            });
            for (int i = 0; i < results.Count; i++)
                _flatRows.Add(new ResultRow { _category = mode, _itemIndex = i, _selectableIndex = _selectableCount++ });
        }

        private void AppendSimpleGroup(SearchMode mode, int count)
        {
            if (count == 0) return;
            _flatRows.Add(new ResultRow
            {
                _isHeader = true, _category = mode,
                _headerLabel = $"── {ModeDisplayNames[mode]} ({count}) ──",
                _selectableIndex = -1
            });
            for (int i = 0; i < count; i++)
                _flatRows.Add(new ResultRow { _category = mode, _itemIndex = i, _selectableIndex = _selectableCount++ });
        }

        private ResultRow GetSelectedRow() =>
            _selectedIndex >= 0 ? _flatRows.FirstOrDefault(r => r._selectableIndex == _selectedIndex) : null;

        private List<SearchResult<AssetEntry>> GetAssetResults(SearchMode mode) =>
            mode switch
            {
                SearchMode.Prefabs => _prefabResults,
                SearchMode.ScriptableObjects => _scriptableObjectResults,
                SearchMode.Scenes => _sceneResults,
                _ => new List<SearchResult<AssetEntry>>()
            };

        private void OnGUI()
        {
            if (_isDragging && Event.current.type == EventType.DragExited)
                _isDragging = false;

            var isComposing = Input.compositionString.Length > 0;
            if (isComposing)
                _wasComposing = true;
            else if (Event.current.type == EventType.Repaint)
                _wasComposing = false;

            HandleKeyboardInput();
            DrawSearchField();
            DrawResultsList();
            DrawPathBar();
        }

        private void HandleKeyboardInput()
        {
            if (Event.current.type != EventType.KeyDown)
                return;

            switch (Event.current.keyCode)
            {
                case KeyCode.Escape:
                    Close();
                    Event.current.Use();
                    break;

                case KeyCode.Tab:
                    ToggleSortMode();
                    Event.current.Use();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (!_wasComposing && _selectedIndex >= 0 && _selectedIndex < _selectableCount)
                    {
                        OpenSelectedResult();
                        Event.current.Use();
                    }

                    break;

                case KeyCode.UpArrow:
                    if (_selectableCount > 0)
                    {
                        GUIUtility.keyboardControl = 0;
                        _selectedIndex = _selectedIndex <= 0 ? _selectableCount - 1 : _selectedIndex - 1;
                        ScrollToSelected();
                        PingSelectedAsset();
                        Event.current.Use();
                        Repaint();
                    }

                    break;

                case KeyCode.DownArrow:
                    if (_selectableCount > 0)
                    {
                        GUIUtility.keyboardControl = 0;
                        _selectedIndex = _selectedIndex >= _selectableCount - 1 ? 0 : _selectedIndex + 1;
                        ScrollToSelected();
                        PingSelectedAsset();
                        Event.current.Use();
                        Repaint();
                    }

                    break;
            }
        }

        private void ToggleSortMode()
        {
            _sortMode = _sortMode == SearchSortMode.ScoreBased
                ? SearchSortMode.Alphabetical
                : SearchSortMode.ScoreBased;
            EditorPrefs.SetInt(SortModePrefKey, (int)_sortMode);
            PerformUnifiedSearch();
            if (_searchField != null) _searchField.SetFocus();
        }

        private void DrawSearchField()
        {
            if (_searchField == null)
            {
                _searchField = new SearchField();
                _searchField.SetFocus();
            }

            const float btnWidth = 50f;
            var searchRect = new Rect(5, 5, position.width - 10 - btnWidth - 4, 18);
            var newSearchString = _searchField.OnGUI(searchRect, _searchString);

            if (newSearchString != _searchString)
            {
                _searchString = newSearchString;
                EditorPrefs.SetString(SearchStringPrefKey, _searchString);
                PerformUnifiedSearch();
            }

            // 排序模式切換按鈕
            var btnRect = new Rect(position.width - btnWidth - 5, 5, btnWidth, 18);
            var btnLabel = _sortMode == SearchSortMode.ScoreBased ? "Score" : "A-Z";
            var btnColor = _sortMode == SearchSortMode.ScoreBased
                ? new Color(0.4f, 0.6f, 1f, 1f)
                : new Color(0.6f, 0.9f, 0.6f, 1f);
            var origColor = GUI.color;
            GUI.color = btnColor;
            if (GUI.Button(btnRect, btnLabel, EditorStyles.miniButton))
                ToggleSortMode();
            GUI.color = origColor;
        }

        private void ScrollToSelected()
        {
            if (_selectedIndex < 0) return;

            const float listStartY = 28f;
            var listHeight = position.height - listStartY - PathBarHeight;
            var y = 0f;

            foreach (var row in _flatRows)
            {
                var rowHeight = row._isHeader ? GroupHeaderHeight : RowHeight;
                if (!row._isHeader && row._selectableIndex == _selectedIndex)
                {
                    if (y < _scrollPos.y)
                        _scrollPos.y = y;
                    else if (y + rowHeight > _scrollPos.y + listHeight)
                        _scrollPos.y = y + rowHeight - listHeight;
                    return;
                }

                y += rowHeight;
            }
        }

        private void DrawResultsList()
        {
            const float listStartY = 28f;
            var listHeight = position.height - listStartY - PathBarHeight;
            var listRect = new Rect(0, listStartY, position.width, listHeight);
            var totalHeight = _flatRows.Sum(r => r._isHeader ? GroupHeaderHeight : RowHeight);
            var contentRect = new Rect(0, 0, position.width - 20, totalHeight);

            if (Event.current.type == EventType.MouseDrag && _dragStartIndex >= 0)
            {
                if (Vector2.Distance(Event.current.mousePosition, _dragStartPos) > DragThreshold)
                {
                    StartDragAsset(_dragStartIndex);
                    _dragStartIndex = -1;
                    Event.current.Use();
                }
            }

            _scrollPos = GUI.BeginScrollView(listRect, _scrollPos, contentRect);

            var y = 0f;
            foreach (var row in _flatRows)
            {
                var rowHeight = row._isHeader ? GroupHeaderHeight : RowHeight;
                var rect = new Rect(0, y, position.width - 20, rowHeight);

                if (row._isHeader)
                {
                    DrawGroupHeader(rect, row._headerLabel);
                }
                else
                {
                    if (row._selectableIndex == _selectedIndex)
                    {
                        var selectedColor = _wasComposing
                            ? new Color(0.4f, 0.4f, 0.4f, 0.6f)
                            : new Color(0.3f, 0.5f, 0.85f, 0.8f);
                        EditorGUI.DrawRect(rect, selectedColor);
                    }
                    else if (rect.Contains(Event.current.mousePosition))
                        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));

                    var iconRect = new Rect(rect.x + 5, rect.y + 3, 16, 16);
                    var nameRect = new Rect(rect.x + 25, rect.y, rect.width - 30, rect.height);
                    DrawResultItem(row, iconRect, nameRect);

                    if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                    {
                        _selectedIndex = row._selectableIndex;
                        _dragStartIndex = row._selectableIndex;
                        _dragStartPos = Event.current.mousePosition;
                        PingSelectedAsset();
                        if (Event.current.clickCount == 2)
                            OpenSelectedResult();
                        Event.current.Use();
                        Repaint();
                    }
                }

                y += rowHeight;
            }

            GUI.EndScrollView();

            if (Event.current.type == EventType.MouseUp)
                _dragStartIndex = -1;
        }

        private void DrawGroupHeader(Rect rect, string label)
        {
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.4f, 0.6f, 1f, 1f) },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            GUI.Label(new Rect(rect.x + 5, rect.y, rect.width - 5, rect.height), label, style);
        }

        private void DrawResultItem(ResultRow row, Rect iconRect, Rect nameRect)
        {
            switch (row._category)
            {
                case SearchMode.Prefabs:
                case SearchMode.ScriptableObjects:
                case SearchMode.Scenes:
                    var assetResults = GetAssetResults(row._category);
                    if (row._itemIndex < assetResults.Count)
                    {
                        var entry = assetResults[row._itemIndex].Item;
                        if (entry.icon != null) GUI.DrawTexture(iconRect, entry.icon);
                        GUI.Label(nameRect, entry.name);
                    }

                    break;

                case SearchMode.MenuItems:
                    if (row._itemIndex < _menuItemResults.Count)
                    {
                        var entry = _menuItemResults[row._itemIndex].Item;
                        var icon = EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image;
                        if (icon != null) GUI.DrawTexture(iconRect, icon);
                        GUI.Label(nameRect, $"{entry.displayName}  ({entry.category})");
                    }

                    break;

                case SearchMode.Windows:
                    if (row._itemIndex < _windowResults.Count)
                    {
                        var entry = _windowResults[row._itemIndex].Item;
                        var icon = EditorGUIUtility.IconContent("d_UnityEditor.SceneHierarchyWindow").image;
                        if (icon != null) GUI.DrawTexture(iconRect, icon);
                        GUI.Label(nameRect, $"{entry.DisplayName}  ({entry.Category})");
                    }

                    break;
            }
        }

        private void DrawPathBar()
        {
            var pathBarRect = new Rect(0, position.height - PathBarHeight, position.width, PathBarHeight);
            EditorGUI.DrawRect(pathBarRect, new Color(0.15f, 0.15f, 0.15f, 1f));

            var path = GetSelectedItemPath();
            if (string.IsNullOrEmpty(path)) return;

            var labelRect = new Rect(5, position.height - PathBarHeight + 2, position.width - 10, PathBarHeight - 4);
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f, 1f) },
                alignment = TextAnchor.MiddleLeft
            };
            GUI.Label(labelRect, path, style);
        }

        private string GetSelectedItemPath()
        {
            var row = GetSelectedRow();
            if (row == null) return "";

            switch (row._category)
            {
                case SearchMode.Prefabs:
                case SearchMode.ScriptableObjects:
                case SearchMode.Scenes:
                    var assetResults = GetAssetResults(row._category);
                    return row._itemIndex < assetResults.Count ? assetResults[row._itemIndex].Item.path : "";

                case SearchMode.MenuItems:
                    return row._itemIndex < _menuItemResults.Count
                        ? _menuItemResults[row._itemIndex].Item.menuPath
                        : "";

                case SearchMode.Windows:
                    return row._itemIndex < _windowResults.Count
                        ? _windowResults[row._itemIndex].Item.Type?.FullName ?? ""
                        : "";

                default:
                    return "";
            }
        }

        private void OpenSelectedResult()
        {
            var row = GetSelectedRow();
            if (row == null) return;

            switch (row._category)
            {
                case SearchMode.Prefabs:
                case SearchMode.ScriptableObjects:
                case SearchMode.Scenes:
                    OpenAssetResult(row);
                    break;
                case SearchMode.MenuItems:
                    OpenMenuItemResult(row);
                    break;
                case SearchMode.Windows:
                    OpenWindowResult(row);
                    break;
            }
        }

        private void OpenAssetResult(ResultRow row)
        {
            var results = GetAssetResults(row._category);
            if (row._itemIndex >= results.Count) return;

            var entry = results[row._itemIndex].Item;
            var obj = entry.asset;
            if (obj == null) return;

            if (row._category == SearchMode.Prefabs && obj is GameObject && !string.IsNullOrEmpty(entry.path))
            {
                try
                {
                    var prefabStageType = typeof(EditorSceneManager).Assembly.GetType(
                        "UnityEditor.SceneManagement.PrefabStageUtility"
                    );
                    var openMethod = prefabStageType?.GetMethod(
                        "OpenPrefab",
                        BindingFlags.Public | BindingFlags.Static,
                        null, new[] { typeof(string) }, null
                    );
                    if (openMethod != null)
                    {
                        openMethod.Invoke(null, new object[] { entry.path });
                        Close();
                        return;
                    }
                }
                catch (System.Exception)
                {
                    // 回退到 AssetDatabase.OpenAsset
                }
            }

            AssetDatabase.OpenAsset(obj);
            Close();
        }

        private void OpenMenuItemResult(ResultRow row)
        {
            if (row._itemIndex >= _menuItemResults.Count) return;
            _menuItemResults[row._itemIndex].Item.Execute();
            Close();
        }

        private void OpenWindowResult(ResultRow row)
        {
            if (row._itemIndex >= _windowResults.Count) return;
            EditorWindowSearchHelper.OpenEditorWindow(_windowResults[row._itemIndex].Item);
            Close();
        }

        private void PingSelectedAsset()
        {
            var row = GetSelectedRow();
            if (row == null) return;

            var results = GetAssetResults(row._category);
            if (results.Count == 0 || row._itemIndex >= results.Count) return;

            var asset = results[row._itemIndex].Item.asset;
            if (asset != null)
                EditorGUIUtility.PingObject(asset);
        }

        private void StartDragAsset(int selectableIndex)
        {
            var row = _flatRows.FirstOrDefault(r => r._selectableIndex == selectableIndex);
            if (row == null) return;

            var results = GetAssetResults(row._category);
            if (results.Count == 0 || row._itemIndex >= results.Count) return;

            var entry = results[row._itemIndex].Item;
            var asset = entry.asset;
            if (asset == null) return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new[] { asset };
            DragAndDrop.paths = new[] { entry.path };
            DragAndDrop.StartDrag(entry.name);
            _isDragging = true;
        }

        private void OnLostFocus()
        {
            if (_isDragging) return;
            Close();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
#endif
