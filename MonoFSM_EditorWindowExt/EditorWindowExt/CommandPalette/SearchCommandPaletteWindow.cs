#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoFSM.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SearchField = UnityEditor.IMGUI.Controls.SearchField;

namespace CommandPalette
{
    /// <summary>
    /// 命令面板 - 統一搜尋，分組顯示（VS Code / Raycast 風格）
    /// 快捷鍵: Cmd+T (Mac) / Ctrl+T (Windows)
    /// 支援 Prefabs, ScriptableObjects, Scenes, MenuItems, Windows, Cheats（Play Mode 中的 CheatRegistry）
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
        private List<SearchResult<CheatEntry>> _cheatResults = new();
        private List<SearchResult<MenuItemEntry>> _editorCheatResults = new();

        // 資源快取
        private Dictionary<SearchMode, List<AssetEntry>> _assetCache = new();
        private List<MenuItemEntry> _menuItemCache;
        private List<EditorWindowEntry> _windowCache;
        private List<MenuItemEntry> _cheatMenuItemCache;

        // 扁平列表（分組顯示用）
        private List<ResultRow> _flatRows = new();
        private int _selectableCount;

        // IME 組字追蹤
        private bool _wasComposing;

        // 開窗回填上次 query 時要全選，否則新打的字（特別是中文 IME）會直接接在舊字串後面
        private bool _pendingSelectAll;

        // 拖拉相關
        private int _dragStartIndex = -1; // selectable index
        private Vector2 _dragStartPos;
        private bool _isDragging;

        // 排序模式（首次預設 Score，之後由 EditorPref 記住；按鈕點擊切換）
        private SearchSortMode _sortMode = SearchSortMode.ScoreBased;
        private const string SortModePrefKey = "CommandPalette_SortMode";
        private const string SearchStringPrefKey = "CommandPalette_SearchString";
        private const string ActiveTabPrefKey = "CommandPalette_ActiveTab";

        /// <summary>分頁：All = 原本的全類別搜尋；Cheats = 只列執行期 CheatRegistry</summary>
        private enum PaletteTab
        {
            All,
            Cheats,
        }

        private PaletteTab _activeTab = PaletteTab.All;
        private static readonly string[] TabLabels = { "All", "Cheats" };

        //Play Mode 中 registry 會增減，用數量變化當 dirty 判斷，不用每幀重搜
        private int _lastCheatEntryCount = -1;

        private const float TabBarHeight = 18f;
        private const float ListStartY = 28f + TabBarHeight;
        private const float RowHeight = 22f;
        private const float GroupHeaderHeight = 18f;
        private const float PathBarHeight = 20f;
        private const float DragThreshold = 5f;
        private const int GroupMaxResults = 5;

