using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace HierarchyIDEWindow.MonoFSM_HierarchyDrawer.Editor
{
    /// <summary>
    /// Shift+P：等同 Hierarchy 右鍵的「Go to Added GameObject in '...'」。
    /// 沿著選取物件的 prefab source 鏈往上找，第一層是 added GameObject override 的 prefab asset 就打開它並選到那個物件。
    /// 能找到該 asset 的 instance 時用 In Context 開，否則 In Isolation。
    /// </summary>
    public static class GoToAddedGameObjectShortcut
    {
        [Shortcut("Hierarchy/Go To Added GameObject", KeyCode.P, ShortcutModifiers.Shift)]
        static void GoToAddedGameObject()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;

            var added = FindAddedSource(go);
            if (added == null)
            {
                Debug.LogWarning($"[GoToAddedGameObject] {go.name} 不是任何 prefab 裡的 added GameObject", go);
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(added);
            var siblingPath = GetSiblingPath(added.transform);

            var instanceRoot = FindInstanceRootOf(go, assetPath);
            var stage = instanceRoot != null
                ? PrefabStageUtility.OpenPrefab(assetPath, instanceRoot, PrefabStage.Mode.InContext)
                : PrefabStageUtility.OpenPrefab(assetPath);
            if (stage == null) return;

            var target = Resolve(stage.prefabContentsRoot.transform, siblingPath);
            if (target == null) return;
            Selection.activeGameObject = target.gameObject;
            EditorGUIUtility.PingObject(target.gameObject);
        }

        /// <summary>從 go 的 source 開始往上走，回傳第一個在 prefab asset 裡是 added GameObject override 的物件</summary>
        static GameObject FindAddedSource(GameObject go)
        {
            var cur = PrefabUtility.GetCorrespondingObjectFromSource(go);
            while (cur != null)
            {
                if (PrefabUtility.IsAddedGameObjectOverride(cur)) return cur;
                cur = PrefabUtility.GetCorrespondingObjectFromSource(cur);
            }
            return null;
        }

        /// <summary>往上找 source 是 assetPath 的 prefab instance root，給 In Context 用</summary>
        static GameObject FindInstanceRootOf(GameObject go, string assetPath)
        {
            var t = go.transform;
            while (t != null)
            {
                var root = PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject);
                if (root == null) return null;
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root) == assetPath) return root;
                t = root.transform.parent;
            }
            return null;
        }

        static List<int> GetSiblingPath(Transform t)
        {
            var path = new List<int>();
            while (t.parent != null)
            {
                path.Add(t.GetSiblingIndex());
                t = t.parent;
            }
            path.Reverse();
            return path;
        }

        static Transform Resolve(Transform root, List<int> siblingPath)
        {
            var t = root;
            foreach (var i in siblingPath)
            {
                if (i >= t.childCount) return null;
                t = t.GetChild(i);
            }
            return t;
        }
    }
}
