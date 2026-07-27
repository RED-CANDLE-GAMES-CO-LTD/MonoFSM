using System.Linq;
using UnityEditor;
using UnityEngine;
using Abort = MonoFSM.Editor.PrefabEditing.EditResolve.EditAbort;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// asset path → 該塞進 ObjectReference 欄位的那個 Object。
    ///
    /// 難處在 prefab：`.prefab` 載進來是 GameObject，但欄位宣告型別常常是某個 component
    /// （MonoFSM 大量欄位是 `MonoObj _prefab` 而不是 `GameObject _prefab`）。所以要用欄位型別
    /// 回去 prefab 上取對應 component，而不是硬塞 GameObject —— 硬塞的話 Unity 會靜默存成 null。
    /// </summary>
    internal static class AssetRef
    {
        internal static Object Resolve(string assetPath, Component owner, string fieldPath)
        {
            var main = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (main == null)
                throw new Abort($"找不到 asset: {assetPath}");

            var want = EditResolve.FieldType(owner.GetType(), fieldPath);
            if (want == null || want.IsInstanceOfType(main))
                return main;

            if (main is GameObject go)
            {
                var comp = go.GetComponent(want);
                if (comp != null) return comp;
                throw new Abort(
                    $"'{assetPath}' 上沒有 {want.Name}（欄位 '{fieldPath}' 的宣告型別）。" +
                    "這個 prefab root 掛的是：" +
                    EditResolve.Join(go.GetComponents<Component>()
                        .Where(c => c != null).Select(c => c.GetType().Name)));
            }

            // ScriptableObject 之類：可能真值在 sub-asset 上
            var sub = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .FirstOrDefault(a => a != null && want.IsInstanceOfType(a));
            if (sub != null) return sub;

            throw new Abort(
                $"'{assetPath}' 是 {main.GetType().Name}，塞不進宣告型別為 {want.Name} 的 '{fieldPath}'");
        }
    }
}
