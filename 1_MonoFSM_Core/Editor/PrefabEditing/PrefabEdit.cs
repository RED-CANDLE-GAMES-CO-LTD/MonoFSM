using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MonoFSM.Core;
using UnityEditor;
using UnityEngine;
using Abort = MonoFSM.Editor.PrefabEditing.EditResolve.EditAbort;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 用「節點路徑」對 prefab asset 做結構編輯的四個原語，供 uloop execute-dynamic-code 一行呼叫。
    ///
    /// 這是 PrefabTextReader.Export（讀）的對稱面（寫）：同一套路徑語彙、
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
                return $"{EditResolve.Describe(nodePath)}.{comp.GetType().Name}.{fieldPath}: " +
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
                return $"{EditResolve.Describe(nodePath)}.{comp.GetType().Name}.{fieldPath} -> " +
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
                return $"{EditResolve.Describe(nodePath)}.{comp.GetType().Name}.{fieldPath} -> res:{targetAssetPath}";
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

        /// <summary>
        /// 複製一份**獨立** prefab（不建 variant 連結）—— 拿既有 prefab 當模板改，
        /// 比從零建安全（底盤 component、Rigidbody / Collider / 網路元件都跟著來）。
        /// 想保留繼承關係請改用 <see cref="CreateVariant" />。
        /// </summary>
        /// <param name="srcPath">來源 prefab 的 asset path</param>
        /// <param name="newAssetPath">新 prefab 的 asset path，要以 .prefab 結尾</param>
        /// <param name="name">root 名稱；留空就用檔名</param>
        public static string CopyAsset(string srcPath, string newAssetPath, string name = null)
        {
            if (!newAssetPath.EndsWith(".prefab"))
                return $"# 未修改：newAssetPath 要以 .prefab 結尾：{newAssetPath}";

            var src = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
            if (src == null) return $"# 未修改：找不到來源 prefab: {srcPath}";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(newAssetPath) != null)
                return $"# 未修改：{newAssetPath} 已存在，不覆蓋";

            EnsureDirectory(newAssetPath);
            if (!AssetDatabase.CopyAsset(srcPath, newAssetPath))
                return $"# 未修改：複製失敗: {srcPath} -> {newAssetPath}";
            AssetDatabase.ImportAsset(newAssetPath, ImportAssetOptions.ForceSynchronousImport);

            // 檔名換了 root 名稱不會跟著換，接下來所有路徑操作都會看到舊名字，先改掉
            var rootName = string.IsNullOrEmpty(name)
                ? System.IO.Path.GetFileNameWithoutExtension(newAssetPath)
                : name;
            var root = PrefabUtility.LoadPrefabContents(newAssetPath);
            try
            {
                root.name = rootName;
                var saved = PrefabUtility.SaveAsPrefabAsset(root, newAssetPath, out var ok);
                if (!ok || saved == null)
                    return $"# 複製了但 root 改名存檔失敗: {newAssetPath}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var isVariant = PrefabUtility.GetPrefabAssetType(
                AssetDatabase.LoadAssetAtPath<GameObject>(newAssetPath)) == PrefabAssetType.Variant;
            return $"複製 prefab {newAssetPath}\n" +
                   $"  來源: {srcPath}\n" +
                   $"  root: {rootName}" +
                   (isVariant ? "\n  注意：來源本身是 variant，複製出來的也是 variant" : "");
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
        public static string Batch(string assetPath, string ops) => Batch(assetPath, ops, false);

        /// <summary>
        /// 一次跑多行操作。quiet=true 時，成功只回 compact summary；任何操作、存檔或
        /// reload 驗證失敗仍回完整逐行 log，不能為了省輸出藏掉修正線索。
        /// </summary>
        public static string Batch(string assetPath, string ops, bool quiet)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
                return $"# 找不到 prefab: {assetPath}";

            var root = PrefabUtility.LoadPrefabContents(assetPath);
            var touches = new List<VerifyTouch>();
            try
            {
                var log = EditBatch.Run(
                    ops, (verb, a) => Dispatch(root.transform, verb, a, touches), out var done);
                // 有任何一行失敗就整批不存檔 —— 半套的 FSM 比沒改更難收拾
                if (log.Contains("# 未修改")) return log + "# 整批未存檔。";

                var callbackLog = RunBeforeSaveCallbacks(root);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, assetPath, out var saveOk);
                if (!saveOk || saved == null)
                    return log + callbackLog + $"# 存檔失敗：{assetPath}\n";

                // SaveAsPrefabAsset 會替新物件分配 local file ID。一定要在 save 後、unload 前
                // 快照，才能把內部 object reference 也轉成可跨 reload 比對的穩定 identity。
                foreach (var touch in touches) touch.Capture(root.transform);

                PrefabUtility.UnloadPrefabContents(root);
                root = null;

                var report = VerifyReloaded(assetPath, touches);
                // quiet 只壓成功輸出；驗證錯誤要把原始逐行操作一起帶回，才知道是哪一步寫的。
                var prefix = quiet && report.Failures.Count == 0 && !callbackLog.Contains("個失敗")
                    ? $"# 操作：{done} 個 OK\n"
                    : log;
                return prefix + callbackLog + "# 存檔：OK\n" + report.Format();
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 跑 IBeforePrefabSaveCallbackReceiver.OnBeforePrefabSave()。
        ///
        /// 為什麼要自己跑：Unity 只在 PrefabStage（人工打開 prefab 編輯再存）時觸發這個
        /// callback，LoadPrefabContents + SaveAsPrefabAsset 這條路不會。而專案有東西掛在上面 ——
        /// NetworkAutoSuggestVarSyncComp 就是靠它掃 subtree 的 NetworkedVarTag、自動配上
        /// 對應的 sync 元件。不跑的話，用 API 加的 networked var 會靜默沒有同步元件，
        /// 只有多人實測才會發現。
        /// </summary>
        private static string RunBeforeSaveCallbacks(GameObject root)
        {
            var receivers = root.GetComponentsInChildren<IBeforePrefabSaveCallbackReceiver>(true);
            if (receivers.Length == 0) return "# 存檔前 callback：0 個（無 receiver）\n";

            // 專案裡幾乎每個 MonoBehaviour 都實作這個介面，逐個列名字會洗掉整份 log，
            // 所以只報數量；出錯的才點名，那才是要看的東西。
            var ok = 0;
            var failed = new List<string>();
            foreach (var receiver in receivers)
            {
                if (receiver == null) continue;
                try
                {
                    receiver.OnBeforePrefabSave();
                    ok++;
                }
                catch (Exception e)
                {
                    // 一個 callback 炸掉不該讓整批改動消失，但一定要講出來
                    failed.Add($"{receiver.GetType().Name}({e.GetType().Name}: {e.Message})");
                }
            }

            return $"# 存檔前 callback：{ok} 個 OK" +
                   (failed.Count > 0 ? $"，{failed.Count} 個失敗 -> {string.Join("; ", failed)}" : "") +
                   "\n";
        }

        private static string Dispatch(
            Transform root, string verb, string[] a, List<VerifyTouch> touches)
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
                    EditBatch.Touch(full);
                    return $"建立 {full}  <{EditResolve.Join(added)}>";
                }
                case "prefab":
                {
                    // 放 nested prefab 實例（模組 prefab 裝進宿主 prefab 用）
                    var prefabPath = EditBatch.Need(a, 0, verb, "prefabPath");
                    var parentPath = EditBatch.At(a, 1);
                    var name = EditBatch.At(a, 2);
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (asset == null) throw new Abort($"找不到 prefab: {prefabPath}");

                    var parent = EditResolve.Node(root, parentPath);
                    var nodeName = string.IsNullOrEmpty(name) ? asset.name : name;
                    if (parent.Find(nodeName) != null)
                        return $"（跳過）{EditResolve.Describe(parentPath)}/{nodeName} 已存在";

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                    if (instance == null) throw new Abort($"實例化失敗: {prefabPath}");
                    instance.transform.SetParent(parent, false);
                    instance.name = nodeName;

                    var fullPath = string.IsNullOrEmpty(parentPath)
                        ? nodeName
                        : $"{parentPath}/{nodeName}";
                    EditBatch.Touch(fullPath);
                    return $"放入 {fullPath}  <- res:{prefabPath}";
                }
                case "comp":
                {
                    // 留空 = root（MonoEntity / MonoObj / NetworkObject 都掛在 root 上），跟 `add` / `delcomp` 一致
                    var nodePath = EditBatch.At(a, 0);
                    var node = EditResolve.Node(root, nodePath);
                    var added = new List<string>();
                    foreach (var typeName in EditBatch.Types(a, 1))
                    {
                        var type = EditResolve.CompType(typeName);
                        if (node.GetComponent(type) != null) continue;
                        node.gameObject.AddComponent(type);
                        added.Add(type.Name);
                    }

                    return $"{EditResolve.Describe(nodePath)} += <{EditResolve.Join(added)}>";
                }
                case "set":
                {
                    // 留空 = root（MonoEntity / MonoObj / NetworkObject 都掛在 root 上），跟 `add` / `delcomp` 一致
                    var nodePath = EditBatch.At(a, 0);
                    var fieldPath = EditBatch.Need(a, 2, verb, "fieldPath");
                    var comp = EditResolve.Comp(EditResolve.Node(root, nodePath), nodePath,
                        EditBatch.Need(a, 1, verb, "componentType"));
                    var so = new SerializedObject(comp);
                    var prop = EditResolve.Prop(so, fieldPath, comp);
                    var before = EditResolve.Preview(prop);
                    EditResolve.ApplyValue(prop, EditBatch.At(a, 3) ?? "", fieldPath);
                    so.ApplyModifiedPropertiesWithoutUndo();
                    touches.Add(VerifyTouch.Serialized(comp, fieldPath, verb));
                    return $"{EditResolve.Describe(nodePath)}.{comp.GetType().Name}.{fieldPath}: " +
                           $"{before} -> {EditResolve.Preview(prop)}";
                }
                case "ref":
                {
                    // 留空 = root（MonoEntity / MonoObj / NetworkObject 都掛在 root 上），跟 `add` / `delcomp` 一致。
                    // targetNodePath 同樣可留空 = 指向 root 自己（例如 _overrideRoot 這種要接 prefab 根節點 Transform 的欄位）。
                    var nodePath = EditBatch.At(a, 0);
                    var fieldPath = EditBatch.Need(a, 2, verb, "fieldPath");
                    var targetPath = EditBatch.At(a, 3);
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
                    touches.Add(VerifyTouch.Serialized(comp, fieldPath, verb));
                    return $"{EditResolve.Describe(nodePath)}.{comp.GetType().Name}.{fieldPath} -> " +
                           $"{EditResolve.Describe(targetPath)}.{targetComp.GetType().Name}";
                }
                case "aref":
                {
                    // 留空 = root（MonoEntity / MonoObj / NetworkObject 都掛在 root 上），跟 `add` / `delcomp` 一致
                    var nodePath = EditBatch.At(a, 0);
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
                    touches.Add(VerifyTouch.Serialized(comp, fieldPath, verb));
                    return $"{EditResolve.Describe(nodePath)}.{comp.GetType().Name}.{fieldPath} -> res:{target}";
                }
                case "addel":
                {
                    // 留空 = root（MonoEntity / MonoObj / NetworkObject 都掛在 root 上），跟 `add` / `delcomp` 一致
                    var nodePath = EditBatch.At(a, 0);
                    var fieldPath = EditBatch.Need(a, 2, verb, "fieldPath");
                    var comp = EditResolve.Comp(EditResolve.Node(root, nodePath), nodePath,
                        EditBatch.Need(a, 1, verb, "componentType"));
                    var so = new SerializedObject(comp);
                    var prop = EditResolve.Prop(so, fieldPath, comp);
                    var index = EditResolve.AddArrayElement(prop, fieldPath);
                    so.ApplyModifiedPropertiesWithoutUndo();
                    touches.Add(VerifyTouch.Serialized(comp, fieldPath, verb));
                    return $"{EditResolve.Describe(nodePath)}.{comp.GetType().Name}.{fieldPath}[{index}] " +
                           $"新增（現有 {prop.arraySize} 筆）";
                }
                case "pos":
                {
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var node = EditResolve.Node(root, nodePath);
                    node.localPosition = EditBatch.Vec3(a, 1, verb, "pos");
                    touches.Add(VerifyTouch.TransformValue(node, VerifyKind.LocalPosition, verb));
                    return $"{EditResolve.Describe(nodePath)}.localPosition = {node.localPosition}";
                }
                case "scale":
                {
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var node = EditResolve.Node(root, nodePath);
                    node.localScale = EditBatch.Vec3(a, 1, verb, "scale");
                    touches.Add(VerifyTouch.TransformValue(node, VerifyKind.LocalScale, verb));
                    return $"{EditResolve.Describe(nodePath)}.localScale = {node.localScale}";
                }
                case "rot":
                {
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var node = EditResolve.Node(root, nodePath);
                    node.localEulerAngles = EditBatch.Vec3(a, 1, verb, "rot");
                    touches.Add(VerifyTouch.TransformValue(node, VerifyKind.LocalEulerAngles, verb));
                    return $"{EditResolve.Describe(nodePath)}.localEulerAngles = {node.localEulerAngles}";
                }
                case "active":
                {
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var active = EditBatch.Bool(a, 1, verb);
                    var node = EditResolve.Node(root, nodePath);
                    node.gameObject.SetActive(active);
                    EditorUtility.SetDirty(node.gameObject);
                    // nested prefab / variant 的 activeSelf 必須成為外層的 property override；只
                    // SetActive 在部分 nested instance 上會看似成功，Save 後卻消失。
                    if (PrefabUtility.IsPartOfPrefabInstance(node.gameObject))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(node.gameObject);
                    touches.Add(VerifyTouch.TransformValue(node, VerifyKind.ActiveSelf, verb));
                    return $"{EditResolve.Describe(nodePath)}.activeSelf = {active}";
                }
                case "idx":
                {
                    // sibling 順序在 MonoFSM 裡是語意的一部分：value source / condition 依 child
                    // 順序取第一個成立的，所以「排第幾」＝優先序。負數 = 從尾端算（-1 = 最後）。
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var node = EditResolve.Node(root, nodePath);
                    if (node.parent == null)
                        throw new Abort($"'{nodePath}' 是 root，沒有 sibling 順序可調");
                    var count = node.parent.childCount;
                    var want = EditBatch.Int(a, 1, verb, "siblingIndex");
                    var target = want < 0 ? count + want : want;
                    if (target < 0 || target >= count)
                        throw new Abort(
                            $"siblingIndex {want} 超出範圍：'{node.parent.name}' 底下有 {count} 個子節點" +
                            $"（可用 0..{count - 1}，或 -1..-{count}）");
                    var before = node.GetSiblingIndex();
                    node.SetSiblingIndex(target);
                    return $"{nodePath} sibling index: {before} -> {node.GetSiblingIndex()}";
                }
                case "mv":
                {
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var newParentPath = EditBatch.At(a, 1);
                    var node = EditResolve.Node(root, nodePath);
                    if (string.IsNullOrEmpty(newParentPath))
                    {
                        node.SetParent(root, false);
                        return $"{nodePath} -> (root)";
                    }

                    var parent = EditResolve.Node(root, newParentPath);
                    if (parent.IsChildOf(node))
                        throw new Abort($"'{newParentPath}' 在 '{nodePath}' 底下，會造成迴圈");
                    node.SetParent(parent, false);
                    return $"{nodePath} -> {newParentPath}/{node.name}";
                }
                case "auto":
                {
                    var node = EditResolve.Node(root, EditBatch.At(a, 0));
                    var result = EditResolve.RunAuto(node);
                    // Auto 可能碰任意 [Auto*] 欄位，目前沒有可枚舉「實際改了哪些欄位」的
                    // API。明確列 unsupported，避免把 reload 成功誤報成完整驗證。
                    touches.Add(VerifyTouch.Unsupported(node, verb,
                        "auto 會改任意 [Auto*] 欄位，無法完整枚舉"));
                    return result;
                }
                case "rename":
                {
                    // 留空 = root（複製模板後 root 名字還是舊的，這是最常見的用途）
                    var nodePath = EditBatch.At(a, 0);
                    var newName = EditBatch.Need(a, 1, verb, "newName");
                    var node = EditResolve.Node(root, nodePath);
                    var before = node.name;
                    node.name = newName;
                    return $"{EditResolve.Describe(nodePath)} 改名: {before} -> {newName}";
                }
                case "del":
                {
                    var nodePath = EditBatch.Need(a, 0, verb, "nodePath");
                    var node = EditResolve.Node(root, nodePath);
                    var count = EditResolve.CountDescendants(node);
                    UnityEngine.Object.DestroyImmediate(node.gameObject);
                    return $"刪除 {nodePath}（含 {count} 個子節點）";
                }
                case "delcomp":
                {
                    // 留空 = root（marker 這類常掛在 root 上），跟 `add` 的 parent 一致
                    var nodePath = EditBatch.At(a, 0);
                    var node = EditResolve.Node(root, nodePath);
                    var removed = new List<string>();
                    foreach (var typeName in EditBatch.Types(a, 1))
                    {
                        var comp = node.GetComponent(EditResolve.CompType(typeName));
                        // 不存在就跳過而不是 abort —— 語意是「確保這個 component 不在」
                        if (comp == null) continue;
                        removed.Add(comp.GetType().Name);
                        UnityEngine.Object.DestroyImmediate(comp, true);
                    }

                    return removed.Count == 0
                        ? $"（跳過）{EditResolve.Describe(nodePath)} 上沒有那些 component"
                        : $"{EditResolve.Describe(nodePath)} -= <{EditResolve.Join(removed)}>";
                }
                case "delmissing":
                {
                    // 已刪掉 C# 型別後，Unity 只剩 null MonoBehaviour，無法再用 delcomp 的型別名解析。
                    var nodePath = EditBatch.At(a, 0);
                    var node = EditResolve.Node(root, nodePath);
                    var removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject);
                    return removed == 0
                        ? $"（跳過）{EditResolve.Describe(nodePath)} 上沒有 MissingScript"
                        : $"{EditResolve.Describe(nodePath)} -= <MissingScript x{removed}>";
                }
                default:
                {
                    var ctx = new EditFsm.Ctx { Node = p => EditResolve.Node(root, p) };
                    if (EditFsm.TryDispatch(ctx, verb, a, out var fsm)) return fsm;
                    throw new Abort(
                        $"prefab batch 不支援 '{verb}'。可用的：add comp set ref aref addel pos scale rot active idx mv auto rename del delcomp delmissing mark " +
                        EditFsm.Verbs + "（save 只有 SceneEdit 有）");
                }
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

                var saved = PrefabUtility.SaveAsPrefabAsset(root, assetPath, out var ok);
                return ok && saved != null
                    ? message
                    : message + $"\n# 存檔失敗：{assetPath}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---- 存檔後 reload 驗證 ----

        private enum VerifyKind
        {
            Serialized,
            ActiveSelf,
            LocalPosition,
            LocalScale,
            LocalEulerAngles,
            Unsupported
        }

        /// <summary>
        /// 持有 loaded contents 裡的 object 到 Save 完成，再快照成 path/type/value。Unload 之後
        /// 不保留任何 UnityEngine.Object reference，確保驗證真的讀的是重新載入的資料。
        /// </summary>
        private sealed class VerifyTouch
        {
            private readonly VerifyKind _kind;
            private readonly Component _component;
            private readonly Transform _node;
            private readonly string _fieldPath;
            private readonly string _verb;
            private readonly string _unsupportedReason;

            private string _nodePath;
            private string _componentType;
            private string _expected;
            private string _captureError;

            private VerifyTouch(
                VerifyKind kind, Component component, Transform node, string fieldPath,
                string verb, string unsupportedReason)
            {
                _kind = kind;
                _component = component;
                _node = node;
                _fieldPath = fieldPath;
                _verb = verb;
                _unsupportedReason = unsupportedReason;
            }

            internal static VerifyTouch Serialized(Component component, string fieldPath, string verb) =>
                new(VerifyKind.Serialized, component, component.transform, fieldPath, verb, null);

            internal static VerifyTouch TransformValue(Transform node, VerifyKind kind, string verb) =>
                new(kind, null, node, null, verb, null);

            internal static VerifyTouch Unsupported(Transform node, string verb, string reason) =>
                new(VerifyKind.Unsupported, null, node, null, verb, reason);

            internal bool IsUnsupported => _kind == VerifyKind.Unsupported || _captureError != null;
            internal string UnsupportedReason => _captureError ?? _unsupportedReason;
            internal string Label => _kind == VerifyKind.Serialized
                ? $"{EditResolve.Describe(_nodePath)}.{ShortType(_componentType)}.{_fieldPath}"
                : $"{EditResolve.Describe(_nodePath)}.{KindName(_kind)}";

            internal void Capture(Transform root)
            {
                if (_node == null)
                {
                    _captureError = $"{_verb} 的 target 被後續操作刪除，無法驗證";
                    return;
                }

                _nodePath = EditResolve.PathOf(root, _node);
                if (_nodePath == null)
                {
                    _captureError = $"{_verb} 的 target 已不在 prefab root 底下";
                    return;
                }

                if (_kind == VerifyKind.Unsupported) return;

                try
                {
                    switch (_kind)
                    {
                        case VerifyKind.Serialized:
                        {
                            if (_component == null)
                            {
                                _captureError = $"{_verb} 的 component 被後續操作刪除，無法驗證";
                                return;
                            }

                            _componentType = _component.GetType().FullName;
                            var so = new SerializedObject(_component);
                            var prop = EditResolve.Prop(so, _fieldPath, _component);
                            _expected = Snapshot(prop, root);
                            if (_expected.StartsWith("unsupported-property:"))
                                _captureError = $"{_verb} 欄位型別 {prop.propertyType} 尚未支援 reload 驗證";
                            break;
                        }
                        case VerifyKind.ActiveSelf:
                            _expected = _node.gameObject.activeSelf ? "true" : "false";
                            break;
                        case VerifyKind.LocalPosition:
                            _expected = Vector(_node.localPosition);
                            break;
                        case VerifyKind.LocalScale:
                            _expected = Vector(_node.localScale);
                            break;
                        case VerifyKind.LocalEulerAngles:
                            // Transform 實際序列化的是 quaternion；Euler angle 有多種等價表示，
                            // reload 後直接比 Euler 會製造假 mismatch。
                            _expected = Quaternion(_node.localRotation);
                            break;
                    }
                }
                catch (Exception e)
                {
                    _captureError = $"{_verb} 快照失敗：{e.GetType().Name}: {e.Message}";
                }
            }

            /// <returns>null = 驗證成功；否則為完整 mismatch 訊息。</returns>
            internal string Verify(Transform reloadedRoot)
            {
                if (IsUnsupported) return null;

                try
                {
                    var node = EditResolve.TryNode(reloadedRoot, _nodePath);
                    if (node == null) return $"{Label}：reload 後找不到節點";

                    string actual;
                    switch (_kind)
                    {
                        case VerifyKind.Serialized:
                        {
                            var comp = EditResolve.Comp(node, _nodePath, _componentType);
                            var so = new SerializedObject(comp);
                            actual = Snapshot(EditResolve.Prop(so, _fieldPath, comp), reloadedRoot);
                            break;
                        }
                        case VerifyKind.ActiveSelf:
                            actual = node.gameObject.activeSelf ? "true" : "false";
                            break;
                        case VerifyKind.LocalPosition:
                            actual = Vector(node.localPosition);
                            break;
                        case VerifyKind.LocalScale:
                            actual = Vector(node.localScale);
                            break;
                        case VerifyKind.LocalEulerAngles:
                            actual = Quaternion(node.localRotation);
                            break;
                        default:
                            return null;
                    }

                    return actual == _expected
                        ? null
                        : $"{Label}：expected {_expected}，reload got {actual}";
                }
                catch (Exception e)
                {
                    return $"{Label}：{e.GetType().Name}: {e.Message}";
                }
            }

            private static string ShortType(string fullName)
            {
                if (string.IsNullOrEmpty(fullName)) return "component";
                var dot = fullName.LastIndexOf('.');
                return dot < 0 ? fullName : fullName.Substring(dot + 1);
            }

            private static string KindName(VerifyKind kind) => kind switch
            {
                VerifyKind.ActiveSelf => "activeSelf",
                VerifyKind.LocalPosition => "localPosition",
                VerifyKind.LocalScale => "localScale",
                VerifyKind.LocalEulerAngles => "localEulerAngles",
                VerifyKind.Unsupported => "auto",
                _ => kind.ToString()
            };
        }

        private sealed class VerifyReport
        {
            internal int Verified;
            internal int Unsupported;
            internal readonly List<string> Failures = new();
            internal readonly List<string> UnsupportedReasons = new();

            internal string Format()
            {
                var text = $"# 驗證（set/ref/aref/addel/active/transform）：" +
                           $"{Verified} 個 OK，{Failures.Count} 個失敗，{Unsupported} 個 unsupported";
                if (UnsupportedReasons.Count > 0)
                    text += $"（{string.Join("；", UnsupportedReasons.Distinct())}）";
                text += "\n";
                if (Failures.Count > 0)
                    text += "# 驗證失敗明細：\n" + string.Join("\n", Failures.Select(f => "# - " + f)) + "\n";
                return text;
            }
        }

        private static VerifyReport VerifyReloaded(string assetPath, List<VerifyTouch> touches)
        {
            var report = new VerifyReport();
            foreach (var touch in touches)
            {
                if (!touch.IsUnsupported) continue;
                report.Unsupported++;
                if (!string.IsNullOrEmpty(touch.UnsupportedReason))
                    report.UnsupportedReasons.Add(touch.UnsupportedReason);
            }

            GameObject reloaded = null;
            try
            {
                reloaded = PrefabUtility.LoadPrefabContents(assetPath);
                if (reloaded == null)
                {
                    report.Failures.Add($"reload 失敗：{assetPath}");
                    return report;
                }

                foreach (var touch in touches)
                {
                    if (touch.IsUnsupported) continue;
                    var failure = touch.Verify(reloaded.transform);
                    if (failure == null) report.Verified++;
                    else report.Failures.Add(failure);
                }
            }
            catch (Exception e)
            {
                report.Failures.Add($"reload 驗證例外：{e.GetType().Name}: {e.Message}");
            }
            finally
            {
                if (reloaded != null) PrefabUtility.UnloadPrefabContents(reloaded);
            }

            return report;
        }

        private static string Snapshot(SerializedProperty prop, Transform root)
        {
            if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                return $"array-size:{prop.arraySize}";

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                    return prop.floatValue.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.Integer:
                    return prop.longValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Boolean:
                    return prop.boolValue ? "true" : "false";
                case SerializedPropertyType.String:
                    return "string:" + prop.stringValue;
                case SerializedPropertyType.Enum:
                    return $"enum:{prop.enumValueIndex}";
                case SerializedPropertyType.Vector2:
                    return Vector(prop.vector2Value);
                case SerializedPropertyType.Vector3:
                    return Vector(prop.vector3Value);
                case SerializedPropertyType.Color:
                    return Color(prop.colorValue);
                case SerializedPropertyType.LayerMask:
                    return $"layer:{prop.intValue}";
                case SerializedPropertyType.ObjectReference:
                    return Reference(prop.objectReferenceValue, root);
                default:
                    return $"unsupported-property:{prop.propertyType}";
            }
        }

        private static string Reference(UnityEngine.Object value, Transform root)
        {
            if (value == null) return "ref:null";

            var transform = value switch
            {
                GameObject go => go.transform,
                Component component => component.transform,
                _ => null
            };
            var path = transform == null ? null : EditResolve.PathOf(root, transform);
            if (path != null)
                return $"ref-path:{path}:{value.GetType().FullName}";

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out var guid, out long fileId))
                return $"ref:{guid}:{fileId}";

            return $"ref-fallback:{AssetDatabase.GetAssetPath(value)}:{value.GetType().FullName}:{value.name}";
        }

        private static string Vector(Vector2 value) =>
            $"v2:{value.x.ToString("R", CultureInfo.InvariantCulture)}," +
            value.y.ToString("R", CultureInfo.InvariantCulture);

        private static string Vector(Vector3 value) =>
            $"v3:{value.x.ToString("R", CultureInfo.InvariantCulture)}," +
            value.y.ToString("R", CultureInfo.InvariantCulture) + "," +
            value.z.ToString("R", CultureInfo.InvariantCulture);

        private static string Color(Color value) =>
            $"color:{value.r.ToString("R", CultureInfo.InvariantCulture)}," +
            value.g.ToString("R", CultureInfo.InvariantCulture) + "," +
            value.b.ToString("R", CultureInfo.InvariantCulture) + "," +
            value.a.ToString("R", CultureInfo.InvariantCulture);

        private static string Quaternion(Quaternion value) =>
            $"q:{value.x.ToString("R", CultureInfo.InvariantCulture)}," +
            value.y.ToString("R", CultureInfo.InvariantCulture) + "," +
            value.z.ToString("R", CultureInfo.InvariantCulture) + "," +
            value.w.ToString("R", CultureInfo.InvariantCulture);
    }
}
