using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor
{
    // Inspector 上 Transform 元件右鍵（CONTEXT）：把該 Transform 子樹匯出成文字並複製到剪貼簿
    // 共用 PrefabToTextExporter / FsmTextExporter，scene 物件與 prefab 都適用
    public static class TransformTextExportContextMenu
    {
        private const string MenuCopy = "CONTEXT/Transform/複製階層為文字";
        private const string MenuCopyFull = "CONTEXT/Transform/複製階層為文字 (完整)";
        private const string MenuCopyFsm = "CONTEXT/Transform/Copy FSM as Text";

        [MenuItem(MenuCopy, false, 2100)]
        private static void CopyHierarchyAsText(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null) return;

            var settings = PrefabExportSettings.CreateQuickCopy();
            var text = PrefabToTextExporter.Export(tr.gameObject, settings);
            CopyToClipboardAndLog(text, tr);
        }

        [MenuItem(MenuCopyFull, false, 2101)]
        private static void CopyHierarchyAsTextFull(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null) return;

            var settings = new PrefabExportSettings
            {
                _excludeDefaults = false,
                _onlyPublicFields = false,
                _excludeDefaultTransform = false,
                _includeComments = true
            };
            var text = PrefabToTextExporter.Export(tr.gameObject, settings);
            CopyToClipboardAndLog(text, tr);
        }

        [MenuItem(MenuCopyFsm, false, 2102)]
        private static void CopyFsmAsText(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null) return;

            var text = FsmTextExporter.Export(tr.gameObject);
            CopyToClipboardAndLog(text, tr);
        }

        private static void CopyToClipboardAndLog(string text, Transform tr)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning($"[TransformTextExport] Empty export for '{tr.name}'", tr);
                return;
            }
            EditorGUIUtility.systemCopyBuffer = text;
            Debug.Log($"[TransformTextExport] Copied '{tr.name}' to clipboard ({text.Length} chars)\n{text}", tr);
        }
    }
}
