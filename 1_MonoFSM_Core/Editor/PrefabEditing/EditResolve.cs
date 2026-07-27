using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// PrefabEdit / SceneEdit 共用的「路徑 → 節點 → component → 欄位」解析。
    ///
    /// 抽出來的理由：prefab 與 scene 只差在 root 怎麼來（prefab 有唯一 root、scene 有多個 root
    /// object），路徑語彙、型別解析、欄位套值、錯誤訊息全部一樣。錯誤訊息尤其不該有兩份 ——
    /// 它是 LLM 修正下一步的唯一線索。
    /// </summary>
    internal static class EditResolve
    {
        /// <summary>
        /// 解析失敗就拋這個。呼叫端（PrefabEdit.Edit / SceneEdit 各原語）攔下來後不存檔，
        /// 不留半殘資料。
        /// </summary>
        internal class EditAbort : Exception
        {
            public EditAbort(string message) : base(message) { }
        }

        internal static EditAbort Abort(string message) => new(message);

        // ---- 節點 ----

        /// <summary>單 root 的路徑解析（prefab）。path 留空 = root 自己。</summary>
        internal static Transform Node(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            var found = root.Find(path);
            if (found == null) throw Abort(DescribeChildren(root, path));
            return found;
        }

        /// <summary>
        /// 多 root 的路徑解析（scene）。第一段比對 root object，其餘走 Transform.Find。
        /// </summary>
        internal static Transform NodeInRoots(IList<GameObject> roots, string path)
        {
            if (string.IsNullOrEmpty(path))
                throw Abort("scene 沒有唯一 root，nodePath 不可留空（第一段要是 root object 名稱）");

            var slash = path.IndexOf('/');
            var head = slash < 0 ? path : path.Substring(0, slash);
            var rest = slash < 0 ? null : path.Substring(slash + 1);

            var rootGo = roots.FirstOrDefault(g => g != null && g.name == head);
            if (rootGo == null)
                throw Abort(
                    $"找不到 root object '{head}'。scene 的 root 有（{roots.Count} 個）：" +
                    Join(roots.Where(g => g != null).Take(40).Select(g => g.name)) +
                    (roots.Count > 40 ? " …" : ""));

            return string.IsNullOrEmpty(rest)
                ? rootGo.transform
                : Node(rootGo.transform, rest);
        }

        /// <summary>
        /// 路徑打錯時沿路徑走到最後一個通的節點，列出那層的子節點 —— 省一次來回。
        /// </summary>
        internal static string DescribeChildren(Transform root, string path)
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
                   $"這層的子節點：{(children.Count == 0 ? "(無)" : Join(children))}";
        }

        internal static int CountDescendants(Transform t)
        {
            var n = 0;
            foreach (Transform c in t) n += 1 + CountDescendants(c);
            return n;
        }

        // ---- component ----

        internal static Component Comp(Transform node, string nodePath, string typeName)
        {
            var comp = node.GetComponent(CompType(typeName));
            if (comp == null)
                throw Abort(
                    $"'{Describe(nodePath)}' 上沒有 {typeName}。這個節點掛的是：" +
                    Join(node.GetComponents<Component>()
                        .Where(c => c != null).Select(c => c.GetType().Name)));
            return comp;
        }

        internal static Type CompType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) throw Abort("component 型別名不可為空");

            var matches = TypeCache.GetTypesDerivedFrom<Component>()
                .Where(t => t.Name == typeName || t.FullName == typeName)
                .ToList();

            if (matches.Count == 1) return matches[0];
            if (matches.Count == 0)
            {
                // 打錯字很常見，給幾個相近的候選比單純說「找不到」有用得多
                var near = TypeCache.GetTypesDerivedFrom<Component>()
                    .Where(t => t.Name.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(t => t.Name).Distinct().Take(10).ToList();
                throw Abort($"找不到 component 型別 '{typeName}'" +
                            (near.Count > 0 ? $"。名稱含這段的有：{Join(near)}" : ""));
            }

            throw Abort($"'{typeName}' 有多個同名型別，請改用 FullName：" +
                        Join(matches.Select(t => t.FullName)));
        }

        /// <summary>逐段走 FieldInfo，支援 _rateVar._var 這種巢狀路徑。</summary>
        internal static Type FieldType(Type type, string fieldPath)
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

        // ---- 欄位 ----

        internal static SerializedProperty Prop(SerializedObject so, string fieldPath, Component comp)
        {
            var prop = so.FindProperty(fieldPath);
            if (prop != null) return prop;

            // 巢狀路徑（_timeMax._constValue）錯在最後一段時，列頂層欄位沒有用 ——
            // 要列的是「走得通的那一層底下有什麼」。VarFloatWrapper 這類 wrapper 的內部
            // 欄位名（_constValue / _value / …）沒有統一慣例，猜不到，必須列出來。
            var segs = fieldPath.Split('.');
            var walked = "";
            SerializedProperty cursor = null;
            foreach (var seg in segs)
            {
                var next = cursor == null
                    ? so.FindProperty(seg)
                    : cursor.FindPropertyRelative(seg);
                if (next == null) break;
                cursor = next;
                walked = walked.Length == 0 ? seg : $"{walked}.{seg}";
            }

            if (cursor != null && walked != fieldPath)
                throw Abort(
                    $"{comp.GetType().Name} 上找不到 '{fieldPath}'，走到 '{walked}' " +
                    $"（{cursor.type}）為止。這一層底下有：{Join(Children(cursor))}");

            var names = new List<string>();
            var it = so.GetIterator();
            if (it.NextVisible(true))
                do
                {
                    if (it.name != "m_Script") names.Add(it.name);
                } while (it.NextVisible(false));

            throw Abort($"{comp.GetType().Name} 上找不到欄位 '{fieldPath}'。可用的頂層欄位：" +
                        Join(names));
        }

        /// <summary>某個 SerializedProperty 的直接子欄位名（含型別），給錯誤訊息用。</summary>
        private static List<string> Children(SerializedProperty parent)
        {
            var names = new List<string>();
            var it = parent.Copy();
            var end = parent.GetEndProperty();
            if (!it.NextVisible(true)) return names;
            do
            {
                if (SerializedProperty.EqualContents(it, end)) break;
                names.Add($"{it.name}: {it.type}");
            } while (it.NextVisible(false));

            return names;
        }

        internal static void ApplyValue(SerializedProperty prop, object value, string fieldPath)
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
                case SerializedPropertyType.Vector3:
                    prop.vector3Value = ToVector3(value, fieldPath);
                    break;
                default:
                    throw Abort(
                        $"'{fieldPath}' 的型別是 {prop.propertyType}，SetField 不支援" +
                        (prop.propertyType == SerializedPropertyType.ObjectReference
                            ? "；請改用 SetRef / SetAssetRef"
                            : ""));
            }
        }

        private static Vector3 ToVector3(object value, string fieldPath)
        {
            if (value is Vector3 v) return v;
            // CLI 傳過來的都是字串，"1,2,3" 是最省字的寫法
            if (value is string s)
            {
                var parts = s.Split(',');
                if (parts.Length == 3 &&
                    float.TryParse(parts[0], out var x) &&
                    float.TryParse(parts[1], out var y) &&
                    float.TryParse(parts[2], out var z))
                    return new Vector3(x, y, z);
            }

            throw Abort($"'{fieldPath}' 是 Vector3，值請傳 \"x,y,z\" 或 Vector3");
        }

        private static int ToEnumIndex(SerializedProperty prop, object value)
        {
            if (value is string s)
            {
                var index = Array.IndexOf(prop.enumNames, s);
                if (index < 0)
                    throw Abort($"enum 沒有 '{s}'，可用的是：{Join(prop.enumNames)}");
                return index;
            }

            return Convert.ToInt32(value);
        }

        internal static string Preview(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float: return prop.floatValue.ToString("0.###");
                case SerializedPropertyType.Integer: return prop.intValue.ToString();
                case SerializedPropertyType.Boolean: return prop.boolValue.ToString();
                case SerializedPropertyType.String: return prop.stringValue;
                case SerializedPropertyType.Vector3: return prop.vector3Value.ToString("0.##");
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

        // ---- 引用 ----

        /// <summary>
        /// 找目標節點上該塞進欄位的 component。targetComponentType 省略時用欄位的宣告型別找 ——
        /// 少一個參數，也避免型別填錯。
        /// </summary>
        internal static Component RefTarget(
            Transform target, string targetNodePath, Component owner, string fieldPath,
            string targetComponentType)
        {
            Component targetComp;
            if (!string.IsNullOrEmpty(targetComponentType))
            {
                targetComp = target.GetComponent(CompType(targetComponentType));
            }
            else
            {
                var fieldType = FieldType(owner.GetType(), fieldPath)
                                ?? throw Abort(
                                    $"找不到欄位 '{fieldPath}' 的宣告型別，請明確指定 targetComponentType");
                targetComp = target.GetComponent(fieldType);
            }

            if (targetComp == null)
                throw Abort(
                    $"'{targetNodePath}' 上沒有需要的 component。這個節點掛的是：" +
                    Join(target.GetComponents<Component>()
                        .Where(c => c != null).Select(c => c.GetType().Name)));
            return targetComp;
        }

        /// <summary>
        /// 對子樹重跑 [Auto] / [AutoParent] / [AutoChildren] 綁定。
        ///
        /// **結構編輯之後一定要做這一步。** MonoFSM 大量欄位靠 Auto 系列 attribute 填
        /// （TransitionBehaviour._conditions 是 [AutoChildren]、Action 的 _parentObj 是
        /// [AutoParent]），平常是 Inspector 畫到時順手綁的。用 API 建節點不會經過 Inspector，
        /// 不補這一步就會存出一份「看起來對、欄位全是 null」的資料。
        /// </summary>
        internal static string RunAuto(Transform root)
        {
            AutoAttributeManager.AutoReferenceAllChildren(root.gameObject);
            var touched = 0;
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                EditorUtility.SetDirty(mb);
                touched++;
            }

            return $"Auto 綁定重跑：{root.name} 底下 {touched} 個 MonoBehaviour";
        }

        internal static string Describe(string path) =>
            string.IsNullOrEmpty(path) ? "(root)" : path;

        internal static string Join(IEnumerable<string> items)
        {
            var list = items as IList<string> ?? items.ToList();
            return list.Count == 0 ? "(無)" : string.Join(", ", list);
        }
    }
}
