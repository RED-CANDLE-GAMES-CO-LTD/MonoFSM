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
        public static string Peek(string nodePath, string componentType, string members = null)
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
                return $"# {abort.Message}";
            }

            return Dump(comp,
                $"{nodePath}.{comp.GetType().Name}  [{(Application.isPlaying ? "PlayMode" : "EditMode")}]",
                members, serializedByDefault: true, listPropertiesWhenEmpty: true);
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
            string assetPath, string nodePath, string componentType, string members = null)
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
                members, serializedByDefault: true);
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
                return $"# {abort.Message}";
            }
        }

        /// <summary>
        /// 在 Unity 合併後的 prefab contents 裡定位節點。variant 繼承來的節點/component 也看得到；
        /// 路徑走 EditResolve 的 escape + 同名 sibling [n] 規則，可直接餵回 --node。
        /// </summary>
        /// <param name="componentType">component 短名或 FullName；留空 = 不用 component 篩選</param>
        /// <param name="nameContains">節點名包含（忽略大小寫）；留空 = 不用名稱篩選</param>
        /// <param name="members">有指定 component 時，順便 dump 這些逗號分隔的欄位/屬性</param>
        /// <param name="limit">最多顯示幾個節點；total / cut 仍回報完整命中數</param>
        public static string LocateAsset(
            string assetPath, string componentType = null, string nameContains = null,
            string members = null, int limit = 20)
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
                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!string.IsNullOrEmpty(nameContains) &&
                        node.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

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
                            sb.Append(Dump(hit.comp, "", members, serializedByDefault: true));
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
        public static string PeekAssetBatch(string assetPath, string probes)
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
                            members, serializedByDefault: true));
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
        /// </summary>
        private static string Dump(
            Component comp, string header, string members, bool serializedByDefault,
            bool listPropertiesWhenEmpty = false)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                       BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
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
                object value = null;
                var found = false;
                for (var t = type; t != null && !found; t = t.BaseType)
                {
                    var f = t.GetField(name, flags | BindingFlags.DeclaredOnly);
                    if (f != null)
                    {
                        value = f.GetValue(comp);
                        found = true;
                        break;
                    }

                    var p = t.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                    if (p == null || !p.CanRead) continue;
                    // 有些 getter 呼叫下去會 native abort，managed catch 攔不到 —— 見 ProbeMineField
                    if (ProbeMineField.IsMine(p))
                    {
                        sb.AppendLine($"  {name} = # 跳過（已知會讓 Editor 閃退，或 [Obsolete]）");
                        found = true;
                        continue;
                    }

                    value = ProbeMineField.ReadGuarded(p, comp);
                    found = true;
                }

                if (!found)
                {
                    if (!string.IsNullOrEmpty(members))
                        sb.AppendLine($"  {name} = # 找不到這個欄位/屬性");
                    continue;
                }

                sb.AppendLine(
                    $"  {name}{(PrefabOverrideMark.Contains(overrides, name) ? "*" : "")} = {Show(value)}");
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
        /// </summary>
        public static string DumpAll(Component comp, bool includeProperties)
        {
            var type = comp.GetType();
            var header = $"{PathOf(comp.transform)}.{type.Name}" +
                         $"  [{(Application.isPlaying ? "PlayMode" : "EditMode")}]";

            var crash = ProbeMineField.HarvestCrashReport();
            var sb = new StringBuilder();
            if (crash != null) sb.AppendLine(crash);

            sb.Append(Dump(comp, header, null, serializedByDefault: true));

            if (!includeProperties) return sb.ToString();

            var props = PropertyNames(type);
            if (props.Count > 0)
            {
                sb.AppendLine("  # --- 屬性 ---");
                sb.Append(Dump(comp, "", string.Join(",", props), serializedByDefault: false));
            }

            return sb.ToString();
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

        private static string Show(object v)
        {
            try
            {
                return Show(v, 0);
            }
            catch (Exception e)
            {
                // dump 是除錯工具：一個欄位印不出來不該讓整份輸出消失
                return $"<throw {e.GetType().Name}>";
            }
        }

        private static string Show(object v, int depth)
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
                    return o == null ? $"null <{o.GetType().Name}>" : $"{o.name} <{o.GetType().Name}>";
                case IEnumerable e when !(v is string):
                {
                    var items = e.Cast<object>().Take(6).Select(x => Show(x, depth + 1)).ToList();
                    var total = e.Cast<object>().Count();
                    return $"[{string.Join(", ", items)}{(total > 6 ? $", … +{total - 6}" : "")}]";
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
                        fields.Select(f => $"{f.Name}={Show(f.GetValue(vt), depth + 1)}")) + "}";
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
