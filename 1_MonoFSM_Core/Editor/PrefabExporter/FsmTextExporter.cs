using System.Collections.Generic;
using System.Linq;
using System.Text;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Editor
{
    // FSM-aware 文字匯出工具。把一個 MonoFSM Entity prefab 匯出成 markdown 風格的 .fsm 文字
    // 給 LLM 閱讀／規劃用。read-only，單向匯出，不負責 round-trip 回 prefab。
    public static class FsmTextExporter
    {
        /// <summary>
        /// FSM 匯出的重點就是「為什麼這樣接」，而 why 只寫在 note 裡（Description 不含 note），
        /// 所以這裡給的截斷上限比 hierarchy 寬。
        /// </summary>
        private const int NoteMaxLength = 200;

        private static string NoteSuffix(Component c) =>
            NoteText.Suffix(NoteText.Of(c, NoteMaxLength));

        public static string Export(GameObject root)
        {
            if (root == null) return string.Empty;

            var sb = new StringBuilder();
            var stateFolders = root.GetComponentsInChildren<StateFolder>(true);

            if (stateFolders.Length == 0)
            {
                sb.AppendLine($"# (no FSM found in '{root.name}')");
                return sb.ToString();
            }

            // 全部 StateFolder 視為已知 FSM。最淺的當主 FSM，其它列為 sub-FSM 並各自輸出 block。
            var ordered = stateFolders
                .OrderBy(sf => GetDepth(sf.transform, root.transform))
                .ToArray();

            var allFsmPaths = new HashSet<string>(
                ordered.Select(sf => GetPath(sf.transform.parent, root.transform)));

            for (int i = 0; i < ordered.Length; i++)
            {
                if (i > 0) sb.AppendLine();
                ExportFsm(ordered[i], root.transform, allFsmPaths, sb, isMain: i == 0);
            }

            return sb.ToString();
        }

        private static void ExportFsm(
            StateFolder stateFolder,
            Transform rootTr,
            HashSet<string> allFsmPaths,
            StringBuilder sb,
            bool isMain)
        {
            var fsmRoot = stateFolder.transform.parent;
            var fsmPath = GetPath(fsmRoot, rootTr);
            var heading = isMain ? "# FSM" : "# Sub-FSM";
            sb.AppendLine($"{heading}: {fsmRoot.name}");
            sb.AppendLine($"> Path: {fsmPath}");
            sb.AppendLine();

            // Variables：找同層的 VariableFolder
            var varFolder = fsmRoot.GetComponentsInChildren<VariableFolder>(true)
                .FirstOrDefault(vf => vf.transform.parent == fsmRoot);
            ExportVariables(varFolder, sb);

            // States
            sb.AppendLine("## States");
            sb.AppendLine();
            foreach (Transform stateTr in stateFolder.transform)
            {
                var state = stateTr.GetComponent<MonoStateBehaviour>();
                if (state == null) continue;
                ExportState(state, sb);
            }

            // Sub-FSMs：找 fsmRoot 子樹中其它 StateFolder（排除自己）
            var subFolders = fsmRoot.GetComponentsInChildren<StateFolder>(true)
                .Where(sf => sf != stateFolder)
                .ToArray();
            if (subFolders.Length > 0)
            {
                sb.AppendLine("## Sub FSMs");
                foreach (var sub in subFolders)
                {
                    var subOwnerPath = GetPath(sub.transform.parent, rootTr);
                    sb.AppendLine($"- {subOwnerPath}");
                }
                sb.AppendLine();
            }
        }

        private static void ExportVariables(VariableFolder varFolder, StringBuilder sb)
        {
            if (varFolder == null) return;

            var vars = varFolder.GetComponentsInChildren<AbstractMonoVariable>(true)
                .Where(v => v.transform.parent == varFolder.transform || HasParentVarFolderEquals(v, varFolder))
                .ToArray();

            if (vars.Length == 0) return;

            sb.AppendLine("## Variables");
            foreach (var v in vars)
            {
                var typeName = v.GetType().Name;
                var displayName = CleanName(v.name);
                var desc = SafeDescription(v);
                if (!string.IsNullOrEmpty(desc) && desc != displayName)
                    sb.AppendLine($"- {displayName} : {typeName}  — {desc}{NoteSuffix(v)}");
                else
                    sb.AppendLine($"- {displayName} : {typeName}{NoteSuffix(v)}");
            }
            sb.AppendLine();
        }

        private static bool HasParentVarFolderEquals(Component child, VariableFolder folder)
        {
            // 走到第一個 VariableFolder 為止，必須是 folder 本人
            var t = child.transform.parent;
            while (t != null)
            {
                var vf = t.GetComponent<VariableFolder>();
                if (vf != null) return vf == folder;
                t = t.parent;
            }
            return false;
        }

        private static void ExportState(MonoStateBehaviour state, StringBuilder sb)
        {
            var stateName = CleanName(state.name);
            sb.AppendLine($"### {stateName}{NoteSuffix(state)}");

            // 走 state 的 direct children，分類成 transitions / actions container
            var transitions = new List<TransitionBehaviour>();
            var actionContainers = new List<Transform>();
            foreach (Transform child in state.transform)
            {
                var tr = child.GetComponent<TransitionBehaviour>();
                if (tr != null) transitions.Add(tr);
                else actionContainers.Add(child);
            }

            // Actions：收集所有 action container 子樹下的 behaviour（不只 AbstractStateAction，
            // 也包含 AnimatorPlayAction 這類 render behaviour），但要濾掉 condition / transition / state
            var actions = new List<AbstractDescriptionBehaviour>();
            foreach (var c in actionContainers)
            {
                foreach (var b in c.GetComponentsInChildren<AbstractDescriptionBehaviour>(true))
                {
                    if (b is AbstractConditionBehaviour) continue;
                    if (b is TransitionBehaviour) continue;
                    if (b is MonoStateBehaviour) continue;
                    actions.Add(b);
                }
            }

            if (actions.Count > 0)
            {
                sb.AppendLine("  enter:");
                foreach (var a in actions)
                {
                    var desc = SafeDescription(a);
                    var typeName = a.GetType().Name;
                    sb.AppendLine($"    - {desc}  ({typeName}){NoteSuffix(a)}");
                }
            }

            foreach (var tr in transitions)
                ExportTransition(tr, sb);

            sb.AppendLine();
        }

        private static void ExportTransition(TransitionBehaviour tr, StringBuilder sb)
        {
            var targetName = tr._target != null ? CleanName(tr._target.name) : "?";
            sb.AppendLine($"  → {targetName}{NoteSuffix(tr)}");

            // direct child conditions
            foreach (Transform child in tr.transform)
            {
                var cond = child.GetComponent<AbstractConditionBehaviour>();
                if (cond == null) continue;
                ExportCondition(cond, sb, indent: "    ");
            }
        }

        private static void ExportCondition(AbstractConditionBehaviour cond, StringBuilder sb, string indent)
        {
            var desc = SafeDescription(cond);
            var typeName = cond.GetType().Name;
            var prefix = cond.FinalResultInverted ? "if not " : "if ";
            sb.AppendLine($"{indent}{prefix}{desc}  [{typeName}]{NoteSuffix(cond)}");
        }

        // ---- helpers ----

        private static string SafeDescription(AbstractDescriptionBehaviour b)
        {
            if (b == null) return "?";
            try
            {
                var d = b.Description;
                if (!string.IsNullOrEmpty(d)) return d;
            }
            catch
            {
                // Description 可能在 edit-time 依賴 runtime 欄位而拋例外，fallback 用 name
            }
            return CleanName(b.name);
        }

        private static string CleanName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            // 移除前綴 tag 如 "[State] "、"[Transition] " 等
            var s = raw;
            while (s.StartsWith("["))
            {
                var end = s.IndexOf(']');
                if (end < 0) break;
                s = s.Substring(end + 1).TrimStart();
            }
            return s;
        }

        private static int GetDepth(Transform t, Transform root)
        {
            int d = 0;
            while (t != null && t != root) { d++; t = t.parent; }
            return d;
        }

        private static string GetPath(Transform t, Transform root)
        {
            if (t == null) return "";
            if (t == root) return t.name;
            var stack = new Stack<string>();
            while (t != null && t != root) { stack.Push(t.name); t = t.parent; }
            if (root != null) stack.Push(root.name);
            return string.Join("/", stack);
        }
    }
}
