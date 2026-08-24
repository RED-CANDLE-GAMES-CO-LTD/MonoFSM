using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// Inspector 上任何 component 的右鍵選單：把整顆 component 的內容 dump 成文字丟到剪貼簿。
    ///
    /// 存在的理由：要給 AI 看「這顆現在到底是什麼狀態」時，截圖沒有欄位名、`up peek` 走 CLI
    /// 又要先講清楚節點路徑。右鍵一下複製貼上最快，而且拿到的是反射看到的真值。
    ///
    /// 兩個版本：只有 serialize 欄位的（安全、常用），跟連 public 屬性值一起撈的（會呼叫
    /// getter，有 <see cref="ProbeMineField"/> 的麵包屑保護，炸過一次的以後自動跳過）。
    /// </summary>
    internal static class ComponentDumpMenu
    {
        private const string Fields = "CONTEXT/Component/Dump 欄位 → 剪貼簿";
        private const string All = "CONTEXT/Component/Dump 欄位 + 屬性 → 剪貼簿";

        [MenuItem(Fields, false, 2000)]
        private static void DumpFields(MenuCommand cmd) => Copy(cmd, false);

        [MenuItem(All, false, 2001)]
        private static void DumpAll(MenuCommand cmd) => Copy(cmd, true);

        private static void Copy(MenuCommand cmd, bool includeProperties)
        {
            if (!(cmd.context is Component comp))
            {
                Debug.LogWarning("[Dump] 這個右鍵目標不是 Component");
                return;
            }

            var text = EditProbe.DumpAll(comp, includeProperties);
            EditorGUIUtility.systemCopyBuffer = text;
            Debug.Log($"[Dump] 已複製 {text.Length} 字元到剪貼簿\n{text}", comp);
        }
    }
}
