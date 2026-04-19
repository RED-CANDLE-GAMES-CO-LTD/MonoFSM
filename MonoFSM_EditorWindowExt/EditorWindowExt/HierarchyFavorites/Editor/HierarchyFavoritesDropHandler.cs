using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HierarchyFavorites.Editor
{
    internal static class HierarchyFavoritesDropHandler
    {
        /// <summary>
        /// 把 drag 進來的 GameObject 加上 HierarchyFavoriteMarker。
        /// 改用 marker 後每個收藏是獨立 component → prefab override 不會整批互相覆蓋。
        /// 回傳實際新增的 marker 數量。
        /// </summary>
        public static int HandleDrop(Object[] refs)
        {
            if (refs == null || refs.Length == 0) return 0;

            int added = 0;
            foreach (var r in refs)
            {
                var go = AsGameObject(r);
                if (go == null) continue;
                if (go.GetComponent<HierarchyFavoriteMarker>() != null) continue;

                Undo.AddComponent<HierarchyFavoriteMarker>(go);
                EditorUtility.SetDirty(go);
                added++;
            }

            if (added > 0)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null)
                    EditorSceneManager.MarkSceneDirty(stage.scene);
            }

            return added;
        }

        private static GameObject AsGameObject(Object obj)
        {
            if (obj == null) return null;
            if (obj is GameObject go) return go;
            if (obj is Component c) return c.gameObject;
            return null;
        }
    }
}
