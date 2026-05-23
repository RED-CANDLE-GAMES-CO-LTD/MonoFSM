using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoFSM.Condition;
using UnityEditor;

namespace MonoFSM.Core.Editor.PropertyDrawer
{
    internal static class ConditionPresetRegistry
    {
        public class Entry
        {
            public Type ConditionType;
            public string DisplayName;
            public string Category;
            public string ColorHex;
            public int Priority;
            public MethodInfo Setup; // void(TCondition)
        }

        private static List<Entry> _cached;

        public static IReadOnlyList<Entry> All => _cached ??= Collect();

        public static IEnumerable<Entry> ForElementType(Type elementType)
        {
            if (elementType == null) yield break;
            foreach (var e in All)
                if (elementType.IsAssignableFrom(e.ConditionType))
                    yield return e;
        }

        [InitializeOnLoadMethod]
        private static void Reset() => _cached = null;

        private static List<Entry> Collect()
        {
            var list = new List<Entry>();
            const BindingFlags bf =
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            // 所有繼承 AbstractConditionBehaviour 的具體型別
            foreach (var t in TypeCache.GetTypesDerivedFrom<AbstractConditionBehaviour>())
            {
                if (t.IsAbstract) continue;
                foreach (var m in t.GetMethods(bf))
                {
                    foreach (var a in m.GetCustomAttributes<ConditionPresetAttribute>())
                    {
                        var ps = m.GetParameters();
                        if (ps.Length != 1 || !ps[0].ParameterType.IsAssignableFrom(t))
                            continue;

                        list.Add(new Entry
                        {
                            ConditionType = t,
                            DisplayName = string.IsNullOrEmpty(a.DisplayName) ? t.Name : a.DisplayName,
                            Category = a.Category,
                            ColorHex = a.ColorHex,
                            Priority = a.Priority,
                            Setup = m,
                        });
                    }
                }
            }

            return list.OrderByDescending(e => e.Priority)
                .ThenBy(e => e.Category)
                .ThenBy(e => e.DisplayName)
                .ToList();
        }
    }
}
