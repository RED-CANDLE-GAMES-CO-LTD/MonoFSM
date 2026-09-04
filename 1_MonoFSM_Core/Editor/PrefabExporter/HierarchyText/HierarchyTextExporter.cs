using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor
{
    // Hierarchy → 精簡結構化文字匯出。Editor only、單向匯出（不 round-trip）。
    // 格式 spec 見 MonoFSM/skills/hierarchy-text-exporter/SKILL.md
    public static class HierarchyTextExporter
    {
        private static readonly Dictionary<string, Type> _typeByName = new();

        public static string Export(GameObject root, HierarchyExportOptions options = null)
        {
            if (root == null) return string.Empty;
            options ??= HierarchyExportOptions.Default;

            var sb = new StringBuilder();
            if (PrefabUtility.IsPartOfPrefabAsset(root))
            {
                var rootAssetPath = AssetDatabase.GetAssetPath(root);
                if (!string.IsNullOrEmpty(rootAssetPath))
                    sb.AppendLine($"# prefab: res:{CompactValueFormatter.StripAssetsPrefix(rootAssetPath)}");
            }

            var ctx = new HierarchyExportContext { Root = root.transform, Options = options };
            TraverseNode(root, "", 0, sb, ctx);
            return sb.ToString();
        }

        public static string ExportToFile(GameObject root, HierarchyExportOptions options = null, string path = null)
        {
            var text = Export(root, options);
            path ??= $"Temp/HierarchyExport/{(root != null ? root.name : "unnamed")}.txt";
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, text);
            var abs = Path.GetFullPath(path);
            return $"written {text.Length} chars to {abs}";
        }

        private static void TraverseNode(GameObject go, string path, int depth, StringBuilder sb, HierarchyExportContext ctx)
        {
            var opt = ctx.Options;
            ctx.Current = go.transform;
            var indent = new string(' ', depth * 2);
            var forcedExpand = opt.IsForcedExpand(path);

            if (!opt._includeInactive && !go.activeSelf && depth > 0)
            {
                sb.AppendLine($"{indent}~{NodeName(go)}{FoldTail(go, opt)}");
                return;
            }

            if (opt._foldBareTransformChains && !forcedExpand && depth > 0 &&
                go.transform.childCount > 0 && IsTransformOnlySubtree(go))
            {
                var n = CountDescendants(go);
                var flags = BuildFlags(go);
                var trPart = FormatTransform(go.transform);
                var tr = string.IsNullOrEmpty(trPart) ? "" : " " + trPart;
                sb.AppendLine($"{indent}{flags}{NodeName(go)}{tr} :: bones/transform-only (+{n} nodes)");
                return;
            }

            if (opt._foldKnownSubtrees && !forcedExpand)
            {
                var summarizer = SubtreeSummarizerRegistry.Find(go);
                if (summarizer != null)
                {
                    var flags = BuildFlags(go);
                    sb.AppendLine(
                        $"{indent}{flags}{NodeName(go)} {summarizer.Summarize(go)}{FoldTail(go, opt)}");
                    return;
                }
            }

            if (opt._maxDepth >= 0 && depth > opt._maxDepth && !forcedExpand)
            {
                var flags = BuildFlags(go);
                sb.AppendLine($"{indent}{flags}{NodeName(go)}{FoldTail(go, opt)}");
                return;
            }

            sb.AppendLine(BuildNodeLine(go, indent, ctx));

            var children = new List<Transform>();
            foreach (Transform child in go.transform) children.Add(child);

            var maxChildren = opt._maxChildrenPerNode;
            var shown = maxChildren <= 0 ? children.Count : Math.Min(children.Count, maxChildren);
            for (var i = 0; i < shown; i++)
            {
                var child = children[i];
                var childPath = path.Length == 0 ? child.name : path + "/" + child.name;
                TraverseNode(child.gameObject, childPath, depth + 1, sb, ctx);
                ctx.Current = go.transform; // 遞迴回來後恢復目前 node
            }

            if (children.Count > shown)
            {
                var childIndent = new string(' ', (depth + 1) * 2);
                sb.AppendLine($"{childIndent}… (+{children.Count - shown} more siblings)");
            }
        }

        /// <summary>
        /// 節點名裡的換行會把一行的樹狀輸出切成兩行，讓路徑沒辦法直接抄回 --node。
        /// 自動命名把 localized 文案（本身含換行）塞進名字時就會發生，所以一律逃逸成 `\n`
        /// —— 這也是 uprefab 路徑解析吃得下的寫法。
        /// </summary>
        private static string NodeName(GameObject go) =>
            // `\\` 先換，跟 EditResolve.EscapeName 同一套規則，抄回 --node 才解得回原名
            go.name.Replace("\r", "").Replace("\\", "\\\\").Replace("\n", "\\n");

        private static string BuildNodeLine(GameObject go, string indent, HierarchyExportContext ctx)
        {
            var sb = new StringBuilder();
            sb.Append(indent);
            sb.Append(BuildFlags(go));
            sb.Append(NodeName(go));

            var transformPart = FormatTransform(go.transform);
            if (!string.IsNullOrEmpty(transformPart))
                sb.Append(' ').Append(transformPart);

            var compsPart = BuildComponentsBlock(go, ctx);
            if (!string.IsNullOrEmpty(compsPart))
                sb.Append(' ').Append(compsPart);

            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                var srcPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                if (!string.IsNullOrEmpty(srcPath))
                    sb.Append($" (prefab:res:{CompactValueFormatter.StripAssetsPrefix(srcPath)})");
            }

            // note 提到行尾當註解，不留在欄位堆裡：欄位堆會被 _maxFieldCharsPerComponent 截掉，
            // 而 note 是掃階層時最該一眼看到的東西
            sb.Append(NoteText.NodeSuffix(go, ctx.Options._maxNoteLength));

            return sb.ToString();
        }

        /// <summary>
        /// 折疊行的尾巴：展開成本 + 子樹裡藏了幾則 note + 自己的 note。
        /// 沒有 note 數的話，讀的人無從判斷這個 (+N nodes) 值不值得下鑽。
        /// </summary>
        private static string FoldTail(GameObject go, HierarchyExportOptions opt)
        {
            var nodes = CountDescendants(go);
            var notes = NoteText.CountInDescendants(go);
            var notePart = notes > 0 ? $", {notes} notes" : "";
            return $" (+{nodes} nodes{notePart}){NoteText.NodeSuffix(go, opt._maxNoteLength)}";
        }

        private static string BuildFlags(GameObject go)
        {
            var flags = "";
            if (!go.activeSelf) flags += "~";
            if (PrefabUtility.IsAddedGameObjectOverride(go)) flags += "+";
            return flags;
        }

        private static string FormatTransform(Transform t)
        {
            var parts = new List<string>();
            var p = t.localPosition;
            if (p != Vector3.zero)
                parts.Add($"p=({Num(p.x)},{Num(p.y)},{Num(p.z)})");

            var e = t.localEulerAngles;
            if (e != Vector3.zero)
                parts.Add($"r=({Num(e.x)},{Num(e.y)},{Num(e.z)})");

            var s = t.localScale;
            if (s != Vector3.one)
            {
                if (Mathf.Approximately(s.x, s.y) && Mathf.Approximately(s.y, s.z))
                    parts.Add($"s={Num(s.x)}");
                else
                    parts.Add($"s=({Num(s.x)},{Num(s.y)},{Num(s.z)})");
            }

            return string.Join(" ", parts);
        }

        private static string Num(float f)
        {
            if (Mathf.Approximately(f, Mathf.Round(f))) return Mathf.RoundToInt(f).ToString();
            return f.ToString("0.###");
        }

        private static string BuildComponentsBlock(GameObject go, HierarchyExportContext ctx)
        {
            var opt = ctx.Options;
            var entries = new List<string>();
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp is Transform) continue;

                if (comp == null)
                {
                    entries.Add("<!MissingScript>");
                    continue;
                }

                var type = comp.GetType();
                if (!ComponentAllowed(type, opt)) continue;

                entries.Add(BuildComponentEntry(comp, ctx));
            }

            if (entries.Count == 0) return "";
            return "<" + string.Join(" | ", entries) + ">";
        }

        private static string BuildComponentEntry(Component comp, HierarchyExportContext ctx)
        {
            var opt = ctx.Options;
            var type = comp.GetType();
            var prefix = "";
            if (PrefabUtility.IsAddedComponentOverride(comp)) prefix += "+";
            if (IsDisabled(comp)) prefix += "-";

            var fields = new List<string>();
            var so = new SerializedObject(comp);
            var prop = so.GetIterator();
            ctx.CurrentComponentType = type;
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.name == "m_Script" || prop.name == "m_GameObject" ||
                        prop.name == "m_ObjectHideFlags" || prop.name == "m_Enabled")
                        continue;

                    // 已經被提到節點行尾了（NoteText.NodeSuffix）
                    if (prop.name == "_note" || prop.name == "note")
                        continue;

                    // 判準抽到 PrefabOverrideMark，跟 uprefab 的 peek / 寫後驗證共用一份
                    var isOverride = opt._markOverrides &&
                                     PrefabEditing.PrefabOverrideMark.IsMeaningfulOverride(prop);

                    if (opt._showOverridesOnly)
                    {
                        if (!isOverride) continue;
                    }
                    else if (opt._excludeDefaults && !isOverride && ComponentDefaultCache.IsDefaultValue(prop, type))
                    {
                        continue;
                    }

                    var fieldName = prop.name + (isOverride ? "*" : "");

                    if (prop.propertyType == SerializedPropertyType.Boolean)
                    {
                        if (prop.boolValue) fields.Add(fieldName);
                        else fields.Add($"{fieldName}=off");
                        continue;
                    }

                    var value = CompactValueFormatter.FormatValue(prop, ctx);
                    if (value == "{}") continue; // 巢狀子欄位全是預設值，整欄略過
                    fields.Add($"{fieldName}={value}");
                } while (prop.NextVisible(false));
            }

            ctx.CurrentComponentType = null;

            var cap = opt._maxFieldCharsPerComponent;
            if (cap > 0)
            {
                var total = 0;
                for (var i = 0; i < fields.Count; i++)
                {
                    total += fields[i].Length + 1;
                    if (total > cap && i > 0)
                    {
                        var dropped = fields.Count - i;
                        fields.RemoveRange(i, dropped);
                        fields.Add($"…(+{dropped} more)");
                        break;
                    }
                }
            }

            var body = fields.Count == 0 ? type.Name : $"{type.Name} {string.Join(" ", fields)}";
            return prefix + body;
        }

        private static bool IsDisabled(Component comp)
        {
            return comp switch
            {
                Behaviour b => !b.enabled,
                Collider c => !c.enabled,
                Renderer r => !r.enabled,
                _ => false
            };
        }

        private static bool ComponentAllowed(Type t, HierarchyExportOptions opt)
        {
            if (opt._includeComponents.Count > 0 && !MatchesAny(t, opt._includeComponents)) return false;
            if (opt._excludeComponents.Count > 0 && MatchesAny(t, opt._excludeComponents)) return false;
            return true;
        }

        private static bool MatchesAny(Type t, List<string> names)
        {
            foreach (var n in names)
            {
                if (t.Name == n || t.FullName == n) return true;
                var named = FindTypeByName(n);
                if (named != null && named.IsAssignableFrom(t)) return true;
            }
            return false;
        }

        private static Type FindTypeByName(string name)
        {
            if (_typeByName.TryGetValue(name, out var cached)) return cached;

            var type = Type.GetType(name) ??
                       AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(a =>
                           {
                               try { return a.GetTypes(); }
                               catch { return Array.Empty<Type>(); }
                           })
                           .FirstOrDefault(t => t.FullName == name || t.Name == name);

            _typeByName[name] = type;
            return type;
        }

        private static bool IsTransformOnlySubtree(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Component>(true))
                if (!(c is Transform)) // null（missing script）也算「有東西」，不摺疊
                    return false;
            return true;
        }

        private static int CountDescendants(GameObject go)
        {
            return go.GetComponentsInChildren<Transform>(true).Length - 1;
        }
    }
}
