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

        /// <summary>
        /// 單 root 的路徑解析（prefab）。path 留空 = root 自己。
        /// 任何一段都可以加 `[n]` 後綴指定「同名的第 n 個」（0-based），例如
        /// `[Switch Simulate] Switch (FirstMatch)[1]/[Case] SwitchCase[2]` ——
        /// MonoFSM 的 SwitchCase / Action 節點常常整排同名，`Transform.Find` 只拿得到第一個。
        /// </summary>
        internal static Transform Node(Transform root, string path)
        {
            var found = TryNode(root, path);
            if (found == null) throw Abort(DescribeChildren(root, path));
            return found;
        }

        /// <summary>找不到回 null 的版本（讀取端用，讀不到不是錯誤，只是要換個訊息）。</summary>
        internal static Transform TryNode(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            // 名稱本身含 `/` 的節點（`=> Localized: GameplayUI/grab` 這種自動命名很常見）
            // 要寫成 `\/`，這時不能走 Transform.Find 的快路徑，它只認真正的階層分隔。
            if (HasEscapedSlash(path)) return FindByIndexedPath(root, path);
            return root.Find(path) ?? FindByIndexedPath(root, path);
        }

        internal static bool HasEscapedSlash(string path) =>
            path != null && path.Contains("\\/");

        /// <summary>第一個「真的是階層分隔」的 `/` 位置；`\/` 不算。找不到回 -1。</summary>
        internal static int IndexOfUnescapedSlash(string path)
        {
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] == '\\' && i + 1 < path.Length && path[i + 1] == '/')
                {
                    i++;
                    continue;
                }

                if (path[i] == '/') return i;
            }

            return -1;
        }

        internal static string Unescape(string segment) => segment.Replace("\\/", "/");

        /// <summary>把節點名裡的 `/` 轉成 `\/`，讓列出來的候選可以直接抄進路徑。</summary>
        internal static string EscapeName(string name) => name.Replace("/", "\\/");

        /// <summary>
        /// 依 `/` 切段，但 `\/` 是「名稱裡的斜線」不切（切完會還原成 `/`）。
        /// MonoFSM 的自動命名會塞進 `Table/key` 這種字串，不逃逸就永遠指不到那個節點。
        /// </summary>
        internal static string[] SplitPath(string path)
        {
            var segments = new List<string>();
            var current = new System.Text.StringBuilder();
            for (var i = 0; i < path.Length; i++)
            {
                var c = path[i];
                if (c == '\\' && i + 1 < path.Length && path[i + 1] == '/')
                {
                    current.Append('/');
                    i++;
                    continue;
                }

                if (c == '/')
                {
                    segments.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            segments.Add(current.ToString());
            return segments.ToArray();
        }

        /// <summary>
        /// 逐段走路徑，每段支援 `名稱[n]` 取同名的第 n 個。沒有 `[n]` 後綴的段就是一般 Find。
        /// 只在 `Transform.Find` 整條路徑失敗後才會走到這裡，所以不影響原本的解析行為。
        /// </summary>
        private static Transform FindByIndexedPath(Transform root, string path)
        {
            var cursor = root;
            foreach (var seg in SplitPath(path))
            {
                cursor = FindSegment(cursor, seg);
                if (cursor == null) return null;
            }

            return cursor;
        }

        internal static Transform FindSegment(Transform cursor, string seg)
        {
            if (!TrySplitIndexSuffix(seg, out var name, out var index))
            {
                // Transform.Find 會把名稱裡的 `/` 當成階層分隔（`=> Localized: GameplayUI/grab`
                // 這種自動命名就永遠找不到），這時只能自己掃子節點比對全名
                if (seg.Contains('/'))
                {
                    foreach (Transform child in cursor)
                        if (child.name == seg)
                            return child;
                    return null;
                }

                return cursor.Find(seg);
            }

            var n = 0;
            foreach (Transform child in cursor)
            {
                if (child.name != name) continue;
                if (n == index) return child;
                n++;
            }

            return null;
        }

        /// <summary>`[Case] SwitchCase[2]` → name=`[Case] SwitchCase`, index=2。</summary>
        private static bool TrySplitIndexSuffix(string seg, out string name, out int index)
        {
            name = seg;
            index = 0;
            if (seg.Length < 4 || seg[^1] != ']') return false;
            var open = seg.LastIndexOf('[');
            if (open <= 0) return false;
            var digits = seg.Substring(open + 1, seg.Length - open - 2);
            if (digits.Length == 0 || !digits.All(char.IsDigit)) return false;
            if (!int.TryParse(digits, out index)) return false;
            name = seg.Substring(0, open);
            return name.Length > 0;
        }

        /// <summary>
        /// 多 root 的路徑解析（scene）。第一段比對 root object，其餘走 Transform.Find。
        /// </summary>
        internal static Transform NodeInRoots(IList<GameObject> roots, string path)
        {
            if (string.IsNullOrEmpty(path))
                throw Abort("scene 沒有唯一 root，nodePath 不可留空（第一段要是 root object 名稱）");

            var slash = IndexOfUnescapedSlash(path);
            var head = slash < 0 ? Unescape(path) : Unescape(path.Substring(0, slash));
            var rest = slash < 0 ? null : path.Substring(slash + 1);

            // root 也可能整排同名（一個 scene 裡十幾個 AppCallbackListener），所以第一段
            // 同樣吃 `名稱[n]`
            // 照原樣比對優先，比不到才試 `[n]` —— 跟 FindByIndexedPath 同一個慣例，
            // 名字本身結尾就是 `[數字]` 的 root 不受影響
            var rootGo = roots.FirstOrDefault(g => g != null && g.name == head);
            if (rootGo == null && TrySplitIndexSuffix(head, out var headName, out var headIndex))
            {
                rootGo = roots.Where(g => g != null && g.name == headName)
                    .Skip(headIndex).FirstOrDefault();
                if (rootGo != null) head = headName;
            }

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
        /// Play Mode 下要拿的 root 集合。
        ///
        /// 只用 activeScene.GetRootGameObjects() 會漏掉 additive scene 與
        /// DontDestroyOnLoad —— Fusion 的 Runner、生成出來的玩家角色（Player1 [Local] …）
        /// 都掛在 DontDestroyOnLoad，peek 這類 runtime 查詢十之八九查的就是它們。
        /// EditMode 沒有這個問題，維持 active scene 以免掃到 preview / 隱藏物件。
        /// </summary>
        internal static List<GameObject> RuntimeRoots()
        {
            if (!Application.isPlaying)
                return UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                    .GetRootGameObjects().ToList();

            return UnityEngine.Object
                .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t != null && t.parent == null)
                .Select(t => t.gameObject)
                .ToList();
        }

        /// <summary>
        /// 路徑打錯時沿路徑走到最後一個通的節點，列出那層的子節點 —— 省一次來回。
        /// </summary>
        internal static string DescribeChildren(Transform root, string path)
        {
            var cursor = WalkAsFarAsPossible(root, path, out var walked);
            var children = ChildLabels(cursor);
            return $"找不到節點 '{path}'，走到 " +
                   $"'{(string.IsNullOrEmpty(walked) ? "(root)" : walked)}' 為止。" +
                   $"這層的子節點：{(children.Count == 0 ? "(無)" : Join(children))}";
        }

        /// <summary>沿路徑往下走到最後一個走得通的節點，`walked` 回報走到哪。</summary>
        internal static Transform WalkAsFarAsPossible(Transform root, string path, out string walked)
        {
            var cursor = root;
            walked = "";
            foreach (var seg in SplitPath(path))
            {
                var next = FindSegment(cursor, seg);
                if (next == null) break;
                cursor = next;
                walked = string.IsNullOrEmpty(walked) ? seg : $"{walked}/{seg}";
            }

            return cursor;
        }

        /// <summary>
        /// 列出一層的子節點，格式 `名稱 (+N)`。**同名的會標上 `[n]`** ——
        /// 那是唯一指得到後面幾個的寫法，不標的話讀的人只會一直打到第一個。
        /// </summary>
        internal static List<string> ChildLabels(Transform cursor)
        {
            var seenNames = new HashSet<string>();
            var dupNames = new HashSet<string>();
            foreach (Transform child in cursor)
                if (!seenNames.Add(child.name))
                    dupNames.Add(child.name);

            var labels = new List<string>();
            var counter = new Dictionary<string, int>();
            foreach (Transform child in cursor)
            {
                var label = EscapeName(child.name);
                if (dupNames.Contains(child.name))
                {
                    counter.TryGetValue(label, out var n);
                    counter[label] = n + 1;
                    label = $"{label}[{n}]";
                }

                labels.Add($"{label} (+{CountDescendants(child)})");
            }

            return labels;
        }

        /// <summary>
        /// Transform → 可以原樣餵回 <see cref="TryNode"/> 的路徑（不含 root 自己）。
        ///
        /// **同名 sibling 一定會標上 `[n]`**（0-based，依 sibling 順序）—— 這是
        /// <see cref="ChildLabels"/> 用的同一套規則。不標的話路徑會指回同名的第一個，
        /// 而 MonoFSM 的 SwitchCase / Action 節點常常整排同名，錯得無聲無息。
        ///
        /// node 不在 root 底下時回 null（呼叫端要能分辨「解不開」而不是拿到一條錯路徑）。
        /// node == root 時回空字串（`--node` 留空就是 root）。
        /// </summary>
        internal static string PathOf(Transform root, Transform node)
        {
            if (root == null || node == null) return null;
            if (node == root) return "";

            var segs = new List<string>();
            for (var cur = node; cur != null; cur = cur.parent)
            {
                if (cur == root) break;
                if (cur.parent == null) return null; // 走到頂都沒遇到 root
                segs.Add(SegmentOf(cur));
            }

            segs.Reverse();
            return string.Join("/", segs);
        }

        /// <summary>一段路徑：名稱，同名 sibling 存在時補 `[n]`。</summary>
        private static string SegmentOf(Transform node)
        {
            var parent = node.parent;
            if (parent == null) return EscapeName(node.name);

            var index = 0;
            var dup = false;
            foreach (Transform sib in parent)
            {
                if (sib == node) continue;
                if (sib.name != node.name) continue;
                dup = true;
                if (sib.GetSiblingIndex() < node.GetSiblingIndex()) index++;
            }

            return dup ? $"{EscapeName(node.name)}[{index}]" : EscapeName(node.name);
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

        internal static Type CompType(string typeName) =>
            ResolveType<Component>(typeName, "component 型別");

        /// <summary>
        /// 解析 ScriptableObject 型別（給 AssetEdit.CreateAsset 用）。走同一套
        /// 「短名/FullName、打錯字列相近候選」邏輯，只是搜尋池換成 ScriptableObject 衍生型別 ——
        /// 這樣解析出來的型別天生就保證繼承 ScriptableObject，不用另外檢查。
        /// </summary>
        internal static Type ScriptableObjectType(string typeName) =>
            ResolveType<ScriptableObject>(typeName, "ScriptableObject 型別");

        /// <summary>
        /// 解析 [SerializeReference] 欄位能塞的具體型別（給 AssetEdit / PrefabEdit 的
        /// managed reference 用）。池子是該欄位宣告型別的非抽象衍生型別 —— SerializeReference
        /// 的欄位型別通常是抽象基底（如 AbstractDataFunction），Unity 端只存得下具體實作。
        /// </summary>
        internal static Type ManagedRefType(Type baseType, string typeName)
        {
            var pool = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => !t.IsAbstract && !t.IsInterface);
            return ResolveTypeFromPool(pool, typeName, $"{baseType.Name} 的實作型別");
        }

        /// <summary>
        /// [SerializeReference] 欄位的宣告型別。managedReferenceFieldTypename 的格式是
        /// "組件名 型別FullName"，Unity 沒有公開的 Type 版本，只能自己拆。
        /// </summary>
        internal static Type ManagedRefFieldType(SerializedProperty prop)
        {
            var raw = prop.managedReferenceFieldTypename;
            if (string.IsNullOrEmpty(raw)) return null;
            var parts = raw.Split(' ');
            return parts.Length != 2 ? null : Type.GetType($"{parts[1]}, {parts[0]}");
        }

        /// <summary>
        /// 型別名（短名或 FullName）→ Type 的共用解析，`CompType` / `ScriptableObjectType`
        /// 都是它的特化。錯誤訊息（找不到時列相近候選、同名多型別時列 FullName）只有這一份。
        /// </summary>
        private static Type ResolveType<T>(string typeName, string kind) where T : class =>
            ResolveTypeFromPool(TypeCache.GetTypesDerivedFrom<T>(), typeName, kind);

        private static Type ResolveTypeFromPool(
            IEnumerable<Type> types, string typeName, string kind)
        {
            if (string.IsNullOrEmpty(typeName)) throw Abort($"{kind}名不可為空");

            var pool = types.ToList();
            var matches = pool.Where(t => t.Name == typeName || t.FullName == typeName).ToList();

            if (matches.Count == 1) return matches[0];
            if (matches.Count == 0)
            {
                // 打錯字很常見，給幾個相近的候選比單純說「找不到」有用得多
                var near = pool
                    .Where(t => t.Name.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(t => t.Name).Distinct().Take(10).ToList();
                throw Abort($"找不到 {kind} '{typeName}'" +
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

        /// <summary>
        /// fieldPath → SerializedProperty。`obj` 只用來讓錯誤訊息報型別名，
        /// 傳 Component（PrefabEdit / SceneEdit）或任何 UnityEngine.Object（AssetEdit 的
        /// ScriptableObject asset）都可以 —— Component 本來就是 Object。
        /// </summary>
        internal static SerializedProperty Prop(SerializedObject so, string fieldPath, UnityEngine.Object obj)
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
                    $"{obj.GetType().Name} 上找不到 '{fieldPath}'，走到 '{walked}' " +
                    $"（{cursor.type}）為止。這一層底下有：{Join(Children(cursor))}");

            var names = new List<string>();
            var it = so.GetIterator();
            if (it.NextVisible(true))
                do
                {
                    if (it.name != "m_Script") names.Add(it.name);
                } while (it.NextVisible(false));

            throw Abort($"{obj.GetType().Name} 上找不到欄位 '{fieldPath}'。可用的頂層欄位：" +
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
                case SerializedPropertyType.Vector2:
                    prop.vector2Value = ToVector2(value, fieldPath);
                    break;
                case SerializedPropertyType.Color:
                    prop.colorValue = ToColor(value, fieldPath);
                    break;
                case SerializedPropertyType.LayerMask:
                    prop.intValue = ToLayerMask(value, fieldPath);
                    break;
                default:
                    throw Abort(
                        $"'{fieldPath}' 的型別是 {prop.propertyType}，SetField 不支援" +
                        (prop.propertyType == SerializedPropertyType.ObjectReference
                            ? "；請改用 SetRef / SetAssetRef"
                            : ""));
            }
        }

        /// <summary>LayerMask：吃整數位元遮罩（-1 = Everything），或逗號分隔的 layer 名稱。</summary>
        private static int ToLayerMask(object value, string fieldPath)
        {
            if (value is int i) return i;
            var s = value?.ToString()?.Trim() ?? "";
            if (int.TryParse(s, out var bits)) return bits;
            if (string.Equals(s, "Everything", StringComparison.OrdinalIgnoreCase)) return -1;
            if (string.Equals(s, "Nothing", StringComparison.OrdinalIgnoreCase)) return 0;

            var mask = 0;
            foreach (var raw in s.Split(','))
            {
                var name = raw.Trim();
                if (name.Length == 0) continue;
                var layer = LayerMask.NameToLayer(name);
                if (layer < 0)
                    throw Abort($"'{fieldPath}' 找不到 layer '{name}'。" +
                                "LayerMask 可以傳整數位元遮罩、Everything / Nothing、或逗號分隔的 layer 名稱");
                mask |= 1 << layer;
            }

            return mask;
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

        private static Vector2 ToVector2(object value, string fieldPath)
        {
            if (value is Vector2 v) return v;
            if (value is string s)
            {
                var parts = s.Split(',');
                if (parts.Length == 2 &&
                    float.TryParse(parts[0], out var x) &&
                    float.TryParse(parts[1], out var y))
                    return new Vector2(x, y);
            }

            throw Abort($"'{fieldPath}' 是 Vector2，值請傳 \"x,y\" 或 Vector2");
        }

        //"r,g,b" / "r,g,b,a"（0~1）或 "#RRGGBB" / "#RRGGBBAA"
        private static Color ToColor(object value, string fieldPath)
        {
            if (value is Color c) return c;
            if (value is string s)
            {
                if (s.StartsWith("#") && ColorUtility.TryParseHtmlString(s, out var parsed))
                    return parsed;

                var parts = s.Split(',');
                if (parts.Length is 3 or 4 &&
                    float.TryParse(parts[0], out var r) &&
                    float.TryParse(parts[1], out var g) &&
                    float.TryParse(parts[2], out var b))
                {
                    var a = 1f;
                    if (parts.Length == 4 && !float.TryParse(parts[3], out a))
                        a = 1f;
                    return new Color(r, g, b, a);
                }
            }

            throw Abort($"'{fieldPath}' 是 Color，值請傳 \"r,g,b[,a]\"（0~1）或 \"#RRGGBB[AA]\"");
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
        /// 欄位宣告型別是 GameObject（UI 常見）時回傳節點的 GameObject 本身。
        /// </summary>
        internal static UnityEngine.Object RefTarget(
            Transform target, string targetNodePath, Component owner, string fieldPath,
            string targetComponentType)
        {
            UnityEngine.Object targetComp;
            if (!string.IsNullOrEmpty(targetComponentType))
            {
                //GameObject 不是 Component，不能走 CompType 的搜尋池
                targetComp = targetComponentType == nameof(GameObject)
                    ? target.gameObject
                    : target.GetComponent(CompType(targetComponentType));
            }
            else
            {
                var fieldType = FieldType(owner.GetType(), fieldPath)
                                ?? throw Abort(
                                    $"找不到欄位 '{fieldPath}' 的宣告型別，請明確指定 targetComponentType");
                targetComp = fieldType == typeof(GameObject)
                    ? target.gameObject
                    : target.GetComponent(fieldType);
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

        /// <summary>
        /// 陣列 / List 欄位尾端加一個元素，回傳新元素的 index（接著用 set / aref 補
        /// `<fieldPath>.Array.data[index]`）。
        ///
        /// 為什麼不能用 `set|…|_stateTags.Array.size|1`：ArraySize 這個 propertyType
        /// 走不進 ApplyValue，只能透過 arraySize 改。
        ///
        /// 注意：SerializedProperty.isArray 對 string 也回 true（舊版序列化 API 把 string
        /// 當 char[] 存），不排除的話會把元素插進字串的位元組裡，存出壞掉的 UTF-8。
        /// </summary>
        internal static int AddArrayElement(SerializedProperty prop, string fieldPath)
        {
            if (!prop.isArray || prop.propertyType == SerializedPropertyType.String)
                throw Abort($"'{fieldPath}' 是 {prop.propertyType}，不是陣列/List，不能加元素");

            var index = prop.arraySize;
            prop.arraySize++;
            return index;
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
