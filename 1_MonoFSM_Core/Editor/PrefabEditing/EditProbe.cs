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
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Transform node;
            Component comp;
            try
            {
                node = EditResolve.NodeInRoots(scene.GetRootGameObjects().ToList(), nodePath);
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

        private static string Show(object v)
        {
            switch (v)
            {
                case null: return "null";
                case string s: return s.Length > 60 ? s.Substring(0, 60) + "…" : s;
                case float f: return f.ToString("0.###");
                case UnityEngine.Object o: return $"{o.name} <{o.GetType().Name}>";
                case IEnumerable e when !(v is string):
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
