using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace HierarchyFavorites.Editor
{
    /// <summary>
    /// UIToolkit 內容構建共用邏輯：tab 列、搜尋框、各 tab 的 groups。
    /// overlay popup 與 dockable window 都呼叫這裡，避免兩份重複 code。
    /// 注意：所有可點元素一律用 MouseDownEvent + TrickleDown，
    /// 因為 Button.clicked 在 popup 視窗 + Alt held 時會被攔截。
    /// 搜尋輸入只重填 entries 容器（TextField 保持存活，focus 不會掉）。
    /// </summary>
    internal static class HierarchyFavoritesContentBuilder
    {
        private const string UxmlFileName = "HierarchyFavoritesOverlay.uxml";
        private const string UssFileName = "HierarchyFavoritesOverlay.uss";

        private static VisualTreeAsset _cachedUxml;
        private static StyleSheet _cachedUss;

        // 搜尋字串（所有 tab 共用），overlay / window 重開時保留
        private static string _variableSearch = string.Empty;

        // tab 切換時重建後自動聚焦搜尋框
        private static bool _focusSearchOnNextBuild;

        /// <summary>重建整個內容（title / tabs / search / groups）到 root 上。</summary>
        public static void Build(VisualElement root, Action onRebuild, string titleSuffix = "")
        {
            BindHotkeys(root, onRebuild);

            // Bug A：重建會把 ScrollView 砍掉重建，先存 scrollOffset，重建完還原
            var oldScroll = root.Q<ScrollView>("scroll");
            var savedScrollOffset = oldScroll?.scrollOffset ?? Vector2.zero;

            root.Clear();

            if (_cachedUxml == null) _cachedUxml = LoadAsset<VisualTreeAsset>(UxmlFileName);
            if (_cachedUss == null) _cachedUss = LoadAsset<StyleSheet>(UssFileName);

            if (_cachedUss != null && !root.styleSheets.Contains(_cachedUss))
                root.styleSheets.Add(_cachedUss);

            if (_cachedUxml != null)
            {
                _cachedUxml.CloneTree(root);
            }
            else
            {
                var fallback = new VisualElement { name = "root" };
                fallback.AddToClassList("favorites-root");
                var scroll = new ScrollView { name = "scroll" };
                var entries = new VisualElement { name = "entries" };
                scroll.Add(entries);
                fallback.Add(scroll);
                fallback.Add(new Label("UXML not found") { name = "empty-hint" });
                root.Add(fallback);
            }

            var contentMode = HierarchyFavoritesSettings.Content;

            var title = root.Q<Label>("title");
            if (title != null)
                title.text = contentMode == HierarchyFavoritesSettings.ContentMode.Favorites
                    ? $"Hierarchy Favorites{titleSuffix}"
                    : $"Hierarchy Favorites - {contentMode}{titleSuffix}";

            // Tab 列插在 title 之後
            var headerParent = title != null && title.parent != null ? title.parent : root;
            var tabs = BuildTabRow(contentMode, onRebuild);
            if (title != null && title.parent != null)
                title.parent.Insert(title.parent.IndexOf(title) + 1, tabs);
            else
                root.Add(tabs);

            var entriesContainer = root.Q<VisualElement>("entries");
            var emptyHint = root.Q<Label>("empty-hint");
            if (entriesContainer == null) return;

            var searchField = BuildSearchField(entriesContainer, emptyHint, contentMode);
            var tabsIndex = headerParent.IndexOf(tabs);
            headerParent.Insert(tabsIndex + 1, searchField);

            RefillContent(entriesContainer, emptyHint, contentMode);

            // Bug B：tab 切換後 / 打字期間被外部 rebuild 砍掉時，focus 回搜尋框
            if (_focusSearchOnNextBuild || !string.IsNullOrEmpty(_variableSearch))
            {
                _focusSearchOnNextBuild = false;
                searchField.schedule.Execute(() => searchField.Focus());
            }

            // 還原 scroll 位置（layout 完成後再設，否則會被 clamp 成 0）
            var newScroll = root.Q<ScrollView>("scroll");
            if (newScroll != null && savedScrollOffset != Vector2.zero)
                newScroll.schedule.Execute(() => newScroll.scrollOffset = savedScrollOffset);
        }

        // ---- 快捷鍵：Tab 循環切換分類 tab、Cmd/Ctrl+F 聚焦搜尋框 ----
        // callback 掛在 root 本身，root.Clear() 不會移除，所以用 class marker 確保只註冊一次
        private const string KeysBoundClass = "hf-keys-bound";

        private static readonly HierarchyFavoritesSettings.ContentMode[] TabOrder =
        {
            HierarchyFavoritesSettings.ContentMode.Descriptions,
            HierarchyFavoritesSettings.ContentMode.Variables,
            HierarchyFavoritesSettings.ContentMode.Effects,
            HierarchyFavoritesSettings.ContentMode.States,
            HierarchyFavoritesSettings.ContentMode.Favorites,
        };

        private static void BindHotkeys(VisualElement root, Action onRebuild)
        {
            if (root.ClassListContains(KeysBoundClass)) return;
            root.AddToClassList(KeysBoundClass);

            root.RegisterCallback<KeyDownEvent>(e =>
            {
                Debug.Log("[HierarchyFavorites] KeyDown:" + e.keyCode + e.character);
                if (e.keyCode == KeyCode.Tab)
                {
                    Debug.Log("[HierarchyFavorites] Tab");
                    CycleContentMode(e.shiftKey ? -1 : 1);
                    e.StopPropagation();
                    root.focusController?.IgnoreEvent(e);
                    // 在 event 處理中直接重建會砍掉 event target，延一個 tick
                    root.schedule.Execute(() => onRebuild?.Invoke());
                }
                else if (e.character == '\t')
                {
                    // Tab 會送第二個帶 character 的 KeyDownEvent，擋掉避免 TextField 吃進 '\t'
                    e.StopPropagation();
                    root.focusController?.IgnoreEvent(e);
                }
                else if (e.keyCode == KeyCode.F && (e.commandKey || e.ctrlKey))
                {
                    Debug.Log("[HierarchyFavorites] Cmd/Ctrl+F -> focus search");
                    e.StopPropagation();
                    var searchField = root.Q<TextField>("hf-var-search");
                    searchField?.schedule.Execute(() => searchField.Focus());
                }
            }, TrickleDown.TrickleDown);
        }

        private static void CycleContentMode(int dir)
        {
            var current = HierarchyFavoritesSettings.Content;
            var idx = Array.IndexOf(TabOrder, current);
            var next = TabOrder[(idx + dir + TabOrder.Length) % TabOrder.Length];
            Debug.Log($"[HierarchyFavorites] Tab hotkey: {current} -> {next}");
            HierarchyFavoritesSettings.Content = next;
            _focusSearchOnNextBuild = true;
        }

        // ---- 搜尋框 ----
        private static TextField BuildSearchField(VisualElement entriesContainer, Label emptyHint,
            HierarchyFavoritesSettings.ContentMode contentMode)
        {
            var searchField = new TextField { name = "hf-var-search", value = _variableSearch };
            searchField.AddToClassList("hf-search-field");

            // 搜尋輸入只重填結果容器，不重建整個 UI（TextField 保持存活，focus 不會掉）
            searchField.RegisterValueChangedCallback(e =>
            {
                _variableSearch = e.newValue;
                RefillContent(entriesContainer, emptyHint, contentMode);
            });

            // 保險：按鍵不要冒泡出去觸發編輯器全域快捷鍵（SceneView 的 F、Q/W/E/R 等）
            // searchField.RegisterCallback<KeyDownEvent>(e => e.StopPropagation(), TrickleDown.TrickleDown);

            return searchField;
        }

        // ---- tab 列 ----
        private static VisualElement BuildTabRow(
            HierarchyFavoritesSettings.ContentMode contentMode, Action onRebuild)
        {
            var row = new VisualElement { name = "hf-tabs" };
            row.AddToClassList("hf-tab-row");
            row.Add(MakeTab("Descriptions", HierarchyFavoritesSettings.ContentMode.Descriptions,
                contentMode, onRebuild));
            row.Add(MakeTab("Variables", HierarchyFavoritesSettings.ContentMode.Variables,
                contentMode, onRebuild));
            row.Add(MakeTab("Effects", HierarchyFavoritesSettings.ContentMode.Effects, contentMode,
                onRebuild));
            row.Add(MakeTab("States", HierarchyFavoritesSettings.ContentMode.States, contentMode,
                onRebuild));
            row.Add(MakeTab("Favorites", HierarchyFavoritesSettings.ContentMode.Favorites,
                contentMode, onRebuild));
            return row;
        }

        private static Button MakeTab(string text, HierarchyFavoritesSettings.ContentMode mode,
            HierarchyFavoritesSettings.ContentMode currentMode, Action onRebuild)
        {
            var tab = new Button { text = text };
            tab.AddToClassList("hf-tab");
            if (currentMode == mode)
                tab.AddToClassList("tab-active");
            // Button.clicked 在 popup + Alt held 時會被攔截，一律用 MouseDownEvent + TrickleDown
            tab.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button != 0) return;
                Debug.Log($"[HierarchyFavorites] Switch content tab to {mode}");
                HierarchyFavoritesSettings.Content = mode;
                _focusSearchOnNextBuild = true;
                e.StopPropagation();
                onRebuild?.Invoke();
            }, TrickleDown.TrickleDown);
            return tab;
        }

        /// <summary>依 tab 重填結果容器（不動 search field，focus 不會掉）。</summary>
        private static void RefillContent(VisualElement entriesContainer, Label emptyHint,
            HierarchyFavoritesSettings.ContentMode contentMode)
        {
            if (contentMode == HierarchyFavoritesSettings.ContentMode.Favorites)
                BuildFavoritesContent(entriesContainer, emptyHint);
            else
                RefillList(entriesContainer, emptyHint, contentMode);
        }

        // ---- Favorites ----
        private static void BuildFavoritesContent(VisualElement entriesContainer, Label emptyHint)
        {
            entriesContainer.Clear();
            var groups = HierarchyFavoritesCollector.GetActiveGroups();
            var search = _variableSearch ?? string.Empty;

            int visibleEntryCount = 0;
            foreach (var groupData in groups)
            {
                if (groupData == null) continue;
                var groupElement = BuildGroup(groupData, search, out int count);
                if (count == 0) continue;
                entriesContainer.Add(groupElement);
                visibleEntryCount += count;
            }

            if (emptyHint != null)
            {
                emptyHint.text = string.IsNullOrEmpty(search)
                    ? "No HierarchyFavoritesHolder in current scene / prefab."
                    : "No favorite matching search.";
                emptyHint.style.display =
                    visibleEntryCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // ---- Variables / Effects / States 共用清單路徑（僅資料來源不同）----
        private static void RefillList(VisualElement entriesContainer, Label emptyHint,
            HierarchyFavoritesSettings.ContentMode contentMode)
        {
            entriesContainer.Clear();

            List<VariableGroup> groups;
            string emptyText;
            switch (contentMode)
            {
                case HierarchyFavoritesSettings.ContentMode.Effects:
                    groups = VariableFolderCollector.GetEffectGroups();
                    emptyText = "No EffectDealer / EffectReceiver (or matching search) found.";
                    break;
                case HierarchyFavoritesSettings.ContentMode.States:
                    groups = VariableFolderCollector.GetStateGroups();
                    emptyText = "No MonoStateBehaviour (or matching search) found.";
                    break;
                case HierarchyFavoritesSettings.ContentMode.Descriptions:
                    groups = VariableFolderCollector.GetDescriptionGroups();
                    emptyText = "No AbstractDescriptionBehaviour (or matching search) found.";
                    break;
                default:
                    groups = VariableFolderCollector.GetActiveGroups();
                    emptyText = "No variable (or matching search) found.";
                    break;
            }

            var search = _variableSearch ?? string.Empty;

            int visibleEntryCount = 0;
            foreach (var groupData in groups)
            {
                if (groupData == null) continue;
                var groupElement = BuildVariableGroup(groupData, search, out int count);
                if (count == 0) continue;
                entriesContainer.Add(groupElement);
                visibleEntryCount += count;
            }

            if (emptyHint != null)
            {
                emptyHint.text = emptyText;
                emptyHint.style.display =
                    visibleEntryCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static VisualElement BuildGroup(FavoriteGroup groupData, string search,
            out int entryCount)
        {
            var group = new VisualElement();
            group.AddToClassList("favorites-group");

            var header = new Label(groupData.Name);
            header.AddToClassList("favorites-group-header");
            group.Add(header);

            entryCount = 0;
            foreach (var item in groupData.Items)
            {
                if (item.Target == null) continue;
                // group 名也算命中，方便整組篩出來
                if (!string.IsNullOrEmpty(search) &&
                    !Contains(item.Label, search) &&
                    !Contains(item.Target.name, search) &&
                    !Contains(groupData.Name, search)) continue;

                var target = item.Target;
                var label = item.Label;
                var btn = new Button { text = label };
                btn.AddToClassList("favorites-item");
                if (item.Tint != Color.white && item.Tint.a > 0.01f)
                    btn.style.color = item.Tint;
                // Button.clicked 在 popup 視窗 + Alt held 時常被攔截，
                // 改攔 MouseDownEvent + trickle-down 以確保觸發。
                btn.RegisterCallback<MouseDownEvent>(e =>
                {
                    Debug.Log(
                        $"[HierarchyFavorites] MouseDown on {label} button={e.button} mods={e.modifiers}");
                    if (e.button != 0) return;
                    HierarchyFavoritesOverlayBase.HandleEntryClick(target);
                    e.StopPropagation();
                }, TrickleDown.TrickleDown);
                group.Add(btn);
                entryCount++;
            }

            return group;
        }

        private static VisualElement BuildVariableGroup(VariableGroup groupData, string search,
            out int entryCount)
        {
            var group = new VisualElement();
            group.AddToClassList("favorites-group");

            var folderTransform = groupData.FolderTransform;
            var header = new Button { text = groupData.Name };
            header.AddToClassList("favorites-group-header");
            header.AddToClassList("hf-group-header-btn");
            header.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button != 0) return;
                HierarchyFavoritesOverlayBase.HandleEntryClick(folderTransform);
                e.StopPropagation();
            }, TrickleDown.TrickleDown);
            group.Add(header);

            entryCount = 0;
            foreach (var item in groupData.Items)
            {
                if (item.Target == null) continue;
                if (!MatchesSearch(item, search)) continue;

                var target = item.Target;
                var displayText = $"{item.Label}  <{item.TypeName}>";
                if (!string.IsNullOrEmpty(item.TagName) && item.TagName != item.Label)
                    displayText += $"  [{item.TagName}]";

                var btn = new Button { text = displayText };
                btn.AddToClassList("favorites-item");
                btn.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button != 0) return;
                    HierarchyFavoritesOverlayBase.HandleEntryClick(target);
                    e.StopPropagation();
                }, TrickleDown.TrickleDown);
                group.Add(btn);
                entryCount++;
            }

            return group;
        }

        internal static bool MatchesSearch(VariableEntry item, string search)
        {
            if (string.IsNullOrEmpty(search)) return true;
            return Contains(item.Label, search) || Contains(item.TypeName, search) ||
                   Contains(item.TagName, search);
        }

        private static bool Contains(string source, string search)
        {
            return source != null &&
                   source.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static T LoadAsset<T>(string fileName) where T : Object
        {
            const string expectedDir = "Assets/0_Gameplay/HierarchyFavorites/Editor";
            var expected =
                AssetDatabase.LoadAssetAtPath<T>(Path.Combine(expectedDir, fileName)
                    .Replace('\\', '/'));
            if (expected != null) return expected;

            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var guids = AssetDatabase.FindAssets(nameWithoutExt + " t:" + typeof(T).Name);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(fileName)) continue;
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) return asset;
            }

            return null;
        }
    }
}
