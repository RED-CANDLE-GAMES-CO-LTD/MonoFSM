#if UNITY_EDITOR
using System;
using MonoFSM.CustomAttributes;
using Sirenix.OdinInspector.Editor;
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
                var items = tree.Add(
                    ownerPath + "/" + comp.name + " (" + comp.GetType().Name + ")",
                    comp);
                foreach (var item in items)
                    item.DefaultToggledState = false;
            }

            tree.Config.SelectMenuItemsOnMouseDown = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
        }
    }
}
#endif
