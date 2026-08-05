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
        /// 欄位名逗號分隔；留空 = 列出所有 public 屬性中的簡單值。
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

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                       BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            var type = comp.GetType();
            var sb = new StringBuilder(
                $"{nodePath}.{type.Name}  [{(Application.isPlaying ? "PlayMode" : "EditMode")}]\n");

            var names = string.IsNullOrEmpty(members)
                ? type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                    .Select(p => p.Name).ToList()
                : members.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

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
                    try
                    {
                        value = p.GetValue(comp);
                    }
                    catch (Exception e)
                    {
                        value = $"<throw {e.GetType().Name}>";
                    }

                    found = true;
                }

                if (!found)
                {
                    if (!string.IsNullOrEmpty(members))
                        sb.AppendLine($"  {name} = # 找不到這個欄位/屬性");
                    continue;
                }

                sb.AppendLine($"  {name} = {Show(value)}");
            }

            return sb.ToString();
        }

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

        private static string Show(object v) => Show(v, 0);

        private static string Show(object v, int depth)
        {
            switch (v)
            {
                case null: return "null";
                case string s: return s.Length > 60 ? s.Substring(0, 60) + "…" : s;
                case float f: return f.ToString("0.###");
                case UnityEngine.Object o: return $"{o.name} <{o.GetType().Name}>";
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
