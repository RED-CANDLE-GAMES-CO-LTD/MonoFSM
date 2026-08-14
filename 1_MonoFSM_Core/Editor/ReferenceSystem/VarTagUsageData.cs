using System;
using System.Collections.Generic;
using System.Text;
using MonoFSM.Variable;

namespace MonoFSM.Editor.ReferenceSystem
{
    public enum VarUsageKind
    {
        Write,
        Read,
        Sync,
        Other
    }

    [Serializable]
    public class VarUsageSite
    {
        public string SourcePath; //prefab 資產路徑或 scene 路徑
        public bool IsScene;
        public string NodePath; //引用方的階層路徑（相對 root）
        public string ComponentType;
        public string FieldName;
        public VarUsageKind Kind;
    }

    [Serializable]
    public class VarProxySite
    {
        public string SourcePath; //prefab 資產路徑或 scene 路徑
        public bool IsScene;
        public string NodePath; //proxy 節點的階層路徑
        public string VariableType; //VarBool / VarFloat / ...
    }

    /// <summary>某顆變數在單一檔案（prefab 或 scene）內的所有使用</summary>
    public class SourceUsageGroup
    {
        public string SourcePath;
        public bool IsScene;
        public readonly List<VarProxySite> Proxies = new();
        public readonly List<VarUsageSite> Sites = new();
        public int WriteCount, ReadCount, SyncCount, OtherCount;

        /// <summary>"[W2 R1]"，全 0 時是空字串。掃描結束後由 BuildCountLabel 算好，不要每次 get 重算</summary>
        public string CountLabel { get; private set; } = string.Empty;

        public void BuildCountLabel()
        {
            if (WriteCount == 0 && ReadCount == 0 && SyncCount == 0 && OtherCount == 0)
            {
                CountLabel = string.Empty;
                return;
            }

            var sb = new StringBuilder("[");
            if (WriteCount > 0)
                sb.Append('W').Append(WriteCount).Append(' ');
            if (ReadCount > 0)
                sb.Append('R').Append(ReadCount).Append(' ');
            if (SyncCount > 0)
                sb.Append('S').Append(SyncCount).Append(' ');
            if (OtherCount > 0)
                sb.Append('O').Append(OtherCount).Append(' ');
            if (sb[sb.Length - 1] == ' ')
                sb.Length -= 1;
            sb.Append(']');
            CountLabel = sb.ToString();
        }
    }

    public class VarTagUsage
    {
        public VariableTag Tag;
        public readonly List<VarProxySite> Proxies = new();
        public readonly List<VarUsageSite> Sites = new();

        /// <summary>用到這顆變數的來源檔案分組（prefab 在前、scene 在後），由 RecountFromSites 重建</summary>
        public readonly List<SourceUsageGroup> Groups = new();

        public int WriteCount;
        public int ReadCount;
        public int SyncCount;
        public int OtherCount;

        public string TagName => Tag != null ? Tag.name : "<null>";

        public string Warning
        {
            get
            {
                if (Proxies.Count == 0)
                    return "⚠ 死宣告（沒有任何 proxy）";
                if (WriteCount > 0 && ReadCount == 0)
                    return "⚠ 只寫不讀";
                if (WriteCount == 0 && ReadCount > 0)
                    return "⚠ 只讀不寫";
                return null;
            }
        }

        /// <summary>只在掃描結束呼叫一次：重算總數並依來源檔案重建 Groups</summary>
        public void RecountFromSites()
        {
            WriteCount = ReadCount = SyncCount = OtherCount = 0;
            Groups.Clear();

            var groupByPath = new Dictionary<string, SourceUsageGroup>(StringComparer.Ordinal);

            foreach (var p in Proxies)
            {
                var g = GetOrAddGroup(groupByPath, p.SourcePath, p.IsScene);
                g.Proxies.Add(p);
            }

            foreach (var s in Sites)
            {
                var g = GetOrAddGroup(groupByPath, s.SourcePath, s.IsScene);
                g.Sites.Add(s);
                switch (s.Kind)
                {
                    case VarUsageKind.Write:
                        WriteCount++;
                        g.WriteCount++;
                        break;
                    case VarUsageKind.Read:
                        ReadCount++;
                        g.ReadCount++;
                        break;
                    case VarUsageKind.Sync:
                        SyncCount++;
                        g.SyncCount++;
                        break;
                    default:
                        OtherCount++;
                        g.OtherCount++;
                        break;
                }
            }

            //prefab 群組排在 scene 群組前面，各自內部依 SourcePath 排序
            Groups.Sort(CompareGroup);
            foreach (var g in Groups)
                g.BuildCountLabel();
        }

        private SourceUsageGroup GetOrAddGroup(Dictionary<string, SourceUsageGroup> groupByPath,
            string sourcePath, bool isScene)
        {
            var key = sourcePath ?? string.Empty;
            if (groupByPath.TryGetValue(key, out var g))
                return g;
            g = new SourceUsageGroup { SourcePath = key, IsScene = isScene };
            groupByPath.Add(key, g);
            Groups.Add(g);
            return g;
        }

        private static int CompareGroup(SourceUsageGroup a, SourceUsageGroup b)
        {
            if (a.IsScene != b.IsScene)
                return a.IsScene ? 1 : -1;
            return StringComparer.Ordinal.Compare(a.SourcePath, b.SourcePath);
        }
    }
}
