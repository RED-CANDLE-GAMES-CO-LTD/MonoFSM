using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor
{
    // 右鍵選單：把 Hierarchy 子樹匯出成精簡結構化文字並複製到剪貼簿
    public static class HierarchyTextContextMenu
    {
        private const string MenuCopy = "GameObject/MonoFSM/複製精簡階層文字";
        private const string MenuCopyFull = "GameObject/MonoFSM/複製精簡階層文字 (完整展開)";
        private const string MenuCopyCtx = "CONTEXT/Transform/複製精簡階層文字";
        private const string MenuCopyCtxFull = "CONTEXT/Transform/複製精簡階層文字 (完整展開)";

        [MenuItem(MenuCopy, false, 2103)]
        private static void CopyCompact()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;
            CopyToClipboardAndLog(HierarchyTextExporter.Export(go, HierarchyExportOptions.Default), go);
        }

        [MenuItem(MenuCopyFull, false, 2104)]
        private static void CopyFull()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;
            CopyToClipboardAndLog(HierarchyTextExporter.Export(go, HierarchyExportOptions.FullExpand), go);
        }

        [MenuItem(MenuCopyCtx, false, 2103)]
        private static void CopyCompactCtx(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null) return;
            CopyToClipboardAndLog(HierarchyTextExporter.Export(tr.gameObject, HierarchyExportOptions.Default), tr.gameObject);
        }

        [MenuItem(MenuCopyCtxFull, false, 2104)]
        private static void CopyFullCtx(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null) return;
            CopyToClipboardAndLog(HierarchyTextExporter.Export(tr.gameObject, HierarchyExportOptions.FullExpand), tr.gameObject);
        }

        private static void CopyToClipboardAndLog(string text, GameObject go)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning($"[HierarchyTextExport] Empty export for '{go.name}'", go);
                return;
            }
            EditorGUIUtility.systemCopyBuffer = text;
            Debug.Log($"[HierarchyTextExport] Copied '{go.name}' to clipboard ({text.Length} chars)", go);
        }
    }
}
