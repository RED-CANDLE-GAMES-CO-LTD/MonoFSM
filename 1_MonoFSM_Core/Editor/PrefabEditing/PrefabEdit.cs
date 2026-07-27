using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Abort = MonoFSM.Editor.PrefabEditing.EditResolve.EditAbort;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 用「節點路徑」對 prefab asset 做結構編輯的四個原語，供 uloop execute-dynamic-code 一行呼叫。
    ///
    /// 這是 PrefabTextCacheWriter.ExportSubtree（讀）的對稱面（寫）：同一套路徑語彙、
    /// 同樣在路徑打錯時列出該層實際子節點，不用先知道 fileID 或 instanceID。
    ///
    /// 路徑 / 型別 / 欄位的解析與錯誤訊息在 EditResolve，跟 SceneEdit 共用。
    ///
    /// 為什麼不做成 MenuItem：MenuItem 無法帶參數，而「在 X 下建 Y 型別、把 Z 欄位指向 W」
    /// 天生就是參數化操作。做成 static API 才能被 dynamic code 組合。
    /// </summary>
    public static class PrefabEdit
    {
        /// <summary>
        /// 在 parentPath 底下建一個子節點並掛上 component。
        /// </summary>
        /// <param name="assetPath">prefab asset path</param>
        /// <param name="parentPath">父節點相對 root 的路徑；留空 = 直接掛在 root 下</param>
        /// <param name="name">新節點名稱（MonoFSM 慣例會帶 [Tag] 前綴）</param>
        /// <param name="componentTypes">要掛的 component 型別名（短名或 FullName）</param>
        public static string AddNode(
            string assetPath, string parentPath, string name, params string[] componentTypes)
        {
            return Edit(assetPath, root =>
            {
                var parent = EditResolve.Node(root, parentPath);
                if (parent.Find(name) != null)
                    throw new Abort(
                        $"'{name}' 已存在於 {EditResolve.Describe(parentPath)}，不重複建立");

                var go = new GameObject(name);
                go.transform.SetParent(parent, false);

                var added = new List<string>();
                foreach (var typeName in componentTypes ?? Array.Empty<string>())
                {
                    var type = EditResolve.CompType(typeName);
                    if (go.GetComponent(type) != null) continue;
                    go.AddComponent(type);
                    added.Add(type.Name);
                }

                var full = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
                return $"建立 {full}  <{string.Join(", ", added)}>";
            });
        }

        /// <summary>
        /// 設定 serialized 欄位的值（非物件引用）。fieldPath 支援巢狀，如 _rateVar._tempValue。
        /// </summary>
        public static string SetField(
            string assetPath, string nodePath, string componentType, string fieldPath, object value)
        {
            return Edit(assetPath, root =>
            {
                var node = EditResolve.Node(root, nodePath);
                var comp = EditResolve.Comp(node, nodePath, componentType);
                var so = new SerializedObject(comp);
                var prop = EditResolve.Prop(so, fieldPath, comp);
                var before = EditResolve.Preview(prop);
                EditResolve.ApplyValue(prop, value, fieldPath);
                so.ApplyModifiedPropertiesWithoutUndo();
                return $"{nodePath}.{comp.GetType().Name}.{fieldPath}: " +
                       $"{before} -> {EditResolve.Preview(prop)}";
            });
        }

        /// <summary>
        /// 把欄位指向另一個節點上的 component（MonoFSM 最常用的操作：Action 指向某個 Var）。
        /// targetComponentType 省略時，用欄位的宣告型別去目標節點上找。
        /// </summary>
        public static string SetRef(
            string assetPath, string nodePath, string componentType, string fieldPath,
            string targetNodePath, string targetComponentType = null)
        {
            return Edit(assetPath, root =>
            {
                var node = EditResolve.Node(root, nodePath);
                var comp = EditResolve.Comp(node, nodePath, componentType);
                var so = new SerializedObject(comp);
                var prop = EditResolve.Prop(so, fieldPath, comp);
                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                    throw new Abort(
                        $"'{fieldPath}' 是 {prop.propertyType}，不是物件引用；請改用 SetField");

                var target = EditResolve.Node(root, targetNodePath);
                var targetComp = EditResolve.RefTarget(
                    target, targetNodePath, comp, fieldPath, targetComponentType);

                prop.objectReferenceValue = targetComp;
                so.ApplyModifiedPropertiesWithoutUndo();
                return $"{nodePath}.{comp.GetType().Name}.{fieldPath} -> " +
                       $"{targetNodePath}.{targetComp.GetType().Name}";
            });
        }

        /// <summary>
        /// 把欄位指向一個 asset（prefab / ScriptableObject）。prefab 會取其上的 component
        /// 或 GameObject 本身，依欄位宣告型別決定。
        /// </summary>
        public static string SetAssetRef(
            string assetPath, string nodePath, string componentType, string fieldPath,
            string targetAssetPath)
        {
            return Edit(assetPath, root =>
            {
                var node = EditResolve.Node(root, nodePath);
                var comp = EditResolve.Comp(node, nodePath, componentType);
                var so = new SerializedObject(comp);
                var prop = EditResolve.Prop(so, fieldPath, comp);
                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                    throw new Abort($"'{fieldPath}' 是 {prop.propertyType}，不是物件引用");

                prop.objectReferenceValue = AssetRef.Resolve(targetAssetPath, comp, fieldPath);
                so.ApplyModifiedPropertiesWithoutUndo();
                return $"{nodePath}.{comp.GetType().Name}.{fieldPath} -> res:{targetAssetPath}";
            });
        }

        public static string DeleteNode(string assetPath, string nodePath)
        {
            return Edit(assetPath, root =>
            {
                if (string.IsNullOrEmpty(nodePath))
                    throw new Abort("不能刪 root");
                var node = EditResolve.Node(root, nodePath);
                var count = EditResolve.CountDescendants(node);
                UnityEngine.Object.DestroyImmediate(node.gameObject);
                return $"刪除 {nodePath}（含 {count} 個子節點）";
            });
        }

        /// <summary>
        /// 建一個 variant（不是從零開一個新 prefab）。
        ///
        /// 為什麼是 variant：這個專案的 prefab 帶著大量共用底盤 —— MonoEntity / MonoObj /
        /// NetworkObject / Culling / ModulePack。從零建一個「只掛必要 component」的 prefab
        /// 看起來乾淨，實際上會漏掉那些底盤，spawn 進場就出錯，而且之後底盤改了它也跟不上。
        /// 從既有 premade 開 variant 才是專案的正確做法。
        /// </summary>
        /// <param name="basePath">base prefab 的 asset path（可以是 Packages/… 底下的）</param>
        /// <param name="newAssetPath">新 variant 的 asset path，要以 .prefab 結尾</param>
        /// <param name="name">root 名稱；留空就用檔名</param>
        public static string CreateVariant(string basePath, string newAssetPath, string name = null)
        {
            if (!newAssetPath.EndsWith(".prefab"))
                return $"# 未修改：newAssetPath 要以 .prefab 結尾：{newAssetPath}";

            var baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (baseAsset == null) return $"# 未修改：找不到 base prefab: {basePath}";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(newAssetPath) != null)
                return $"# 未修改：{newAssetPath} 已存在，不覆蓋";

            EnsureDirectory(newAssetPath);

            // 對「prefab 實例」SaveAsPrefabAsset 就會存成 variant（連結保留）；
            // 對一般 GameObject 才是存成獨立 prefab。差別就在 instance 這一步。
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset);
            if (instance == null) return $"# 未修改：實例化失敗: {basePath}";
            try
            {
                instance.name = string.IsNullOrEmpty(name)
                    ? System.IO.Path.GetFileNameWithoutExtension(newAssetPath)
                    : name;

                var variant = PrefabUtility.SaveAsPrefabAsset(instance, newAssetPath, out var ok);
                if (!ok || variant == null) return $"# 未修改：存檔失敗: {newAssetPath}";

                var isVariant = PrefabUtility.GetPrefabAssetType(variant) == PrefabAssetType.Variant;
                return $"建立 variant {newAssetPath}\n" +
                       $"  base: {basePath}\n" +
                       $"  variant 連結: {(isVariant ? "有" : "無（存成獨立 prefab 了，檢查 base 是否也是 variant）")}";
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        // AssetDatabase 不會自己建中間資料夾
        private static void EnsureDirectory(string assetPath)
        {
            var dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || AssetDatabase.IsValidFolder(dir)) return;

            var parts = dir.Split('/');
            var cursor = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{cursor}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cursor, parts[i]);
                cursor = next;
            }
        }

        /// <summary>
        /// 一次跑多行操作（語法見 EditBatch，`prefab` / `pos` / `mv` / `save` 不適用）。
        /// **整批共用一次 LoadPrefabContents / SaveAsPrefabAsset** —— 逐個原語呼叫的話，
        /// 建一個 FSM 會 load/save 幾十次，慢且每次都重跑一遍 cache 更新。
        /// </summary>
        public static string Batch(string assetPath, string ops)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
                return $"# 找不到 prefab: {assetPath}";

            var root = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                var log = EditBatch.Run(ops, (verb, a) => Dispatch(root.transform, verb, a));
                // 有任何一行失敗就整批不存檔 —— 半套的 FSM 比沒改更難收拾
                if (log.Contains("# 未修改")) return log + "# 整批未存檔。";
                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                return log + AfterSaveNote(assetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string Dispatch(Transform root, string verb, string[] a)
        {
            switch (verb)
            {
                case "add":
                {
                    var parentPath = EditBatch.At(a, 0);
                    var name = EditBatch.Need(a, 1, verb, "name");
                    var parent = EditResolve.Node(root, parentPath);
                    // 已存在就跳過而不是 abort，理由同 SceneEdit.AddNode
                    if (parent.Find(name) != null)
                        return $"（跳過）{EditResolve.Describe(parentPath)}/{name} 已存在";

                    var go = new GameObject(name);
                    go.transform.SetParent(parent, false);
                    var added = new List<string>();
                    foreach (var typeName in EditBatch.Types(a, 2))
                    {
                        var type = EditResolve.CompType(typeName);
                        if (go.GetComponent(type) != null) continue;
                        go.AddComponent(type);
                        added.Add(type.Name);
                    }

                    var full = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
                    return $"建立 {full}  <{EditResolve.Join(added)}>";
                }
                case "comp":
                {
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var node = EditResolve.Node(root, nodePath);
                    var added = new List<string>();
                    foreach (var typeName in EditBatch.Types(a, 1))
                    {
                        var type = EditResolve.CompType(typeName);
                        if (node.GetComponent(type) != null) continue;
                        node.gameObject.AddComponent(type);
                        added.Add(type.Name);
                    }

                    return $"{nodePath} += <{EditResolve.Join(added)}>";
                }
                case "set":
                {
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var fieldPath = EditBatch.Need(a, 2, verb, "fieldPath");
                    var comp = EditResolve.Comp(EditResolve.Node(root, nodePath), nodePath,
                        EditBatch.Need(a, 1, verb, "componentType"));
                    var so = new SerializedObject(comp);
                    var prop = EditResolve.Prop(so, fieldPath, comp);
                    var before = EditResolve.Preview(prop);
                    EditResolve.ApplyValue(prop, EditBatch.At(a, 3) ?? "", fieldPath);
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return $"{nodePath}.{comp.GetType().Name}.{fieldPath}: " +
                           $"{before} -> {EditResolve.Preview(prop)}";
                }
                case "ref":
                {
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var fieldPath = EditBatch.Need(a, 2, verb, "fieldPath");
                    var targetPath = EditBatch.Need(a, 3, verb, "targetNodePath");
                    var comp = EditResolve.Comp(EditResolve.Node(root, nodePath), nodePath,
                        EditBatch.Need(a, 1, verb, "componentType"));
                    var so = new SerializedObject(comp);
                    var prop = EditResolve.Prop(so, fieldPath, comp);
                    if (prop.propertyType != SerializedPropertyType.ObjectReference)
                        throw new Abort(
                            $"'{fieldPath}' 是 {prop.propertyType}，不是物件引用；請改用 set");
                    var targetComp = EditResolve.RefTarget(
                        EditResolve.Node(root, targetPath), targetPath, comp, fieldPath,
                        EditBatch.At(a, 4));
                    prop.objectReferenceValue = targetComp;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return $"{nodePath}.{comp.GetType().Name}.{fieldPath} -> " +
                           $"{targetPath}.{targetComp.GetType().Name}";
                }
                case "aref":
                {
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var fieldPath = EditBatch.Need(a, 2, verb, "fieldPath");
                    var target = EditBatch.Need(a, 3, verb, "assetPath");
                    var comp = EditResolve.Comp(EditResolve.Node(root, nodePath), nodePath,
                        EditBatch.Need(a, 1, verb, "componentType"));
                    var so = new SerializedObject(comp);
                    var prop = EditResolve.Prop(so, fieldPath, comp);
                    if (prop.propertyType != SerializedPropertyType.ObjectReference)
                        throw new Abort($"'{fieldPath}' 是 {prop.propertyType}，不是物件引用");
                    prop.objectReferenceValue = AssetRef.Resolve(target, comp, fieldPath);
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return $"{nodePath}.{comp.GetType().Name}.{fieldPath} -> res:{target}";
                }
                case "auto":
                    return EditResolve.RunAuto(
                        EditResolve.Node(root, EditBatch.At(a, 0)));
                case "del":
                {
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var node = EditResolve.Node(root, nodePath);
                    var count = EditResolve.CountDescendants(node);
                    UnityEngine.Object.DestroyImmediate(node.gameObject);
                    return $"刪除 {nodePath}（含 {count} 個子節點）";
                }
                default:
                    throw new Abort(
                        $"prefab batch 不支援 '{verb}'。可用的：add comp set ref aref auto del" +
                        "（prefab / pos / mv / save 只有 SceneEdit 有）");
            }
        }

        // ---- 共用 session ----

        private static string Edit(string assetPath, Func<Transform, string> body)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
                return $"# 找不到 prefab: {assetPath}";

            var root = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                string message;
                try
                {
                    message = body(root.transform);
                }
                catch (Abort abort)
                {
                    // 沒存檔，prefab 維持原狀
                    return $"# 未修改：{abort.Message}";
                }

                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                return message + AfterSaveNote(assetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // LoadPrefabContents 不會觸發 IBeforePrefabSaveCallbackReceiver，
        // 所以 prefab text cache 得在這裡主動更新，否則 cache 會跟 prefab 不同步
        private static string AfterSaveNote(string assetPath)
        {
            try
            {
                PrefabTextCacheWriter.RefreshCacheFor(assetPath);
                return "";
            }
            catch (Exception e)
            {
                return $"\n# 已存檔，但 cache 更新失敗（cache 可能過時）：{e.Message}";
            }
        }
    }
}
