using System.IO;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor
{
    // Hierarchy / Project 右鍵入口：把選中的 GameObject / Prefab 匯出成 .fsm 文字
    public static class FsmTextExporterContextMenu
    {
        private const string MenuHierarchy = "GameObject/MonoFSM/Copy FSM as Text";
        private const string MenuAssets = "Assets/MonoFSM/Copy FSM as Text";

        [MenuItem(MenuHierarchy, false, 30)]
        private static void CopyFromHierarchy()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;
            CopyToClipboardAndLog(FsmTextExporter.Export(go), go.name);
        }

        [MenuItem(MenuHierarchy, true)]
        private static bool ValidateHierarchy() => Selection.activeGameObject != null;

        [MenuItem(MenuAssets, false, 30)]
        private static void CopyFromAssets()
        {
            var go = Selection.activeObject as GameObject;
            if (go == null) return;
            CopyToClipboardAndLog(FsmTextExporter.Export(go), go.name);
        }

        [MenuItem(MenuAssets, true)]
        private static bool ValidateAssets() => Selection.activeObject is GameObject;

        private static void CopyToClipboardAndLog(string text, string name)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning($"[FsmTextExporter] Empty export for '{name}'");
                return;
            }
            EditorGUIUtility.systemCopyBuffer = text;
            Debug.Log($"[FsmTextExporter] Copied '{name}' to clipboard ({text.Length} chars)\n{text}");
        }
    }
}
