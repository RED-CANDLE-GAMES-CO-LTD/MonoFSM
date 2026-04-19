using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HierarchyFavorites.Editor
{
    public class HierarchyFavoritesOverlayUIToolkit : HierarchyFavoritesOverlayBase
    {
        private const string UxmlFileName = "HierarchyFavoritesOverlay.uxml";
        private const string UssFileName = "HierarchyFavoritesOverlay.uss";

        private static VisualTreeAsset _cachedUxml;
        private static StyleSheet _cachedUss;

        private void OnEnable()
        {
            var root = rootVisualElement;
            root.pickingMode = PickingMode.Position;
            root.focusable = true;

            // trickle-down 攔截 drag 事件（在 button 等子元素之前）
            root.RegisterCallback<DragEnterEvent>(e => DragAndDrop.visualMode = DragAndDropVisualMode.Copy,
                TrickleDown.TrickleDown);
            root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
            root.RegisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
        }

        private void OnDragUpdated(DragUpdatedEvent e)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            e.StopPropagation();
        }

        private void OnDragPerform(DragPerformEvent e)
        {
            DragAndDrop.AcceptDrag();
            var refs = DragAndDrop.objectReferences;
            var added = HierarchyFavoritesDropHandler.HandleDrop(refs);
            Debug.Log($"[HierarchyFavorites] DragPerform refs={refs.Length} added={added}");
            if (added > 0) OnRebuild();
            e.StopPropagation();
        }

        protected override void OnRebuild()
        {
            var root = rootVisualElement;
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

            var title = root.Q<Label>("title");
            if (title != null) title.text = "Hierarchy Favorites (UI Toolkit)";

            var entriesContainer = root.Q<VisualElement>("entries");
            var emptyHint = root.Q<Label>("empty-hint");
            if (entriesContainer == null) return;

            entriesContainer.Clear();
            var groups = HierarchyFavoritesCollector.GetActiveGroups();

            int visibleEntryCount = 0;
            foreach (var groupData in groups)
            {
                if (groupData == null) continue;
                var groupElement = BuildGroup(groupData, out int count);
                if (count == 0) continue;
                entriesContainer.Add(groupElement);
                visibleEntryCount += count;
            }

            if (emptyHint != null)
                emptyHint.style.display = visibleEntryCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement BuildGroup(FavoriteGroup groupData, out int entryCount)
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
                    Debug.Log($"[HierarchyFavorites] MouseDown on {label} button={e.button} mods={e.modifiers}");
                    if (e.button != 0) return;
                    HandleEntryClick(target);
                    e.StopPropagation();
                }, TrickleDown.TrickleDown);
                group.Add(btn);
                entryCount++;
            }

            return group;
        }

        private static T LoadAsset<T>(string fileName) where T : Object
        {
            const string expectedDir = "Assets/0_Gameplay/HierarchyFavorites/Editor";
            var expected = AssetDatabase.LoadAssetAtPath<T>(Path.Combine(expectedDir, fileName).Replace('\\', '/'));
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
