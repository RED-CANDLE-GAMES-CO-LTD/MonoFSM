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
        /// <summary>
        /// owner 只用來查欄位的宣告型別（`owner.GetType()`），所以吃 Component（PrefabEdit /
        /// SceneEdit）或任何 UnityEngine.Object（AssetEdit 的 ScriptableObject asset）都可以。
        /// </summary>
        internal static Object Resolve(string assetPath, Object owner, string fieldPath)
        {
            if (assetPath != null && assetPath.StartsWith(BuiltinPrefix))
                return Builtin(assetPath.Substring(BuiltinPrefix.Length).Trim());

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

        private const string BuiltinPrefix = "builtin:";

        private static readonly string[] BuiltinMeshes =
            { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };

        /// <summary>
        /// Unity 內建的 primitive mesh 與 default material。它們住在 "Library/unity default
        /// resources" 裡，AssetDatabase.LoadMainAssetAtPath 讀不到（那不是真的 asset 路徑），
        /// 只能走 Resources.GetBuiltinResource。用 `builtin:Cube` / `builtin:Quad` 指定 ——
        /// 組 placeholder 幾何（螢幕面板、機殼）時很常用，不該逼人跑一趟 dynamic code。
        /// </summary>
        private static Object Builtin(string name)
        {
            foreach (var mesh in BuiltinMeshes)
                if (string.Equals(mesh, name, System.StringComparison.OrdinalIgnoreCase))
                    return Resources.GetBuiltinResource<Mesh>($"{mesh}.fbx");

            if (string.Equals("Default-Material", name, System.StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.GetBuiltinExtraResource<Material>(
                    "Default-Material.mat");

            throw new Abort(
                $"不認得的內建資源 '{name}'。可用的有：{EditResolve.Join(BuiltinMeshes)}, Default-Material");
        }
    }
}
