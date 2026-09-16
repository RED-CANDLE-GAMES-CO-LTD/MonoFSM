using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 「這個型別叫什麼」「它有哪些可 serialize 的欄位」的查詢。
    ///
    /// 存在的理由純粹是省 context：要知道 VarFloatCountDownTimer 的欄位叫 `_timeMax` 還是
    /// `_maxTime`，替代方案是把整份 .cs 讀進來（幾百行）。這裡一行就回答，而且回的是
    /// **反射看到的真值**，不會被註解掉的舊欄位誤導。
    /// </summary>
    public static class EditProbe
    {
        /// <summary>名稱含 keyword 的 Component 型別。重名的才印 FullName。</summary>
        public static string Types(string keyword, int limit = 40)
        {
            var all = TypeCache.GetTypesDerivedFrom<Component>()
                .Where(t => !t.IsAbstract &&
                            t.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(t => t.Name.Length)
                .ToList();

            if (all.Count == 0) return $"# 沒有 Component 型別的名稱含 '{keyword}'";

            var dupes = all.GroupBy(t => t.Name).Where(g => g.Count() > 1)
                .Select(g => g.Key).ToHashSet();

            var sb = new StringBuilder($"{all.Count} 個（顯示 {Math.Min(limit, all.Count)}）\n");
            foreach (var t in all.Take(limit))
                sb.AppendLine("  " + (dupes.Contains(t.Name) ? t.FullName : t.Name));
            return sb.ToString();
        }

        /// <summary>
        /// 型別的可 serialize 欄位（含繼承來的），照 `名稱: 型別` 列出。
        /// 走反射而不是 SerializedObject —— 不需要先有一個實例。
        /// </summary>
        public static string Fields(string typeName, bool includeInherited = true)
        {
            Type type;
            try
            {
                type = EditResolve.CompType(typeName);
            }
            catch (EditResolve.EditAbort abort)
            {
                return $"# {abort.Message}";
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                       BindingFlags.NonPublic;

            var sb = new StringBuilder($"# {type.FullName}\n");
            var seen = new HashSet<string>();
            for (var t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                var own = t.GetFields(flags | BindingFlags.DeclaredOnly)
                    .Where(IsSerialized)
                    .Where(f => seen.Add(f.Name))
                    .ToList();
                if (own.Count == 0)
                {
                    if (!includeInherited) break;
                    continue;
                }

                if (t != type) sb.AppendLine($"  # from {t.Name}");
                foreach (var f in own)
                    sb.AppendLine($"  {f.Name}: {Pretty(f.FieldType)}");
                if (!includeInherited) break;
            }

            return sb.ToString();
        }

        /// <summary>型別上所有 serialize 欄位的名稱（含繼承來的，子類優先）。</summary>
        private static List<string> SerializedNames(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                       BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var names = new List<string>();
            var seen = new HashSet<string>();
            for (var t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
                foreach (var f in t.GetFields(flags))
                    if (IsSerialized(f) && seen.Add(f.Name))
                        names.Add(f.Name);
            return names;
        }

        private static bool IsSerialized(FieldInfo f)
        {
            if (f.IsStatic || f.IsLiteral) return false;
            if (f.GetCustomAttribute<NonSerializedAttribute>() != null) return false;
            if (f.IsPublic) return true;
            return f.GetCustomAttribute<SerializeField>() != null ||
                   f.GetCustomAttribute<SerializeReference>() != null;
        }

        private static string Pretty(Type t)
        {
            if (t.IsArray) return Pretty(t.GetElementType()) + "[]";
            if (t.IsGenericType)
            {
                var args = string.Join(",", t.GetGenericArguments().Select(Pretty));
                return $"{t.Name.Split('`')[0]}<{args}>";
            }

            if (t == typeof(float)) return "float";
            if (t == typeof(int)) return "int";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(string)) return "string";
            return t.Name;
        }

        /// <summary>
        /// 讀 runtime 值（Play Mode 驗證用）：某個節點上 component 的某幾個欄位/屬性現在是多少。
        /// 欄位名逗號分隔；留空 = 列出 serialize 欄位，並在尾巴附上可查的屬性名清單。
        ///
        /// 為什麼留空時不直接掃所有 public 屬性（2026-08-24 改）：那會對每個屬性呼叫 getter，
        /// 而 Unity component 上的屬性 getter 有些會在 native 層 abort 或把 stack 爆掉
        /// （Editor.log 留下 mono stack dump，managed try/catch 攔不到）—— 一次 peek 就閃退整個
        /// Editor。屬性要查得顯式寫進 members，範圍縮到一個，炸了也知道是誰。
        /// </summary>
        public static string Peek(string nodePath, string componentType, string members = null,
            int deep = 0)
        {
            Transform node;
            Component comp;
            try
            {
                node = EditResolve.NodeInRoots(EditResolve.RuntimeRoots(), nodePath);
                comp = EditResolve.Comp(node, nodePath, componentType);
            }
            catch (EditResolve.EditAbort abort)
            {
                return Abort(abort, nodePath);
            }

            return Dump(comp,
                $"{nodePath}.{comp.GetType().Name}  [{(Application.isPlaying ? "PlayMode" : "EditMode")}]",
                members, serializedByDefault: true, listPropertiesWhenEmpty: true, deep: deep);
        }

        /// <summary>
        /// 讀 prefab asset 上某個節點某個 component 的欄位值 —— 不進 Play Mode、不載整棵子樹。
        ///
        /// 為什麼跟 Peek 分開：`prefab read` 的最小單位是「一整顆子樹摺疊輸出」（實測平均
        /// 6.4KB），而最常問的其實是「那條 ref 到底接上了沒」。同一個問題走這裡是 ~100 字元。
        /// members 留空 = 列出這顆 component 的 serialize 欄位（不是 public 屬性 —— asset
        /// 上沒跑過任何 runtime 邏輯，屬性大半是空的或會炸）。
        /// </summary>
        public static string PeekAsset(
            string assetPath, string nodePath, string componentType, string members = null,
            int deep = 0)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null) return $"# 找不到 prefab: {assetPath}";

            Component comp;
            try
            {
                var node = string.IsNullOrEmpty(nodePath)
                    ? asset.transform
                    : EditResolve.TryNode(asset.transform, nodePath);
                if (node == null)
                    return EditResolve.DescribeChildren(asset.transform, nodePath);
                comp = EditResolve.Comp(node, nodePath, componentType);
            }
            catch (EditResolve.EditAbort abort)
            {
                return $"# {abort.Message}";
            }

            return Dump(comp, $"{EditResolve.Describe(nodePath)}.{comp.GetType().Name}  [asset]",
                members, serializedByDefault: true, deep: deep);
        }

        /// <summary>
        /// 只列一個節點上掛了哪些 component（名稱）。
        ///
        /// 存在理由：`peek` 少給 `--comp` 時原本只回一行「要 --comp」，那趟 round trip 完全白跑
        /// （usage log 裡有 19 次）。而下一步一定是「先看看這節點上有什麼」。
        ///
        /// **只取 GetType().Name，絕對不呼叫任何 property getter** —— 盲掃屬性會在 native 層
        /// abort 掉整個 Editor（managed try/catch 攔不到，見 Peek 的註解與
        /// reference_up_peek_property_getter_crash）。
        /// </summary>
        /// <param name="assetPath">prefab asset 路徑；留空 = 對當前 scene 的節點</param>
        public static string ComponentNames(string assetPath, string nodePath)
        {
            try
            {
                Transform node;
                string where;
                if (string.IsNullOrEmpty(assetPath))
                {
                    node = EditResolve.NodeInRoots(EditResolve.RuntimeRoots(), nodePath);
                    where = Application.isPlaying ? "PlayMode" : "EditMode";
                }
                else
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (asset == null) return $"# 找不到 prefab: {assetPath}";
                    node = string.IsNullOrEmpty(nodePath)
                        ? asset.transform
                        : EditResolve.TryNode(asset.transform, nodePath);
                    if (node == null)
                        return EditResolve.DescribeChildren(asset.transform, nodePath);
                    where = "asset";
                }

                var names = node.GetComponents<Component>()
                    .Where(c => c != null).Select(c => c.GetType().Name).ToList();
                return $"# {EditResolve.Describe(nodePath)} [{where}] 上的 component："
                       + EditResolve.Join(names) + "\n# 挑一個接 --comp（欄位值才會 dump 出來）";
            }
            catch (EditResolve.EditAbort abort)
            {
                return Abort(abort, string.IsNullOrEmpty(assetPath) ? nodePath : null);
            }
        }

        /// <summary>
        /// 在 Unity 合併後的 prefab contents 裡定位節點。variant 繼承來的節點/component 也看得到；
        /// 路徑走 EditResolve 的 escape + 同名 sibling [n] 規則，可直接餵回 --node。
        /// </summary>
        /// <param name="componentType">component 短名或 FullName；留空 = 不用 component 篩選</param>
        /// <param name="nameContains">節點名篩選（忽略大小寫）：含 * / ? 當 glob 整段比對，
        /// 否則當 substring；留空 = 不篩選</param>
        /// <param name="members">有指定 component 時，順便 dump 這些逗號分隔的欄位/屬性</param>
        /// <param name="limit">最多顯示幾個節點；total / cut 仍回報完整命中數</param>
        public static string LocateAsset(
            string assetPath, string componentType = null, string nameContains = null,
            string members = null, int limit = 20, int deep = 0)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
                return $"# 找不到 prefab: {assetPath}";
            if (!string.IsNullOrEmpty(members) && string.IsNullOrEmpty(componentType))
                return "# --members 需要同時指定 --comp";

            Type wanted = null;
            try
            {
                if (!string.IsNullOrEmpty(componentType))
                    wanted = EditResolve.CompType(componentType);
            }
            catch (EditResolve.EditAbort abort)
            {
                return $"# {abort.Message}";
            }

            GameObject root = null;
            try
            {
                // LoadPrefabContents 是關鍵：AssetDatabase 的離線/YAML 視角在 variant 邊界
                // 看不到完整繼承階層；這裡要的是 Unity 合併後真值。
                root = PrefabUtility.LoadPrefabContents(assetPath);
                var hits = new List<(Transform node, Component comp)>();
                var nameOk = EditResolve.NameMatcher(nameContains);
                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!nameOk(node.name)) continue;

                    var comp = wanted == null ? null : node.GetComponent(wanted);
                    if (wanted != null && comp == null) continue;
                    hits.Add((node, comp));
                }

                var shown = Math.Min(Math.Max(0, limit), hits.Count);
                var cut = hits.Count - shown;
                var sb = new StringBuilder();
                sb.AppendLine($"# prefab locate: {assetPath}");
                sb.AppendLine($"# filter: comp={componentType ?? "*"} name={nameContains ?? "*"}");
                sb.AppendLine($"# total={hits.Count} shown={shown} cut={cut}");
                sb.AppendLine("# paths are root-relative; (root) means an empty --node");

                foreach (var hit in hits.Take(shown))
                {
                    var path = EditResolve.PathOf(root.transform, hit.node);
                    sb.AppendLine(string.IsNullOrEmpty(path) ? "(root)" : path);

                    if (hit.comp != null)
                    {
                        sb.AppendLine($"  <{hit.comp.GetType().Name}>");
                        if (!string.IsNullOrEmpty(members))
                            sb.Append(Dump(hit.comp, "", members, serializedByDefault: true,
                                deep: deep));
                    }
                    else
                    {
                        var comps = hit.node.GetComponents<Component>()
                            .Where(c => c != null && !(c is Transform))
                            .Select(c => c.GetType().Name);
                        sb.AppendLine($"  <{string.Join(" ", comps)}>");
                    }
                }

                return sb.ToString();
            }
            catch (Exception e)
            {
                return $"# prefab locate 失敗：{e.GetType().Name}: {e.Message}";
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 一次載入 prefab 後執行多筆 peek。probes 每行 `node|component|members`；node 留空 = root，
        /// members 留空 = 所有 serialized 欄位。每筆各自攔錯，前一筆失敗不會吃掉後面的結果。
        /// </summary>
        public static string PeekAssetBatch(string assetPath, string probes, int deep = 0)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
                return $"# 找不到 prefab: {assetPath}";

            var lines = (probes ?? "").Replace("\r", "").Split('\n')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0 && !s.StartsWith("#"))
                .ToList();
            if (lines.Count == 0) return "# 沒有 probe；每行格式是 node|component|members";

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(assetPath);
                var sb = new StringBuilder();
                sb.AppendLine($"# prefab peek batch: {assetPath} ({lines.Count} probes)");
                for (var i = 0; i < lines.Count; i++)
                {
                    var raw = lines[i];
                    var a = raw.Split(new[] { '|' }, 3);
                    var nodePath = a.Length > 0 ? a[0].Trim() : "";
                    var componentType = a.Length > 1 ? a[1].Trim() : "";
                    var members = a.Length > 2 ? a[2].Trim() : null;

                    sb.AppendLine($"## probe {i + 1}: {raw}");
                    if (string.IsNullOrEmpty(componentType))
                    {
                        sb.AppendLine("# 失敗：缺 component；格式是 node|component|members");
                        continue;
                    }

                    try
                    {
                        var node = string.IsNullOrEmpty(nodePath)
                            ? root.transform
                            : EditResolve.TryNode(root.transform, nodePath);
                        if (node == null)
                        {
                            sb.AppendLine("# 失敗：" + EditResolve.DescribeChildren(
                                root.transform, nodePath));
                            continue;
                        }

                        var comp = EditResolve.Comp(node, nodePath, componentType);
                        sb.Append(Dump(comp,
                            $"{EditResolve.Describe(nodePath)}.{comp.GetType().Name}  [asset]",
                            members, serializedByDefault: true, deep: deep));
                    }
                    catch (EditResolve.EditAbort abort)
                    {
                        sb.AppendLine($"# 失敗：{abort.Message}");
                    }
                    catch (Exception e)
                    {
                        // 查詢工具不該因一顆 getter/序列化資料異常而吞掉剩下 probes。
                        sb.AppendLine($"# 失敗：{e.GetType().Name}: {e.Message}");
                    }
                }

                return sb.ToString();
            }
            catch (Exception e)
            {
                return $"# prefab peek batch 載入失敗：{e.GetType().Name}: {e.Message}";
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 印出 component 上指定成員的值。members 留空時：serializedByDefault = 走反射看
        /// serialize 欄位（asset 用），否則列 public 屬性（runtime 用）。
        ///
        /// members 的每一項可以是**點路徑**（`_ignoreFilter._ignoreSelfEntity`、
        /// `_entries[0]._family`）—— 巢狀 `[Serializable]` 純資料類別在這專案很常見
        /// （IgnoreColliderFilter / TargetPositionResolver），沒有點路徑就只印得到型別名。
        /// deep &gt; 0 則把巢狀 `[Serializable]` 類別攤開 deep 層（只走 serialize 欄位，
        /// **不呼叫任何 property getter**）。
        /// </summary>
        private static string Dump(
            Component comp, string header, string members, bool serializedByDefault,
            bool listPropertiesWhenEmpty = false, int deep = 0)
        {
            var type = comp.GetType();
            var sb = new StringBuilder(string.IsNullOrEmpty(header) ? "" : header + "\n");

            // override 標記：合併後的值看不出「是這顆自己改的還是繼承的」，而那正是改完
            // prefab 之後最想確認的一件事。判準與 HierarchyTextExporter 共用（PrefabOverrideMark），
            // isDefaultOverride 已排除，否則每顆 component 都是滿滿的星號。
            var overrides = PrefabOverrideMark.TopLevelOverrides(comp);
            var source = PrefabOverrideMark.SourceLabel(comp);
            if (source != null)
                sb.AppendLine(overrides.Count > 0
                    ? $"  # * = 這顆自己 override 的欄位；其餘繼承自 {source}"
                    : $"  # 沒有任何 override，整顆繼承自 {source}");

            List<string> names;
            if (!string.IsNullOrEmpty(members))
                names = members.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            else
                names = SerializedNames(type);

            foreach (var name in names)
            {
                if (!TryResolvePath(comp, name, out var value, out var reason))
                {
                    // 沒點名時（dump 全部欄位）不該為了讀不到的東西吵，只有顯式問了才回報
                    if (!string.IsNullOrEmpty(members)) sb.AppendLine($"  {name} = {reason}");
                    continue;
                }

                // `*` 只對「直接欄位」有意義 —— override 記錄的是 top level property path，
                // 巢狀段標星會讓人誤以為那一格自己被 override 了
                var star = name.IndexOf('.') < 0 && PrefabOverrideMark.Contains(overrides, name)
                    ? "*"
                    : "";
                sb.AppendLine($"  {name}{star} = {Show(value, deep)}");
            }

            if (listPropertiesWhenEmpty && string.IsNullOrEmpty(members))
                AppendPropertyNames(sb, type);

            return sb.ToString();
        }

        /// <summary>
        /// 一顆 component 的全部內容：serialize 欄位 + 全部可讀的 public 屬性值。
        ///
        /// 給 Inspector 右鍵選單用（`ComponentDumpMenu`）—— 使用者想一次撈完整狀態貼出來，
        /// 而 `up peek` 走 CLI 是刻意保守的（留空不掃屬性）。這裡掃，但每個屬性都走
        /// <see cref="ProbeMineField"/> 的麵包屑保護，炸過一次就永久跳過。
        ///
        /// reference 欄位多印一段 `= ValueInfo`（<see cref="IHierarchyValueInfo"/>，與 hierarchy
        /// 右側那欄同一來源：Var → CurrentValue、Condition → FinalResult、Getter → 取值）。
        /// 右鍵沒辦法像 CLI 用點路徑穿過 reference，而看引用的 Var / Condition 時九成只想知道
        /// 這一個值；把整顆展開試過，太吵（2026-09-15）。
        /// </summary>
        public static string DumpAll(Component comp, bool includeProperties)
        {
            var type = comp.GetType();
            var header = $"{PathOf(comp.transform)}.{type.Name}" +
                         $"  [{(Application.isPlaying ? "PlayMode" : "EditMode")}]";

            var crash = ProbeMineField.HarvestCrashReport();
            var sb = new StringBuilder();
            if (crash != null) sb.AppendLine(crash);

            s_refValueInfo = true;
            try
            {
                sb.Append(Dump(comp, header, null, serializedByDefault: true, deep: MenuDeep));

                if (!includeProperties) return sb.ToString();

                var props = PropertyNames(type);
                if (props.Count > 0)
                {
                    sb.AppendLine("  # --- 屬性 ---");
                    sb.Append(Dump(comp, "", string.Join(",", props), serializedByDefault: false,
                        deep: MenuDeep));
                }

                return sb.ToString();
            }
            finally
            {
                s_refValueInfo = false;
            }
        }

        /// <summary>右鍵選單版本攤開巢狀 [Serializable] 類別的層數（CLI 預設 0，這裡沒得下 --deep）。</summary>
        private const int MenuDeep = 2;

        /// <summary>
        /// 開著時 <see cref="ShowAt"/> 對 reference 多印 `= ValueInfo`。只有右鍵 dump 開，
        /// CLI 不開 —— ValueInfo 是 getter，CLI 那邊「留空不呼叫任何 getter」的約定不動。
        /// </summary>
        private static bool s_refValueInfo;

        /// <summary>reference 後面接的值摘要；沒有可講的就回空字串。</summary>
        private static string RefValueInfo(UnityEngine.Object o)
        {
            if (!s_refValueInfo || !(o is MonoFSM.EditorExtension.IHierarchyValueInfo info)) return "";
            try
            {
                var v = info.ValueInfo;
                if (string.IsNullOrEmpty(v)) return "";
                return " = " + (v.Length > 60 ? v.Substring(0, 60) + "…" : v);
            }
            catch (Exception e)
            {
                return $" = <throw {e.GetType().Name}>";
            }
        }

        /// <summary>hierarchy 路徑，dump 出來的內容要能看出是誰。</summary>
        private static string PathOf(Transform t)
        {
            var path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        /// <summary>
        /// 列出「可以再用 --members 點名去查」的屬性名 —— 只印名字，不呼叫任何 getter。
        /// 過濾掉 Component / Behaviour / Object 這層的 Unity 內建屬性（沒有 gameplay 資訊，
        /// 而且 legacy 的那幾個正是會讓 Editor 閃退的來源）。
        /// </summary>
        private static void AppendPropertyNames(StringBuilder sb, Type type)
        {
            var names = PropertyNames(type);
            if (names.Count == 0) return;
            sb.AppendLine($"  # 屬性（要看值請 --members 點名，一次一兩個）：{string.Join(", ", names)}");
        }

        /// <summary>
        /// 值得看的 public 屬性名。過濾掉 Component / Behaviour / Object 這層的 Unity 內建屬性
        /// （沒有 gameplay 資訊，而且 legacy 的那幾個正是會讓 Editor 閃退的來源）。
        /// </summary>
        private static List<string> PropertyNames(Type type) =>
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Where(p => !ProbeMineField.IsMine(p))
                .Where(p => p.DeclaringType != typeof(Component) &&
                            p.DeclaringType != typeof(Behaviour) &&
                            p.DeclaringType != typeof(MonoBehaviour) &&
                            p.DeclaringType != typeof(UnityEngine.Object))
                .Select(p => p.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

        /// <summary>
        /// Play Mode 下把一個 Var 的 runtime 值設成 value —— 自動測試用的「手動撥一下」。
        ///
        /// 為什麼需要：peek 只能讀。要驗「按了左鍵游標會不會動」「錢夠了買不買得成」，
        /// 得先能給錢、能把按鍵旗標撥起來。真的去驅動玩家角色互動成本高得多，而那段
        /// （EffectReceiver → ManualEvent）本來就是照抄現成模組，風險在後面的 FSM 這段。
        ///
        /// 走 AbstractMonoVariable.SetValue(TType, Object, string) —— 那是專案設值的正門，
        /// 會過 modifier、觸發 valueChangedHandler，跟遊戲裡真的被改是同一條路。
        /// </summary>
        public static string Poke(string nodePath, string componentType, string value)
        {
            if (!Application.isPlaying)
                return "# 未修改：poke 只在 Play Mode 有意義（EditMode 請用 prefab do / scene do）";

            Component comp;
            try
            {
                var node = EditResolve.NodeInRoots(EditResolve.RuntimeRoots(), nodePath);
                comp = EditResolve.Comp(node, nodePath, componentType);
            }
            catch (EditResolve.EditAbort abort)
            {
                return $"# 未修改：{abort.Message}";
            }

            var type = comp.GetType();
            var setValue = type.GetMethods(BindingFlags.Instance | BindingFlags.Public |
                                           BindingFlags.FlattenHierarchy)
                .FirstOrDefault(m => m.Name == "SetValue" && m.GetParameters().Length == 3);
            if (setValue == null)
                return $"# 未修改：{type.Name} 上沒有 SetValue(值, byWho, reason)，" +
                       "poke 只支援 AbstractMonoVariable 系列";

            var wanted = setValue.GetParameters()[0].ParameterType;
            object typed;
            try
            {
                typed = wanted.IsEnum
                    ? Enum.Parse(wanted, value, true)
                    : Convert.ChangeType(value, wanted);
            }
            catch (Exception e)
            {
                return $"# 未修改：'{value}' 轉不成 {wanted.Name}（{e.GetType().Name}）";
            }

            object before = null;
            var valueProp = type.GetProperty("Value",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (valueProp != null && valueProp.CanRead)
                try { before = valueProp.GetValue(comp); }
                catch { /* 讀不到就算了，不值得為了印個 before 中斷 */ }

            setValue.Invoke(comp, new[] { typed, null, "uprefab poke" });

            object after = null;
            if (valueProp != null && valueProp.CanRead)
                try { after = valueProp.GetValue(comp); }
                catch { /* 同上 */ }

            return $"{nodePath}.{type.Name}.Value: {Show(before)} -> {Show(after)}";
        }

        private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public |
                                                 BindingFlags.NonPublic |
                                                 BindingFlags.FlattenHierarchy;

        /// <summary>
        /// 解析一段成員路徑：`_ignoreSelfEntity`、`_ignoreFilter._ignoreSelfEntity`、
        /// `_selfEntities[0]._note`。每一段都是**使用者顯式點名的**，所以允許讀 property
        /// （仍過 <see cref="ProbeMineField"/>）；盲掃 property 的是 deep 展開那條路，那裡只走欄位。
        /// </summary>
        /// <param name="reason">失敗時的 `# …` 說明，直接印在欄位值的位置</param>
        private static bool TryResolvePath(object target, string path, out object value,
            out string reason)
        {
            value = null;
            reason = null;
            var segs = (path ?? "").Split('.');
            object cursor = target;
            for (var i = 0; i < segs.Length; i++)
            {
                if (IsNullish(cursor))
                {
                    reason = $"# '{string.Join(".", segs.Take(i))}' 是 null，後面走不下去";
                    return false;
                }

                var seg = segs[i].Trim();
                if (seg.Length == 0)
                {
                    reason = "# 路徑有空白段（是不是多打了一個點？）";
                    return false;
                }

                var index = -1;
                var open = seg.IndexOf('[');
                if (open > 0 && seg.EndsWith("]") &&
                    int.TryParse(seg.Substring(open + 1, seg.Length - open - 2), out var parsed))
                {
                    index = parsed;
                    seg = seg.Substring(0, open);
                }

                if (!TryReadMember(cursor, seg, out cursor, out reason)) return false;
                if (index >= 0 && !TryIndex(cursor, index, seg, out cursor, out reason)) return false;
            }

            value = cursor;
            return true;
        }

        /// <summary>一段成員名：先找欄位，再找可讀的 property（走 ProbeMineField 保護）。</summary>
        private static bool TryReadMember(object target, string name, out object value,
            out string reason)
        {
            value = null;
            reason = null;
            var owner = target.GetType();
            for (var t = owner; t != null && t != typeof(object); t = t.BaseType)
            {
                var f = t.GetField(name, MemberFlags | BindingFlags.DeclaredOnly);
                if (f != null)
                {
                    value = f.GetValue(target);
                    return true;
                }

                var prop = t.GetProperty(name, MemberFlags | BindingFlags.DeclaredOnly);
                if (prop == null || !prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
                // 有些 getter 呼叫下去會 native abort，managed catch 攔不到 —— 見 ProbeMineField
                if (ProbeMineField.IsMine(prop))
                {
                    reason = "# 跳過（已知會讓 Editor 閃退，或 [Obsolete]）";
                    return false;
                }

                value = ProbeMineField.ReadGuarded(prop, target);
                return true;
            }

            reason = $"# 找不到這個欄位/屬性（{owner.Name} 上沒有 '{name}'）";
            return false;
        }

        /// <summary>路徑裡的 `[n]`。IList 直接取，其他 IEnumerable 走一遍。</summary>
        private static bool TryIndex(object collection, int index, string seg, out object item,
            out string reason)
        {
            item = null;
            reason = null;
            if (collection is IList list)
            {
                if (index < 0 || index >= list.Count)
                {
                    reason = $"# {seg}[{index}] 超出範圍（count={list.Count}）";
                    return false;
                }

                item = list[index];
                return true;
            }

            if (collection is IEnumerable en && !(collection is string))
            {
                var i = 0;
                foreach (var o in en)
                {
                    if (i++ != index) continue;
                    item = o;
                    return true;
                }

                reason = $"# {seg}[{index}] 超出範圍（count={i}）";
                return false;
            }

            reason = $"# {seg} 不是陣列/List，不能用 [{index}]";
            return false;
        }

        /// <summary>Unity 的「假 null」不是 C# null（未指派 / 已 destroy），要用 Unity 的 == 判。</summary>
        private static bool IsNullish(object v) =>
            v == null || (v is UnityEngine.Object uo && uo == null);

        /// <summary>
        /// 巢狀 `[Serializable]` 純資料類別（IgnoreColliderFilter / TargetPositionResolver 這種）。
        /// UnityEngine.Object 不算 —— 那是引用，印名字就夠了，展開下去會爬到整個場景。
        /// </summary>
        private static bool IsNestedSerializable(Type t) =>
            t.IsClass && t != typeof(string) &&
            !typeof(UnityEngine.Object).IsAssignableFrom(t) &&
            t.GetCustomAttribute<SerializableAttribute>(false) != null;

        /// <summary>型別上所有 serialize 欄位（含繼承），deep 展開只看這些、不碰 property。</summary>
        private static List<FieldInfo> SerializedFieldsOf(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                       BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var list = new List<FieldInfo>();
            var seen = new HashSet<string>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                foreach (var f in t.GetFields(flags))
                    if (IsSerialized(f) && seen.Add(f.Name))
                        list.Add(f);
            return list;
        }

        /// <summary>EditAbort 的統一出口：路徑第一段其實是 prefab 名時順便給出正確指令。</summary>
        private static string Abort(EditResolve.EditAbort abort, string nodePath)
        {
            var hint = nodePath == null ? null : PrefabPathHint(nodePath);
            return hint == null ? $"# {abort.Message}" : $"# {abort.Message}\n{hint}";
        }

        /// <summary>
        /// 路徑第一段是某顆 prefab 的檔名時的指引。
        ///
        /// 為什麼需要：`up peek "PPlayer/…"` 原本只回「找不到 root object 'PPlayer'。scene 的 root
        /// 有（26 個）…」—— 下一隻 agent 會直接判定節點不存在，而其實那是 prefab 路徑，
        /// 用 `up prefab peek` 根本不用開 stage 就讀得到。
        /// </summary>
        private static string PrefabPathHint(string nodePath)
        {
            if (string.IsNullOrEmpty(nodePath)) return null;
            var slash = EditResolve.IndexOfUnescapedSlash(nodePath);
            var head = EditResolve.Unescape(slash < 0 ? nodePath : nodePath.Substring(0, slash));
            var rest = slash < 0 ? "" : nodePath.Substring(slash + 1);

            // root 也吃 `名稱[n]`，比對 prefab 檔名前先把後綴拿掉
            var bracket = head.LastIndexOf('[');
            if (bracket > 0 && head.EndsWith("]") &&
                int.TryParse(head.Substring(bracket + 1, head.Length - bracket - 2), out _))
                head = head.Substring(0, bracket);
            if (head.Length == 0) return null;

            List<string> paths;
            try
            {
                paths = AssetDatabase.FindAssets($"\"{head}\" t:Prefab")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(a => !string.IsNullOrEmpty(a) &&
                                System.IO.Path.GetFileNameWithoutExtension(a) == head)
                    .Distinct().Take(3).ToList();
            }
            catch (Exception)
            {
                return null; // 名稱含 search filter 的特殊字元；沒有提示總比炸掉好
            }

            if (paths.Count == 0) return null;

            var sb = new StringBuilder(
                $"# 但 '{head}' 是 prefab 的名字 —— 這條路徑看起來是 prefab 內部節點，不是 scene 上的物件：\n");
            foreach (var a in paths) sb.AppendLine($"#   {a}");
            sb.AppendLine("# prefab 不用開 stage 也讀得到，改用（node 不含 prefab 名那一段）：");
            sb.Append($"#   up prefab peek \"{paths[0]}\" --node \"{rest}\" --comp <Component> --members <欄位>");
            return sb.ToString();
        }

        /// <summary>陣列 / List 最多印幾個元素，其餘只回報數量 —— deep 展開時很容易爆量。</summary>
        private const int ListPreview = 6;

        /// <param name="classDepth">還能把巢狀 [Serializable] 類別攤開幾層；0 = 維持舊行為</param>
        private static string Show(object v, int classDepth = 0)
        {
            try
            {
                return ShowAt(v, 0, classDepth);
            }
            catch (Exception e)
            {
                // dump 是除錯工具：一個欄位印不出來不該讓整份輸出消失
                return $"<throw {e.GetType().Name}>";
            }
        }

        private static string ShowAt(object v, int depth, int classDepth)
        {
            switch (v)
            {
                case null: return "null";
                case string s: return s.Length > 60 ? s.Substring(0, 60) + "…" : s;
                case float f: return f.ToString("0.###");
                // Unity 的「假 null」不是 C# null，接不到上面的 case null：未指派的 reference
                // （UnassignedReference）跟已 destroy 的物件都會在讀 .name 時丟 exception。
                // 要用 Unity 自己的 == 才判得出來。
                case UnityEngine.Object o:
                    return o == null
                        ? $"null <{o.GetType().Name}>"
                        : $"{o.name} <{o.GetType().Name}>{RefValueInfo(o)}";
                case IEnumerable e when !(v is string):
                {
                    // 集合本身不算一層 class 巢狀，所以 classDepth 原樣傳下去：
                    // List<SomeSerializable> 的元素要跟直接欄位一樣看得到內容
                    var all = e.Cast<object>().ToList();
                    var items = all.Take(ListPreview).Select(x => ShowAt(x, depth + 1, classDepth));
                    var cut = all.Count - ListPreview;
                    return $"[{string.Join(", ", items)}{(cut > 0 ? $", … 還有 {cut} 個未列出" : "")}]";
                }
                // 沒 override ToString 的 struct（CharacterMovement.MovingPlatform 這種
                // 純資料容器）預設只印出型別名，等於什麼都沒查到。攤開欄位才有意義；
                // 巢狀限一層，Vector3 / Quaternion 有自己的 ToString 不受影響。
                case ValueType vt when depth < 2 && !(v is Enum) && !vt.GetType().IsPrimitive &&
                                       ToStringIsDefault(vt.GetType()):
                {
                    var fields = vt.GetType().GetFields(BindingFlags.Instance |
                                                        BindingFlags.Public | BindingFlags.NonPublic);
                    return "{" + string.Join(", ",
                        fields.Select(f => $"{f.Name}={ShowAt(f.GetValue(vt), depth + 1, classDepth)}")) + "}";
                }
                // 巢狀 [Serializable] 類別：預設只會印出型別名（等於什麼都沒查到），
                // --deep 才攤開。**只走 serialize 欄位，不呼叫任何 property getter** ——
                // 盲掃 getter 會在 native 層 abort 掉整個 Editor（見 Peek 的註解）。
                case object nested when classDepth > 0 && IsNestedSerializable(nested.GetType()):
                {
                    var fields = SerializedFieldsOf(nested.GetType());
                    if (fields.Count == 0) return nested.ToString();
                    return "{" + string.Join(", ", fields.Select(
                        f => $"{f.Name}={ShowAt(f.GetValue(nested), depth + 1, classDepth - 1)}")) + "}";
                }
                default: return v.ToString();
            }
        }

        /// <summary>型別自己沒實作 ToString()（拿到的會是 System.ValueType 的預設型別名）。</summary>
        private static bool ToStringIsDefault(Type t)
        {
            var m = t.GetMethod("ToString", Type.EmptyTypes);
            return m == null || m.DeclaringType == typeof(ValueType) || m.DeclaringType == typeof(object);
        }
    }
}
