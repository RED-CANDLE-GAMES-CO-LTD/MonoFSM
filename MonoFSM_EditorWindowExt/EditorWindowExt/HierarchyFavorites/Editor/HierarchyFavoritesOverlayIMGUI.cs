using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyFavorites.Editor
{
    public class HierarchyFavoritesOverlayIMGUI : HierarchyFavoritesOverlayBase
    {
        private Vector2 _scroll;
        private List<FavoriteGroup> _cachedGroups;
        private List<VariableGroup> _cachedVariableGroups;

        // Variables 模式的搜尋字串，overlay 重開時保留
        private static string _variableSearch = string.Empty;

        private static GUIStyle _titleStyle;
        private static GUIStyle _groupHeaderStyle;
        private static GUIStyle _itemStyle;
        private static GUIStyle _itemHoverStyle;
        private static GUIStyle _tabStyle;
        private static GUIStyle _searchStyle;
        private static Texture2D _bgTex;
        private static Texture2D _itemBgTex;
        private static Texture2D _itemHoverBgTex;

        protected override void OnRebuild()
        {
            _cachedGroups = HierarchyFavoritesCollector.GetActiveGroups();
            _cachedVariableGroups = VariableFolderCollector.GetActiveGroups();
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();

            HandleDragDropEvents();

            // 背景
            var rect = new Rect(0, 0, position.width, position.height);
            GUI.DrawTexture(rect, _bgTex, ScaleMode.StretchToFill);

            GUILayout.BeginArea(new Rect(6, 4, position.width - 12, position.height - 8));

            var contentMode = HierarchyFavoritesSettings.Content;
            var title = contentMode == HierarchyFavoritesSettings.ContentMode.Variables
                ? "Hierarchy Favorites - Variables (IMGUI)"
                : "Hierarchy Favorites (IMGUI)";
            GUILayout.Label(title, _titleStyle);
            GUILayout.Space(2);

            DrawTabRow(contentMode);
            GUILayout.Space(2);

            if (contentMode == HierarchyFavoritesSettings.ContentMode.Variables)
                DrawVariablesMode();
            else
                DrawFavoritesMode();

            GUILayout.EndArea();
        }

        private void DrawTabRow(HierarchyFavoritesSettings.ContentMode contentMode)
        {
            GUILayout.BeginHorizontal();
            var selected = (int)contentMode;
            var newSelected = GUILayout.Toolbar(selected, new[] { "Favorites", "Variables" }, _tabStyle);
            if (newSelected != selected)
            {
                HierarchyFavoritesSettings.Content = (HierarchyFavoritesSettings.ContentMode)newSelected;
                OnRebuild();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawFavoritesMode()
        {
            if (_cachedGroups == null)
                _cachedGroups = HierarchyFavoritesCollector.GetActiveGroups();

            _scroll = GUILayout.BeginScrollView(_scroll);

            int totalEntries = 0;
            foreach (var group in _cachedGroups)
            {
                if (group == null) continue;
                totalEntries += DrawGroup(group);
            }

            if (totalEntries == 0)
            {
                GUILayout.Space(12);
                var hintStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.75f, 0.75f, 0.75f, 0.6f) },
                    wordWrap = true,
                };
                GUILayout.Label("No entries. Drag a GameObject here to add.", hintStyle);
            }

            GUILayout.EndScrollView();
        }

        private void DrawVariablesMode()
        {
            // popup 視窗（ShowPopup）不保證是 pro skin，不能依賴 GUI.skin 的預設配色，
            // 統一用自製的深色底 + 亮字 style
            EditorGUI.BeginChangeCheck();
            _variableSearch = EditorGUILayout.TextField(_variableSearch ?? string.Empty, _searchStyle,
                GUILayout.Height(18));
            if (EditorGUI.EndChangeCheck())
                Repaint();

            if (_cachedVariableGroups == null)
                _cachedVariableGroups = VariableFolderCollector.GetActiveGroups();

            _scroll = GUILayout.BeginScrollView(_scroll);

            int totalEntries = 0;
            foreach (var group in _cachedVariableGroups)
            {
                if (group == null) continue;
                totalEntries += DrawVariableGroup(group, _variableSearch);
            }

            if (totalEntries == 0)
            {
                GUILayout.Space(12);
                var hintStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.75f, 0.75f, 0.75f, 0.6f) },
                    wordWrap = true,
                };
                GUILayout.Label("No VariableFolder / matching variable found.", hintStyle);
            }

            GUILayout.EndScrollView();
        }

        private int DrawGroup(FavoriteGroup group)
        {
            if (group.Items.Count == 0) return 0;

            GUILayout.Space(4);
            GUILayout.Label(group.Name, _groupHeaderStyle);

            foreach (var item in group.Items)
            {
                if (item.Target == null) continue;

                var prevColor = GUI.color;
                if (item.Tint != Color.white && item.Tint.a > 0.01f)
                    GUI.color = item.Tint;

                if (GUILayout.Button(item.Label, _itemStyle, GUILayout.Height(18)))
                {
                    HandleEntryClick(item.Target);
                }

                GUI.color = prevColor;
            }

            return group.Items.Count;
        }

        private int DrawVariableGroup(VariableGroup group, string search)
        {
            // 先過濾符合的 entries，group 若無符合結果就整組不畫
            var visibleItems = new List<VariableEntry>();
            foreach (var item in group.Items)
            {
                if (item.Target == null) continue;
                if (!HierarchyFavoritesContentBuilder.MatchesSearch(item, search)) continue;
                visibleItems.Add(item);
            }

            if (visibleItems.Count == 0) return 0;

            GUILayout.Space(4);
            if (GUILayout.Button(group.Name, _groupHeaderStyle))
            {
                HandleEntryClick(group.FolderTransform);
            }

            foreach (var item in visibleItems)
            {
                var displayText = $"{item.Label}  <{item.TypeName}>";
                if (!string.IsNullOrEmpty(item.TagName) && item.TagName != item.Label)
                    displayText += $"  [{item.TagName}]";

                if (GUILayout.Button(displayText, _itemStyle, GUILayout.Height(18)))
                {
                    HandleEntryClick(item.Target);
                }
            }

            return visibleItems.Count;
        }

        private void HandleDragDropEvents()
        {
            var evt = Event.current;
            if (evt == null) return;
            var windowRect = new Rect(0, 0, position.width, position.height);
            if (!windowRect.Contains(evt.mousePosition) && evt.type != EventType.DragExited) return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;
                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    var added = HierarchyFavoritesDropHandler.HandleDrop(DragAndDrop.objectReferences);
                    if (added > 0) OnRebuild();
                    evt.Use();
                    break;
            }
        }

        private static void EnsureStyles()
        {
            if (_bgTex == null)
            {
                _bgTex = MakeTex(new Color(0.125f, 0.125f, 0.125f, 0.96f));
                _itemBgTex = MakeTex(new Color(0.235f, 0.235f, 0.235f, 0.6f));
                _itemHoverBgTex = MakeTex(new Color(0.35f, 0.47f, 0.67f, 0.9f));
            }

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                };
            }

            if (_groupHeaderStyle == null)
            {
                _groupHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 10,
                    normal = { textColor = new Color(0.7f, 0.78f, 0.94f) },
                };
            }

            if (_itemStyle == null)
            {
                _itemStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    padding = new RectOffset(6, 6, 2, 2),
                    margin = new RectOffset(2, 2, 1, 1),
                    normal = { textColor = new Color(0.86f, 0.86f, 0.86f), background = _itemBgTex },
                    hover = { textColor = Color.white, background = _itemHoverBgTex },
                    active = { textColor = Color.white, background = _itemHoverBgTex },
                };
            }

            if (_tabStyle == null)
            {
                // 不能用 EditorStyles.toolbarButton：非 pro skin / popup 下會白底白字看不到，
                // 明確給深色底 + 亮字；onNormal 系列給 Toolbar 選中的 tab 用
                _tabStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter,
                    fixedHeight = 18,
                    margin = new RectOffset(1, 1, 0, 0),
                    normal = { textColor = new Color(0.78f, 0.78f, 0.78f), background = _itemBgTex },
                    hover = { textColor = Color.white, background = _itemHoverBgTex },
                    active = { textColor = Color.white, background = _itemHoverBgTex },
                    onNormal = { textColor = Color.white, background = _itemHoverBgTex },
                    onHover = { textColor = Color.white, background = _itemHoverBgTex },
                    onActive = { textColor = Color.white, background = _itemHoverBgTex },
                };
            }

            if (_searchStyle == null)
            {
                //搜尋框也要明確配色，不依賴預設 GUI.skin
                _searchStyle = new GUIStyle(EditorStyles.textField)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(6, 6, 2, 2),
                    margin = new RectOffset(2, 2, 2, 4),
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f), background = _itemBgTex },
                    hover = { textColor = Color.white, background = _itemBgTex },
                    focused = { textColor = Color.white, background = _itemBgTex },
                    active = { textColor = Color.white, background = _itemBgTex },
                };
            }
        }

        private static Texture2D MakeTex(Color col)
        {
            var tex = new Texture2D(1, 1);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }
    }
}