        //Cheats 分頁是專屬列表，不用像 All 分頁那樣每組只留 5 筆
        private const int CheatsTabMaxResults = 300;

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
                { SearchMode.Cheats, "CHEATS" },
                { SearchMode.EditorCheats, "EDITOR CHEATS" },
            };

        /// <summary>
        /// MenuItem 型 cheat 的判定就是這張前綴表（Edit Mode 也能用的那種 cheat 都是 MenuItem），
        /// 要新增/移除哪些選單算 cheat 就改這裡。
        /// </summary>
        private static readonly string[] CheatMenuPathPrefixes =
        {
            "RCGMaker/",
            "MonoFSM/",
            "Tools/MonoFSM/",
            "RCGs/ShortCut/",
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
            _activeTab = (PaletteTab)EditorPrefs.GetInt(ActiveTabPrefKey, (int)PaletteTab.All);
            _searchString = EditorPrefs.GetString(SearchStringPrefKey, "");
            // 立刻載入 Prefabs（最重要），其他非同步補齊
            _pendingSelectAll = !string.IsNullOrEmpty(_searchString);
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
            if (_activeTab == PaletteTab.Cheats)
            {
                PerformCheatsSearch();
                return;
            }

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
            _editorCheatResults = new List<SearchResult<MenuItemEntry>>();
            //cheat 只有 Play Mode 才有東西（是執行期登錄的），而且每次搜尋都重讀，不快取
            _cheatResults = SortHoldToBottom(EditorApplication.isPlaying && CheatRegistry.Entries.Count > 0
                ? SearchEngine.Search(_searchString, CheatRegistry.Entries, GroupMaxResults, _sortMode)
                : new List<SearchResult<CheatEntry>>());

            BuildFlatRows();
            if (_selectableCount == 0 && !string.IsNullOrEmpty(_searchString))
                Debug.Log($"[CommandPalette] 查無結果 query=\"{_searchString}\" len={_searchString.Length} " +
                          $"codes=[{string.Join(",", _searchString.Select(c => ((int)c).ToString("X4")))}]");
            _selectedIndex = _selectableCount > 0 ? 0 : -1;
            Repaint();
        }

        /// <summary>
        /// Cheats 分頁：只搜 CheatRegistry，沒打字也全列出來
        /// </summary>
        private void PerformCheatsSearch()
        {
            EnsureAllCachesLoaded();

            _prefabResults = new List<SearchResult<AssetEntry>>();
            _scriptableObjectResults = new List<SearchResult<AssetEntry>>();
            _sceneResults = new List<SearchResult<AssetEntry>>();
            _menuItemResults = new List<SearchResult<MenuItemEntry>>();
            _windowResults = new List<SearchResult<EditorWindowEntry>>();

            //MenuItem 型 cheat 不分 Play / Edit Mode 都有
            _editorCheatResults = CheatMenuItems.Count > 0
                ? SearchEngine.Search(_searchString, CheatMenuItems, CheatsTabMaxResults, _sortMode)
                : new List<SearchResult<MenuItemEntry>>();

            _lastCheatEntryCount = EditorApplication.isPlaying ? CheatRegistry.Entries.Count : -1;
            _cheatResults = SortHoldToBottom(EditorApplication.isPlaying && CheatRegistry.Entries.Count > 0
                ? SearchEngine.Search(_searchString, CheatRegistry.Entries, CheatsTabMaxResults, _sortMode)
                : new List<SearchResult<CheatEntry>>());

            BuildFlatRows();
            _selectedIndex = _selectableCount > 0 ? 0 : -1;
            Repaint();
        }

        //白名單前綴過濾出來的 MenuItem cheat，只算一次
        private List<MenuItemEntry> CheatMenuItems
        {
            get
            {
                if (_cheatMenuItemCache != null)
                    return _cheatMenuItemCache;

                _cheatMenuItemCache = new List<MenuItemEntry>();
                if (_menuItemCache == null)
                    return _cheatMenuItemCache;

                foreach (var item in _menuItemCache)
                {
                    if (string.IsNullOrEmpty(item.menuPath))
                        continue;
                    foreach (var prefix in CheatMenuPathPrefixes)
                    {
                        if (!item.menuPath.StartsWith(prefix))
                            continue;
                        _cheatMenuItemCache.Add(item);
                        break;
                    }
                }

                return _cheatMenuItemCache;
            }
        }

        //按住型（灰色、不能執行）一律沉底；OrderBy 是穩定排序，其餘順序維持原本的分數 / 登錄順序
        private static List<SearchResult<CheatEntry>> SortHoldToBottom(List<SearchResult<CheatEntry>> results)
        {
            return results.OrderBy(r => r.Item.CanInvoke ? 0 : 1).ToList();
        }

        //Play Mode 中 cheat 會隨物件 enable/disable 增減，數量變了就重搜
        private void RefreshCheatsIfRegistryChanged()
        {
            if (_activeTab != PaletteTab.Cheats || !EditorApplication.isPlaying)
                return;
            if (_lastCheatEntryCount == CheatRegistry.Entries.Count)
                return;
            PerformCheatsSearch();
        }

        private void DrawTabBar()
        {
            var rect = new Rect(0, 26, position.width, TabBarHeight);
            var newTab = (PaletteTab)GUI.Toolbar(rect, (int)_activeTab, TabLabels, EditorStyles.miniButton);
            if (newTab == _activeTab)
                return;

            _activeTab = newTab;
            EditorPrefs.SetInt(ActiveTabPrefKey, (int)_activeTab);
            GUIUtility.keyboardControl = 0;
            _scrollPos = Vector2.zero;
            PerformUnifiedSearch();
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

            if (_activeTab == PaletteTab.Cheats)
            {
                AppendSimpleGroup(SearchMode.EditorCheats, _editorCheatResults.Count);
                AppendSimpleGroup(SearchMode.Cheats, _cheatResults.Count);
                return;
            }

            // 空 query 時維持固定順序，有搜尋詞時依各分組最高分降序排列
            float TopScore<T>(List<SearchResult<T>> r) => r.Count > 0 ? r[0].Score : -1f;

            var groups = new (float score, System.Action append)[]
            {
                (TopScore(_prefabResults),           () => AppendAssetGroup(SearchMode.Prefabs, _prefabResults)),
                (TopScore(_scriptableObjectResults), () => AppendAssetGroup(SearchMode.ScriptableObjects, _scriptableObjectResults)),
                (TopScore(_sceneResults),            () => AppendAssetGroup(SearchMode.Scenes, _sceneResults)),
                (TopScore(_menuItemResults),         () => AppendSimpleGroup(SearchMode.MenuItems, _menuItemResults.Count)),
                (TopScore(_windowResults),           () => AppendSimpleGroup(SearchMode.Windows, _windowResults.Count)),
                (TopScore(_cheatResults),            () => AppendSimpleGroup(SearchMode.Cheats, _cheatResults.Count)),
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

            RefreshCheatsIfRegistryChanged();
            HandleKeyboardInput();
            DrawSearchField();
            DrawTabBar();
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
                    JumpToNextGroup();
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

                case KeyCode.L:
                    if (Event.current.command || Event.current.control)
                    {
                        CopySelectedLink();
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

        /// <summary>
        /// Tab：將選取項跳到「下一組」的第一個可選取項（循環）。
        /// 分組順序依 _flatRows 實際排列（各分組於 BuildFlatRows 中連續排列）。
        /// </summary>
        private void JumpToNextGroup()
        {
            if (_selectableCount == 0) return;

            var groupFirsts = new List<int>(); // 每組第一個可選取項的 selectableIndex
            var selectedGroup = -1;            // 目前選取項所在的組 index
            SearchMode? lastCategory = null;

            foreach (var row in _flatRows)
            {
                if (row._isHeader) continue;
                if (lastCategory == null || row._category != lastCategory.Value)
                {
                    lastCategory = row._category;
                    groupFirsts.Add(row._selectableIndex);
                }

                if (row._selectableIndex == _selectedIndex)
                    selectedGroup = groupFirsts.Count - 1;
            }

            if (groupFirsts.Count == 0) return;

            var nextGroup = (selectedGroup + 1) % groupFirsts.Count;
            GUIUtility.keyboardControl = 0;
            _selectedIndex = groupFirsts[nextGroup];
            ScrollToSelected();
            PingSelectedAsset();
            Repaint();
        }

        private void DrawSearchField()
        {
            if (_searchField == null)
            {
                _searchField = new SearchField();
                _searchField.SetFocus();
            }

            const float btnWidth = 50f;
            const float linkBtnWidth = 40f;
            var searchRect = new Rect(5, 5, position.width - 10 - btnWidth - linkBtnWidth - 8, 18);
            var newSearchString = _searchField.OnGUI(searchRect, _searchString);

            if (_pendingSelectAll && Event.current.type == EventType.Repaint)
            {
                _pendingSelectAll = false;
                TrySelectAllInFocusedTextField();
            }

            if (newSearchString != _searchString)
            {
                _searchString = newSearchString;
                EditorPrefs.SetString(SearchStringPrefKey, _searchString);
                PerformUnifiedSearch();
            }

            // 複製選取項的 unity link（Cmd/Ctrl+L 也可以）
            var linkBtnRect = new Rect(position.width - btnWidth - linkBtnWidth - 9, 5, linkBtnWidth, 18);
            using (new EditorGUI.DisabledScope(!CanCopySelectedLink()))
            {
                var linkContent = new GUIContent("Link", "複製選取項的連結（Cmd/Ctrl+L），點連結會回到 Unity 執行");
                if (GUI.Button(linkBtnRect, linkContent, EditorStyles.miniButton))
                    CopySelectedLink();
            }

            // 排序模式切換按鈕（Tab 已改為跳組，排序切換改由此按鈕點擊）
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

        /// <summary>
        /// 全選目前 focus 的 IMGUI 文字欄位內容（回填上次 query 時用）。
        /// IMGUI 沒有公開 API，只能反射拿 EditorGUI 內部的 RecycledTextEditor；拿不到就放棄（不影響搜尋）。
        /// </summary>
        private static void TrySelectAllInFocusedTextField()
        {
            var editorField = typeof(EditorGUI).GetField("activeEditor",
                                  BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                              ?? typeof(EditorGUI).GetField("s_RecycledEditor",
                                  BindingFlags.Static | BindingFlags.NonPublic);
            if (editorField == null)
            {
                Debug.Log("[CommandPalette] 找不到 EditorGUI 內部 TextEditor，略過全選");
                return;
            }

            var textEditor = editorField.GetValue(null);
            textEditor?.GetType().GetMethod("SelectAll")?.Invoke(textEditor, null);
        }

        private void ScrollToSelected()
        {
            if (_selectedIndex < 0) return;

            var listHeight = position.height - ListStartY - PathBarHeight;
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
            var listHeight = position.height - ListStartY - PathBarHeight;
            var listRect = new Rect(0, ListStartY, position.width, listHeight);

            //兩組都沒東西才給提示，不然 Edit Mode 下還有 MenuItem 型 cheat 可看
            if (_activeTab == PaletteTab.Cheats && _flatRows.Count == 0)
            {
                DrawCenterHint(listRect,
                    !string.IsNullOrEmpty(_searchString) ? "沒有符合的 cheat" :
                    EditorApplication.isPlaying ? "目前沒有任何 cheat 登錄" :
                    "執行期 cheat 需要 Play Mode；Edit Mode 只有 MenuItem 型 cheat");
                return;
            }
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

        private static void DrawCenterHint(Rect rect, string message)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.65f, 0.65f, 0.65f, 1f) }
            };
            GUI.Label(rect, message, style);
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
                        DrawShortcut(nameRect, entry.shortcut, false);
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

                case SearchMode.Cheats:
                    if (row._itemIndex < _cheatResults.Count)
                    {
                        var entry = _cheatResults[row._itemIndex].Item;
                        var icon = EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow").image;
                        if (icon != null) GUI.DrawTexture(iconRect, icon);

                        //按住型不能一次性觸發，畫灰的
                        var labelStyle = entry.CanInvoke
                            ? EditorStyles.label
                            : new GUIStyle(EditorStyles.label)
                            {
                                normal = { textColor = new Color(0.55f, 0.55f, 0.55f, 1f) }
                            };
                        var label = entry._isHold ? $"{entry.Description}（按住型，不可執行）" : entry.Description;
                        //右側保留按鈕區，文字與熱鍵只畫在剩下的寬度裡
                        var textRect = new Rect(nameRect.x, nameRect.y,
                            Mathf.Max(0f, nameRect.width - CheatButtonsWidth), nameRect.height);
                        GUI.Label(textRect, label, labelStyle);
                        DrawShortcut(textRect, entry.ShortcutText, !entry.CanInvoke);
                        DrawCheatButtons(nameRect, entry);
                    }

                    break;

                case SearchMode.EditorCheats:
                    if (row._itemIndex < _editorCheatResults.Count)
                    {
                        var entry = _editorCheatResults[row._itemIndex].Item;
                        var icon = EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image;
                        if (icon != null) GUI.DrawTexture(iconRect, icon);

                        var textRect = new Rect(nameRect.x, nameRect.y,
                            Mathf.Max(0f, nameRect.width - CheatButtonsWidth), nameRect.height);
                        GUI.Label(textRect, $"{entry.displayName}  ({entry.category})");
                        DrawShortcut(textRect, entry.shortcut, false);
                        DrawEditorCheatButton(nameRect, entry);
                    }

                    break;
            }
        }

        /// <summary>
        /// EDITOR CHEATS 列右側只有「執行」（MenuItem 沒有場上節點可跳）。跟 Cheats 一樣不關面板。
        /// </summary>
        private void DrawEditorCheatButton(Rect nameRect, MenuItemEntry entry)
        {
            var rect = new Rect(nameRect.xMax - CheatPingButtonWidth - 4f, nameRect.y + 2f,
                CheatPingButtonWidth, nameRect.height - 4f);
            using (new EditorGUI.DisabledScope(!entry.isEnabled))
            {
                if (!GUI.Button(rect, new GUIContent("執行", entry.menuPath), EditorStyles.miniButton))
                    return;
                entry.Execute();
                GUIUtility.keyboardControl = 0;
                ShowNotification(new GUIContent("已執行\n" + entry.displayName));
            }
        }

        private const float CheatInvokeButtonWidth = 44f;
        private const float CheatPingButtonWidth = 68f;
        private const float CheatButtonsWidth = CheatInvokeButtonWidth + CheatPingButtonWidth + 10f;

        /// <summary>
        /// Cheats 列右側的兩顆按鈕：執行 / 跳到節點。點按鈕時 GUI.Button 會吃掉事件，
        /// 不會落到列本身的 MouseDown（選取 / 雙擊執行）判定，也不影響 Enter 的鍵盤操作。
        /// </summary>
        private void DrawCheatButtons(Rect nameRect, CheatEntry entry)
        {
            var y = nameRect.y + 2f;
            var h = nameRect.height - 4f;
            var pingRect = new Rect(nameRect.xMax - CheatPingButtonWidth - 4f, y, CheatPingButtonWidth, h);
            var invokeRect = new Rect(pingRect.x - CheatInvokeButtonWidth - 2f, y, CheatInvokeButtonWidth, h);

            using (new EditorGUI.DisabledScope(!entry.CanInvoke))
            {
                var content = new GUIContent("執行",
                    entry.CanInvoke ? "觸發這個 cheat（面板不關閉，可連續按）" : "按住型 cheat 沒有一次性觸發");
                if (GUI.Button(invokeRect, content, EditorStyles.miniButton))
                {
                    CheatRegistry.Invoke(entry);
                    GUIUtility.keyboardControl = 0;
                    ShowNotification(new GUIContent("已執行\n" + entry.Description));
                }
            }

            using (new EditorGUI.DisabledScope(entry._owner == null))
            {
                var content = new GUIContent("跳到節點",
                    entry._owner != null ? "選取並 Ping 登錄這個 cheat 的物件" : "這個 cheat 沒有對應物件");
                if (GUI.Button(pingRect, content, EditorStyles.miniButton))
                {
                    Selection.activeObject = entry._owner;
                    EditorGUIUtility.PingObject(entry._owner);
                    GUIUtility.keyboardControl = 0;
                }
            }
        }

        //結果列右側的熱鍵標示
        private static void DrawShortcut(Rect nameRect, string shortcut, bool isDimmed)
        {
            if (string.IsNullOrEmpty(shortcut)) return;

            //顏色從 EditorStyles 取，dark / light skin 都可讀（原本寫死的灰在 light skin 下幾乎看不到）
            var color = EditorStyles.label.normal.textColor;
            color.a = isDimmed ? 0.6f : 0.95f;
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold,
                normal = { textColor = color }
            };
            GUI.Label(new Rect(nameRect.x, nameRect.y, nameRect.width - 6, nameRect.height), shortcut, style);
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

                case SearchMode.Cheats:
                    return row._itemIndex < _cheatResults.Count
                        ? _cheatResults[row._itemIndex].Item._source ?? ""
                        : "";

                case SearchMode.EditorCheats:
                    return row._itemIndex < _editorCheatResults.Count
                        ? _editorCheatResults[row._itemIndex].Item.menuPath
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
                case SearchMode.Cheats:
                    InvokeCheatResult(row);
                    break;
                case SearchMode.EditorCheats:
                    OpenEditorCheatResult(row);
                    break;
            }
        }

        //Windows 分類沒有可以還原成連結的識別碼，其他分類都可以
        private bool CanCopySelectedLink()
        {
            var row = GetSelectedRow();
            return row != null && row._category != SearchMode.Windows &&
                   row._category != SearchMode.Cheats;
        }

        /// <summary>
        /// 複製選取項的 unity link：MenuItem 複製成「點了就執行這個指令」，
        /// Prefab / SO / Scene 則複製成既有的 asset_guid 連結。
        /// </summary>
        private void CopySelectedLink()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                Debug.LogWarning("[CommandPalette] 沒有選取項，無法複製連結");
                return;
            }

            switch (row._category)
            {
                case SearchMode.Prefabs:
                case SearchMode.ScriptableObjects:
                case SearchMode.Scenes:
                    var assetResults = GetAssetResults(row._category);
                    if (row._itemIndex >= assetResults.Count) return;
                    var asset = assetResults[row._itemIndex].Item;
                    CommandPaletteLinkHelper.CopyToClipboard(
                        asset.name, CommandPaletteLinkHelper.BuildAssetLink(asset.guid));
                    ShowNotification(new GUIContent("已複製連結\n" + asset.name));
                    break;

                case SearchMode.EditorCheats:
                    if (row._itemIndex >= _editorCheatResults.Count) return;
                    var editorCheat = _editorCheatResults[row._itemIndex].Item;
                    CommandPaletteLinkHelper.CopyToClipboard(
                        editorCheat.displayName, CommandPaletteLinkHelper.BuildMenuLink(editorCheat.menuPath));
                    ShowNotification(new GUIContent("已複製指令連結\n" + editorCheat.displayName));
                    break;

                case SearchMode.MenuItems:
                    if (row._itemIndex >= _menuItemResults.Count) return;
                    var menuItem = _menuItemResults[row._itemIndex].Item;
                    CommandPaletteLinkHelper.CopyToClipboard(
                        menuItem.displayName, CommandPaletteLinkHelper.BuildMenuLink(menuItem.menuPath));
                    ShowNotification(new GUIContent("已複製指令連結\n" + menuItem.displayName));
                    break;

                default:
                    Debug.LogWarning("[CommandPalette] 這個分類不支援複製連結：" + row._category);
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

        private void OpenEditorCheatResult(ResultRow row)
        {
            if (row._itemIndex >= _editorCheatResults.Count) return;
            _editorCheatResults[row._itemIndex].Item.Execute();
            Close();
        }

        private void InvokeCheatResult(ResultRow row)
        {
            if (row._itemIndex >= _cheatResults.Count) return;
            var entry = _cheatResults[row._itemIndex].Item;
            if (!entry.CanInvoke)
            {
                ShowNotification(new GUIContent("這個 cheat 是按住型，無法從面板觸發"));
                return;
            }

            CheatRegistry.Invoke(entry);
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
