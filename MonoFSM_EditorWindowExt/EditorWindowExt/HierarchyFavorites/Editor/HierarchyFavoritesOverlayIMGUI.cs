using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyFavorites.Editor
{
    public class HierarchyFavoritesOverlayIMGUI : HierarchyFavoritesOverlayBase
    {
        private Vector2 _scroll;
        private List<FavoriteGroup> _cachedGroups;

        private static GUIStyle _titleStyle;
        private static GUIStyle _groupHeaderStyle;
        private static GUIStyle _itemStyle;
        private static GUIStyle _itemHoverStyle;
        private static Texture2D _bgTex;
        private static Texture2D _itemBgTex;
        private static Texture2D _itemHoverBgTex;

        protected override void OnRebuild()
        {
            _cachedGroups = HierarchyFavoritesCollector.GetActiveGroups();
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

            GUILayout.Label("Hierarchy Favorites (IMGUI)", _titleStyle);
            GUILayout.Space(2);

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
            GUILayout.EndArea();
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
