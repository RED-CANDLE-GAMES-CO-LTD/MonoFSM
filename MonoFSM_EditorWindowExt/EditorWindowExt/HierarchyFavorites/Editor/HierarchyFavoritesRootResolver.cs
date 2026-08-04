using System.Collections.Generic;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HierarchyFavorites.Editor
{
    /// <summary>
    /// 資料來源 root 判定（Favorites / Variables / Effects / States 共用）。
    /// PrefabStage 優先；否則從 Selection 找 MonoObj：
    /// 先往 parent 找（含 self），parent 沒有時往 children 找最上層的那幾個
    /// （常見情境：用一層純 GameObject 或外層 prefab 把數個 entity 包住，點外層原本會找不到內容）。
    /// </summary>
    internal static class HierarchyFavoritesRootResolver
    {
        public static List<Transform> GetActiveRoots()
        {
            var roots = new List<Transform>();

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                roots.Add(stage.prefabContentsRoot.transform);
                return roots;
            }

            var selectObj = Selection.activeGameObject;
            if (selectObj == null) return roots;

            var parentObj = selectObj.GetComponentInParent<MonoObj>();
            if (parentObj != null)
            {
                roots.Add(parentObj.transform);
                return roots;
            }

            // parent 找不到 -> 往下找，只取最上層的 MonoObj（底下的巢狀 MonoObj 會被它的子樹涵蓋）
            var childObjs = selectObj.GetComponentsInChildren<MonoObj>(true);
            foreach (var mo in childObjs)
            {
                if (mo == null) continue;
                if (HasMonoObjAncestorWithin(mo.transform, selectObj.transform)) continue;
                roots.Add(mo.transform);
            }

            if (roots.Count == 0)
                Debug.Log(
                    $"[HierarchyFavorites] No MonoObj in parents or children of {selectObj.name}",
                    selectObj);

            return roots;
        }

        // target 到 boundary（不含 target 自己）之間有沒有其他 MonoObj
        private static bool HasMonoObjAncestorWithin(Transform target, Transform boundary)
        {
            var p = target.parent;
            while (p != null)
            {
                if (p.GetComponent<MonoObj>() != null) return true;
                if (p == boundary) return false;
                p = p.parent;
            }

            return false;
        }
    }
}
