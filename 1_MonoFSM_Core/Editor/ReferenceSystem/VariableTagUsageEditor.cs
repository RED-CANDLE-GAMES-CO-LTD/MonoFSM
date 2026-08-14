using MonoFSM.Variable;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.ReferenceSystem
{
    /// <summary>
    ///     在 VariableTag 的 Inspector 上掛一鍵「查誰用了這顆變數」。
    ///     VariableTag 本身在 Runtime assembly，看不到 Editor 端的掃描器，
    ///     所以按鈕做在這裡而不是 VariableTag 上的 [Button]。
    /// </summary>
    [CustomEditor(typeof(VariableTag), true)]
    public class VariableTagUsageEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(6);
            if (GUILayout.Button("🔍 查誰用了這顆變數（掃全庫 prefab）", GUILayout.Height(28)))
                VarTagUsageWindow.OpenAndScan((VariableTag)target);
        }
    }
}
