using System.Collections.Generic;
using System.Linq;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Editor
{
    // 已知子樹的一行摘要，讓 StateFolder/VariableFolder/EffectDetectable 這類常見結構不用每次全展開
    public interface ISubtreeSummarizer
    {
        int Priority { get; }
        bool CanSummarize(GameObject go);
        string Summarize(GameObject go); // 一行，不含 "(+N nodes)"（exporter 補）
    }

    public static class SubtreeSummarizerRegistry
    {
        private static readonly List<ISubtreeSummarizer> _summarizers = new();

        static SubtreeSummarizerRegistry()
        {
            Register(new StateFolderSummarizer());
            Register(new VariableFolderSummarizer());
            Register(new EffectDetectableSummarizer());
        }

        public static void Register(ISubtreeSummarizer summarizer)
        {
            _summarizers.Add(summarizer);
            _summarizers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public static ISubtreeSummarizer Find(GameObject go)
        {
            foreach (var s in _summarizers)
                if (s.CanSummarize(go))
                    return s;
            return null;
        }

        internal static string CleanName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var s = raw;
            while (s.StartsWith("["))
            {
                var end = s.IndexOf(']');
                if (end < 0) break;
                s = s.Substring(end + 1).TrimStart();
            }
            return s;
        }
    }

    public class StateFolderSummarizer : ISubtreeSummarizer
    {
        public int Priority => 0;
        public bool CanSummarize(GameObject go) => go.GetComponent<StateFolder>() != null;

        public string Summarize(GameObject go)
        {
            var names = new List<string>();
            foreach (Transform t in go.transform)
            {
                var st = t.GetComponent<MonoStateBehaviour>();
                if (st != null) names.Add(SubtreeSummarizerRegistry.CleanName(st.name));
            }

            var shown = names.Count > 8
                ? string.Join(", ", names.Take(8)) + ", …"
                : string.Join(", ", names);
            return $"<StateFolder> :: {names.Count} states: {shown}";
        }
    }

    public class VariableFolderSummarizer : ISubtreeSummarizer
    {
        public int Priority => 0;
        public bool CanSummarize(GameObject go) => go.GetComponent<VariableFolder>() != null;

        public string Summarize(GameObject go)
        {
            var folder = go.GetComponent<VariableFolder>();
            var vars = go.GetComponentsInChildren<AbstractMonoVariable>(true)
                .Where(v => HasParentVarFolderEquals(v, folder))
                .ToArray();

            var names = vars.Select(v => $"{SubtreeSummarizerRegistry.CleanName(v.name)}:{v.GetType().Name}").ToList();
            var shown = names.Count > 8
                ? string.Join(", ", names.Take(8)) + ", …"
                : string.Join(", ", names);
            return $"<VariableFolder> :: {names.Count} vars: {shown}";
        }

        private static bool HasParentVarFolderEquals(AbstractMonoVariable child, VariableFolder folder)
        {
            var t = child.transform.parent;
            while (t != null)
            {
                var vf = t.GetComponent<VariableFolder>();
                if (vf != null) return vf == folder;
                t = t.parent;
            }
            return false;
        }
    }

    public class EffectDetectableSummarizer : ISubtreeSummarizer
    {
        public int Priority => 0;
        public bool CanSummarize(GameObject go) => go.GetComponent<EffectDetectable>() != null;

        public string Summarize(GameObject go)
        {
            var receivers = go.GetComponentsInChildren<GeneralEffectReceiver>(true);
            var names = receivers.Select(r => SubtreeSummarizerRegistry.CleanName(r.name)).ToList();
            var shown = names.Count > 8
                ? string.Join(", ", names.Take(8)) + ", …"
                : string.Join(", ", names);
            return $"<EffectDetectable> :: {names.Count} receivers: {shown}";
        }
    }
}
