using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor
{
    public static class RevealHiddenChildrenContextMenu
    {
        [MenuItem("CONTEXT/Transform/Reveal Hidden Children")]
        private static void RevealHiddenChildren(MenuCommand command)
        {
            if (command.context is not Transform transform) return;

            int count = 0;
            UnityEngine.Debug.Log(
                $"Checking children of {transform.name} for hidden objects... childCount:{transform.childCount}",
                transform);
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var prevFlags = child.gameObject.hideFlags;
                Undo.RecordObject(child.gameObject, "Reveal Hidden Child");
                child.gameObject.hideFlags = HideFlags.None;
                EditorUtility.SetDirty(child.gameObject);
                count++;
                Debug.Log($"Revealed: {child.name} (was {prevFlags})", child.gameObject);
            }

            Debug.Log(count > 0
                ? $"共顯示了 {count} 個隱藏的子物件"
                : "沒有發現隱藏的子物件", transform);
        }
    }
}