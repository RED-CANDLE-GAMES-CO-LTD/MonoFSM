#if UNITY_EDITOR
using System;
using MonoFSM.CustomAttributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// 共用的 Component dropdown 樹建立工具
    /// 抽自 DropDownRefCompSelector，讓其他需要相同 sibling/prefab-scope component 列舉 + IDropdownRoot 分組
    /// 的 dropdown UI 可以重用（例如 AbstractComponentPropertyValueSource 的 _sourceObject）
    /// </summary>
    public static class ComponentDropdownTreeUtility
    {
        /// <summary>
        /// 在指定的 OdinMenuTree 上加入符合條件的 sibling component
        /// </summary>
        /// <param name="tree">目標選單樹</param>
        /// <param name="forComp">當前 component (排除自身)</param>
        /// <param name="filterType">要列舉的 component 型別（會傳給 GetComponents）</param>
        /// <param name="parentType">查找的 root 邊界類型，預設為 IDropdownRoot</param>
        /// <param name="findFromParentTransform">true 則從 parent transform 開始找而不是 sibling root</param>
        /// <param name="additionalPredicate">可選的額外篩選 (例如「只接受具有 T 型別 property 的 component」)</param>
        public static void Build(
            OdinMenuTree tree,
            Component forComp,
            Type filterType,
            Type parentType = null,
            bool findFromParentTransform = false,
            Func<Component, bool> additionalPredicate = null)
        {
            if (forComp == null)
            {
                Debug.LogError("[ComponentDropdownTreeUtility] forComp is null");
                return;
            }

            tree.Config.DrawSearchToolbar = true;
            tree.Config.UseCachedExpandedStates = true;

            parentType ??= typeof(IDropdownRoot);
            Component[] comps;

            if (findFromParentTransform)
            {
                var parent = forComp.transform.parent;
                if (parent == null)
                {
                    Debug.LogError("Parent is null", forComp);
                    return;
                }
                comps = parent.GetComponentsInChildren(filterType, true);
            }
            else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                var root = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                comps = root.GetComponentsInChildren(filterType, true);
            }
            else
            {
                comps = forComp.GetComponentsOfSiblingAll(parentType, filterType);
            }

            if (comps == null || comps.Length == 0)
            {
                Debug.LogError(
                    $"No components found of type {filterType?.Name} in parent of {forComp.name}",
                    forComp);
                return;
            }

            foreach (var comp in comps)
            {
                if (comp == forComp)
                    continue;

                if (additionalPredicate != null && !additionalPredicate(comp))
                    continue;

                var parents = comp.GetComponentsInParent<IDropdownRoot>(true);
                if (parents == null || parents.Length == 0)
                {
                    Debug.LogError("IDropdownRoot not found for component " + comp.name, comp);
                    continue;
                }

                var ownerNames = new string[parents.Length];
                for (var i = parents.Length - 1; i >= 0; i--)
                    ownerNames[parents.Length - 1 - i] = parents[i].name;
                var ownerPath = string.Join("/", ownerNames);
                var displayName = comp.name + " (" + comp.GetType().Name + ")";
                var items = tree.Add(ownerPath + "/" + displayName, comp);
                foreach (var item in items)
                {
                    item.DefaultToggledState = false;
                    if (!ReferenceEquals(item.Value, comp))
                        continue;

                    //搜尋時可以用路徑關鍵字過濾（預設只比對 leaf Name）
                    item.SearchString = ownerPath + "/" + displayName;
                    //搜尋結果是攤平的，補畫 ownerPath 才能分辨同名項目
                    var capturedPath = ownerPath;
                    item.OnDrawItem += it => DrawOwnerPathWhenSearching(it, capturedPath);
                }
            }

            tree.Config.SelectMenuItemsOnMouseDown = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
        }

        //搜尋結果用的兩行高 style：名字上移，下半行留給路徑
        private static OdinMenuStyle _searchResultStyle;
        private static OdinMenuStyle SearchResultStyle =>
            _searchResultStyle ??= new OdinMenuStyle
            {
                Height = 38,
                LabelVerticalOffset = -8f,
            };

        /// <summary>
        /// 搜尋中（樹被攤平）時，把項目換成兩行高，在下半行以灰色小字顯示所屬路徑，
        /// 讓同名 component 可以被分辨
        /// </summary>
        private static void DrawOwnerPathWhenSearching(OdinMenuItem item, string ownerPath)
        {
            var isSearching = !string.IsNullOrEmpty(item.MenuTree.Config.SearchTerm);

            //搜尋開始/結束時切換 style（影響下一次 layout 的高度）
            if (isSearching)
            {
                if (item.Style != SearchResultStyle)
                    item.Style = SearchResultStyle;
            }
            else if (item.Style != item.MenuTree.DefaultMenuStyle)
            {
                item.Style = item.MenuTree.DefaultMenuStyle;
            }

            if (!isSearching || Event.current.type != EventType.Repaint)
                return;

            var rect = item.Rect;
            //兩行高還沒生效前（切換後第一個 repaint）先不畫，避免疊字
            if (rect.height < SearchResultStyle.Height - 1f)
                return;

            var pathRect = new Rect(
                rect.x + item.Style.Offset,
                rect.yMax - 16f,
                rect.width - item.Style.Offset - 10f,
                14f);
            GUI.Label(pathRect, ownerPath, SirenixGUIStyles.LeftAlignedGreyMiniLabel);
        }
    }
}
#endif
