using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 用「節點路徑」對 prefab asset 做結構編輯的四個原語，供 uloop execute-dynamic-code 一行呼叫。
    ///
    /// 這是 PrefabTextCacheWriter.ExportSubtree（讀）的對稱面（寫）：同一套路徑語彙、
    /// 同樣在路徑打錯時列出該層實際子節點，不用先知道 fileID 或 instanceID。
    ///
    /// 為什麼不做成 MenuItem：MenuItem 無法帶參數，而「在 X 下建 Y 型別、把 Z 欄位指向 W」
    /// 天生就是參數化操作。做成 static API 才能被 dynamic code 組合。
    /// </summary>
    public static class PrefabEdit
    {
        // 路徑/型別解析失敗時拋這個，Edit() 攔下來就不會存檔 —— 不留半殘 prefab
        private class EditAbort : Exception
        {
            public EditAbort(string message) : base(message) { }
        }

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
                var parent = ResolveNode(root, parentPath);
                if (parent.Find(name) != null)
                    throw new EditAbort($"'{name}' 已存在於 {Describe(parentPath)}，不重複建立");

                var go = new GameObject(name);
                go.transform.SetParent(parent, false);

                var added = new List<string>();
                foreach (var typeName in componentTypes ?? Array.Empty<string>())
                {
                    var type = ResolveComponentType(typeName);
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
                var comp = ResolveComponent(root, nodePath, componentType);
                var so = new SerializedObject(comp);
                var prop = FindProp(so, fieldPath, comp);
                var before = Preview(prop);
                ApplyValue(prop, value, fieldPath);
                so.ApplyModifiedPropertiesWithoutUndo();
                return $"{nodePath}.{comp.GetType().Name}.{fieldPath}: {before} -> {Preview(prop)}";
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
                var comp = ResolveComponent(root, nodePath, componentType);
                var so = new SerializedObject(comp);
                var prop = FindProp(so, fieldPath, comp);
                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                    throw new EditAbort(
                        $"'{fieldPath}' 是 {prop.propertyType}，不是物件引用；請改用 SetField");

                var target = ResolveNode(root, targetNodePath);
                Component targetComp;
                if (!string.IsNullOrEmpty(targetComponentType))
                {
                    targetComp = target.GetComponent(ResolveComponentType(targetComponentType));
                }
                else
                {
                    // 沒指定就用欄位的宣告型別找 —— 少一個參數，也避免型別填錯
                    var fieldType = ResolveFieldType(comp.GetType(), fieldPath)
                                    ?? throw new EditAbort(
                                        $"找不到欄位 '{fieldPath}' 的宣告型別，請明確指定 targetComponentType");
                    targetComp = target.GetComponent(fieldType);
                }

                if (targetComp == null)
                    throw new EditAbort(
                        $"'{targetNodePath}' 上沒有需要的 component。這個節點掛的是：" +
                        string.Join(", ", target.GetComponents<Component>()
                            .Where(c => c != null).Select(c => c.GetType().Name)));

                prop.objectReferenceValue = targetComp;
                so.ApplyModifiedPropertiesWithoutUndo();
                return $"{nodePath}.{comp.GetType().Name}.{fieldPath} -> " +
                       $"{targetNodePath}.{targetComp.GetType().Name}";
            });
        }

        public static string DeleteNode(string assetPath, string nodePath)
        {
            return Edit(assetPath, root =>
            {
                if (string.IsNullOrEmpty(nodePath))
                    throw new EditAbort("不能刪 root");
                var node = ResolveNode(root, nodePath);
                var count = CountDescendants(node);
                UnityEngine.Object.DestroyImmediate(node.gameObject);
                return $"刪除 {nodePath}（含 {count} 個子節點）";
            });
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
                catch (EditAbort abort)
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

        // ---- 解析 ----

        private static Transform ResolveNode(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            var found = root.Find(path);
            if (found == null) throw new EditAbort(DescribeChildren(root, path));
            return found;
        }

        private static Component ResolveComponent(Transform root, string nodePath, string typeName)
        {
            var node = ResolveNode(root, nodePath);
            var comp = node.GetComponent(ResolveComponentType(typeName));
            if (comp == null)
                throw new EditAbort(
                    $"'{nodePath}' 上沒有 {typeName}。這個節點掛的是：" +
                    string.Join(", ", node.GetComponents<Component>()
                        .Where(c => c != null).Select(c => c.GetType().Name)));
            return comp;
        }

        private static Type ResolveComponentType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) throw new EditAbort("component 型別名不可為空");

            var matches = TypeCache.GetTypesDerivedFrom<Component>()
                .Where(t => t.Name == typeName || t.FullName == typeName)
                .ToList();

            if (matches.Count == 1) return matches[0];
            if (matches.Count == 0)
                throw new EditAbort($"找不到 component 型別 '{typeName}'");
            throw new EditAbort(
                $"'{typeName}' 有多個同名型別，請改用 FullName：" +
                string.Join(", ", matches.Select(t => t.FullName)));
        }

        // 逐段走 FieldInfo，支援 _rateVar._var 這種巢狀路徑
        private static Type ResolveFieldType(Type type, string fieldPath)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                       BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            var current = type;
            FieldInfo field = null;
            foreach (var seg in fieldPath.Split('.'))
            {
                field = null;
                for (var t = current; t != null && field == null; t = t.BaseType)
                    field = t.GetField(seg, flags);
                if (field == null) return null;
                current = field.FieldType;
            }

            return field?.FieldType;
        }

        private static SerializedProperty FindProp(
            SerializedObject so, string fieldPath, Component comp)
        {
            var prop = so.FindProperty(fieldPath);
            if (prop != null) return prop;

            // 欄位名打錯很常見，直接把這個 component 上有哪些 serialized 欄位列出來
            var names = new List<string>();
            var it = so.GetIterator();
            if (it.NextVisible(true))
                do
                {
                    if (it.name != "m_Script") names.Add(it.name);
                } while (it.NextVisible(false));

            throw new EditAbort(
                $"{comp.GetType().Name} 上找不到欄位 '{fieldPath}'。可用的頂層欄位：" +
                string.Join(", ", names));
        }

        private static void ApplyValue(SerializedProperty prop, object value, string fieldPath)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                    prop.floatValue = Convert.ToSingle(value);
                    break;
                case SerializedPropertyType.Integer:
                    prop.intValue = Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = Convert.ToBoolean(value);
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = value?.ToString() ?? "";
                    break;
                case SerializedPropertyType.Enum:
                    prop.enumValueIndex = ToEnumIndex(prop, value);
                    break;
                default:
                    throw new EditAbort(
                        $"'{fieldPath}' 的型別是 {prop.propertyType}，SetField 不支援" +
                        (prop.propertyType == SerializedPropertyType.ObjectReference
                            ? "；請改用 SetRef"
                            : ""));
            }
        }

        private static int ToEnumIndex(SerializedProperty prop, object value)
        {
            if (value is string s)
            {
                var index = Array.IndexOf(prop.enumNames, s);
                if (index < 0)
                    throw new EditAbort(
                        $"enum 沒有 '{s}'，可用的是：{string.Join(", ", prop.enumNames)}");
                return index;
            }

            return Convert.ToInt32(value);
        }

        private static string Preview(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float: return prop.floatValue.ToString("0.###");
                case SerializedPropertyType.Integer: return prop.intValue.ToString();
                case SerializedPropertyType.Boolean: return prop.boolValue.ToString();
                case SerializedPropertyType.String: return prop.stringValue;
                case SerializedPropertyType.Enum:
                    return prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumNames.Length
                        ? prop.enumNames[prop.enumValueIndex]
                        : prop.enumValueIndex.ToString();
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue != null
                        ? prop.objectReferenceValue.name
                        : "null";
                default: return prop.propertyType.ToString();
            }
        }

        // 路徑打錯時沿路徑走到最後一個通的節點，列出那層的子節點 —— 省一次來回
        // （跟 PrefabTextCacheWriter.DescribeChildren 同樣的回饋方式）
        private static string DescribeChildren(Transform root, string path)
        {
            var cursor = root;
            var walked = "";
            foreach (var seg in path.Split('/'))
            {
                var next = cursor.Find(seg);
                if (next == null) break;
                cursor = next;
                walked = string.IsNullOrEmpty(walked) ? seg : $"{walked}/{seg}";
            }

            var children = new List<string>();
            foreach (Transform child in cursor)
                children.Add($"{child.name} (+{CountDescendants(child)})");

            return $"找不到節點 '{path}'，走到 " +
                   $"'{(string.IsNullOrEmpty(walked) ? "(root)" : walked)}' 為止。" +
                   $"這層的子節點：{(children.Count == 0 ? "(無)" : string.Join(" | ", children))}";
        }

        private static int CountDescendants(Transform t)
        {
            var n = 0;
            foreach (Transform c in t) n += 1 + CountDescendants(c);
            return n;
        }

        private static string Describe(string path) =>
            string.IsNullOrEmpty(path) ? "(root)" : path;
    }
}
