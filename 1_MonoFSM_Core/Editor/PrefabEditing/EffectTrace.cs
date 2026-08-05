using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 「這個 EffectReceiver 為什麼沒觸發」的一次性診斷。
    ///
    /// 存在的理由是省 context：這條鏈有六段（detector 偵測 → detectable dict → dealer 有效 →
    /// receiver 配對 → enterNode gate → action），每段都可能靜靜地 return，
    /// 逐段 peek 要十幾次來回。這裡一次把每段的真值攤開，並指出卡在哪一段。
    ///
    /// 全程反射 + 型別名比對，不對 runtime assembly 產生編譯期依賴。
    /// </summary>
    public static class EffectTrace
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public |
                                          BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        /// <summary>
        /// nodePath 可以是 receiver 本身，也可以是它的祖先（會往子孫找第一個 GeneralEffectReceiver）。
        /// effectTypeFilter 有給時，只看 effectType 名稱含這段的 receiver。
        /// </summary>
        public static string Trace(string nodePath, string effectTypeFilter = null)
        {
            Transform node;
            try
            {
                node = EditResolve.NodeInRoots(EditResolve.RuntimeRoots(), nodePath);
            }
            catch (EditResolve.EditAbort abort)
            {
                return $"# {abort.Message}";
            }

            var receivers = node.GetComponentsInChildren<Component>(true)
                .Where(c => c != null && TypeName(c) == "GeneralEffectReceiver")
                .Where(c => effectTypeFilter == null ||
                            Name(Get(c, "_effectType")).IndexOf(effectTypeFilter,
                                StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (receivers.Count == 0)
                return $"# {nodePath} 底下找不到 GeneralEffectReceiver" +
                       (effectTypeFilter != null ? $"（effectType 含 '{effectTypeFilter}'）" : "");

            var sb = new StringBuilder(
                $"# effect-trace [{(Application.isPlaying ? "PlayMode" : "EditMode")}]" +
                (Application.isPlaying ? "" : " —— 沒在 Play Mode，runtime 欄位都會是初始值") + "\n");

            foreach (var receiver in receivers)
            {
                TraceOne(receiver, sb);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void TraceOne(Component receiver, StringBuilder sb)
        {
            var effectType = Get(receiver, "_effectType");
            //_enterNode 是 [AutoChildren] 填的，EditMode 下還是 null —— 退回用階層找，才不會誤報「沒有 enterNode」
            var enterNode = Get(receiver, "_enterNode") as Component
                            ?? FirstChildOfType(receiver.transform, "EffectEnterNode");
            var dealers = Get(receiver, "_dealers") as IDictionary;

            sb.AppendLine($"receiver {Path(receiver.transform)}");
            sb.AppendLine($"  effectType={Name(effectType)} IsValid={Call(receiver, "IsValid")} " +
                          $"HasDealerOverlap={Call(receiver, "HasDealerOverlap")} " +
                          $"enterNode={(enterNode == null ? "null" + Hint("← 沒有 [Event] EffectEnterNode 子節點") : enterNode.name)}");

            // ── detectable：receiver 有沒有被登記進去（key 就是 effectType）
            var detectable = FindInParents(receiver.transform, "EffectDetectable");
            if (detectable == null)
            {
                sb.AppendLine("  detectable=null ← receiver 的祖先沒有 EffectDetectable，永遠不會被 detector 找到");
            }
            else
            {
                var keys = Prop(detectable, "GetKeys") as IEnumerable;
                var registered = keys != null && keys.Cast<object>()
                    .Any(k => ReferenceEquals(k, effectType));
                sb.AppendLine($"  detectable={detectable.name} IsValid={Call(detectable, "IsValid")} " +
                              $"registered={(registered ? "YES" : "NO" + Hint("← dict 裡沒有這個 effectType，dealer 配對會拿到 null"))}");
                sb.AppendLine($"    detectTargets={Count(Get(detectable, "_effectDetectTargets"))} " +
                              $"debugDetectors={Show(Get(detectable, "_debugDetectors"))}");
            }

            // ── 目前重疊中的 dealer
            if (dealers == null || dealers.Count == 0)
            {
                sb.AppendLine("  overlapping dealers: 0" + Hint("← 沒有任何 dealer 打進來"));
                ListCandidateDealers(receiver, effectType, sb);
            }
            else
            {
                sb.AppendLine($"  overlapping dealers: {dealers.Count}");
                foreach (var key in dealers.Keys)
                {
                    var dealer = key as Component;
                    if (dealer == null) continue;
                    var detector = FindInParents(dealer.transform, "EffectDetector");
                    sb.AppendLine($"    {Path(dealer.transform)}");
                    sb.AppendLine($"      IsValid={Call(dealer, "IsValid")} " +
                                  $"fail={Q(Get(dealer, "_failReason"))} " +
                                  $"detector={(detector == null ? "null" : detector.name)} " +
                                  $"{(detector == null ? "" : "valueInfo=" + Call(detector, "ValueInfo"))}");
                }
            }

            // ── enterNode 的四道 gate（這次踩的坑就在最後一道）
            if (enterNode == null) return;
            var lastSim = Get(enterNode, "_lastSimulateEventTime");
            //_parentObj 同樣是 Auto 填的，EditMode 下退回用階層找
            var parentObj = Get(enterNode, "_parentObj") as Component
                            ?? FindInParents(enterNode.transform, "MonoObj");
            sb.AppendLine($"  enterNode {enterNode.name}");
            sb.AppendLine($"    lastSimulateEventTime={Show(lastSim)}" +
                          $"{(IsNever(lastSim) ? Hint("← 從來沒執行過底下的 action") : "")} " +
                          $"lastSkipReason={Q(Get(enterNode, "_lastSkipReason"))} " +
                          $"@{Show(Get(enterNode, "_lastSkipTime"))}");
            sb.AppendLine($"    activeSelf={enterNode.gameObject.activeSelf} " +
                          $"conditions={Call(Get(enterNode, "_conditionFolder"), "IsValid")} " +
                          $"forceWithoutAuthority={Get(enterNode, "_forceExecuteWithoutStateAuthority")}");
            if (parentObj != null)
                sb.AppendLine($"    parentObj={parentObj.name} " +
                              $"ShouldSimulte={Call(parentObj, "ShouldSimulte")} " +
                              $"IsCulling={Call(parentObj, "IsCulling")}" +
                              $"{(Truthy(Call(parentObj, "ShouldSimulte")) ? "" : Hint("← 沒有 authority，事件會靜靜地不執行"))}");
            else
                sb.AppendLine("    parentObj=null ← 不在任何 MonoObj 底下，不會被 WorldUpdateSimulator 更新");
        }

        /// <summary>沒有 dealer 打進來時，把場上帶同 effectType 的 dealer 列出來（含距離），看是誰該打進來。</summary>
        private static void ListCandidateDealers(Component receiver, object effectType,
            StringBuilder sb)
        {
            if (effectType == null) return;
            var all = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(c => c != null && TypeName(c) == "GeneralEffectDealer" &&
                            ReferenceEquals(Get(c, "_effectType"), effectType))
                .ToList();

            if (all.Count == 0)
            {
                sb.AppendLine($"    場上沒有任何 dealer 的 effectType 是 {Name(effectType)} ← 沒人會打它");
                return;
            }

            sb.AppendLine($"    場上有 {all.Count} 個同 effectType 的 dealer：");
            foreach (var dealer in all.OrderBy(d =>
                         Vector3.Distance(d.transform.position, receiver.transform.position))
                         .Take(5))
            {
                var detector = FindInParents(dealer.transform, "EffectDetector");
                var dist = Vector3.Distance(dealer.transform.position, receiver.transform.position);
                sb.AppendLine($"      {Path(dealer.transform)} dist={dist:0.##} " +
                              $"IsValid={Call(dealer, "IsValid")} " +
                              $"detector={(detector == null ? "null ← dealer 不在 detector 底下，永遠不會偵測" : detector.name)} " +
                              $"{(detector == null ? "" : "valueInfo=" + Call(detector, "ValueInfo"))}");
            }
        }

        // ── 反射小工具（型別名比對，不依賴 runtime assembly）

        private static string TypeName(Component c) => c.GetType().Name;

        //EditMode 下 runtime cache（Auto 欄位、dict、overlap）全是空的，
        //那些「所以壞在這」的結論只有 Play Mode 講得準
        private static string Hint(string text) => Application.isPlaying ? " " + text : "";

        private static Component FirstChildOfType(Transform parent, string typeName)
        {
            foreach (Transform child in parent)
            {
                var hit = child.GetComponents<Component>()
                    .FirstOrDefault(c => c != null && IsOrDerives(c.GetType(), typeName));
                if (hit != null) return hit;
            }

            return null;
        }

        private static Component FindInParents(Transform from, string typeName)
        {
            for (var t = from; t != null; t = t.parent)
            {
                var hit = t.GetComponents<Component>()
                    .FirstOrDefault(c => c != null && IsOrDerives(c.GetType(), typeName));
                if (hit != null) return hit;
            }

            return null;
        }

        private static bool IsOrDerives(Type type, string typeName)
        {
            for (var t = type; t != null; t = t.BaseType)
                if (t.Name == typeName)
                    return true;
            return false;
        }

        private static object Get(object target, string fieldName)
        {
            if (target == null) return null;
            for (var t = target.GetType(); t != null; t = t.BaseType)
            {
                var f = t.GetField(fieldName, Flags | BindingFlags.DeclaredOnly);
                if (f != null) return f.GetValue(target);
            }

            return null;
        }

        private static object Call(object target, string memberName)
        {
            var value = Prop(target, memberName);
            return value == null ? "n/a" : Show(value);
        }

        /// <summary>property 或 field 的原始值（要拿來比對時用，Call 回的是印出來的字串）。</summary>
        private static object Prop(object target, string memberName)
        {
            if (target == null) return null;
            for (var t = target.GetType(); t != null; t = t.BaseType)
            {
                var p = t.GetProperty(memberName, Flags | BindingFlags.DeclaredOnly);
                if (p == null || !p.CanRead) continue;
                try
                {
                    return p.GetValue(target);
                }
                catch (Exception e)
                {
                    return $"<throw {e.GetType().Name}>";
                }
            }

            return Get(target, memberName);
        }

        private static bool Truthy(object shown) => shown is string s && s == "True";

        private static bool IsNever(object v) => v is float f && f < 0f;

        private static string Q(object v) =>
            v == null ? "null" : $"'{Show(v)}'";

        private static int Count(object v) => v is ICollection c ? c.Count : 0;

        private static string Name(object v) =>
            v is UnityEngine.Object o ? o.name : v?.ToString() ?? "null";

        private static string Path(Transform t)
        {
            var parts = new List<string>();
            for (var cur = t; cur != null; cur = cur.parent)
                parts.Add(cur.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Show(object v)
        {
            switch (v)
            {
                case null: return "null";
                case string s: return s.Length > 80 ? s.Substring(0, 80) + "…" : s;
                case float f: return f.ToString("0.###");
                case UnityEngine.Object o: return o.name;
                case IEnumerable e:
                {
                    var items = e.Cast<object>().Take(6).Select(Show).ToList();
                    var total = e.Cast<object>().Count();
                    return $"[{string.Join(", ", items)}{(total > 6 ? $", … +{total - 6}" : "")}]";
                }
                default: return v.ToString();
            }
        }
    }
}
