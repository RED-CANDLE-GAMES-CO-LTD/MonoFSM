using System;
using System.Collections.Generic;

namespace MonoFSM.Editor
{
    // HierarchyTextExporter 的匯出選項
    [Serializable]
    public class HierarchyExportOptions
    {
        public int _maxDepth = -1;
        public int _maxChildrenPerNode = 30;
        public bool _excludeDefaults = true;
        public bool _foldKnownSubtrees = true;
        public bool _foldBareTransformChains = true; // 整棵子樹只有 Transform 時折成一行（rig bones）
        public List<string> _expandPaths = new(); // root 相對路徑；尾端 "/*" 整棵展開；"*" 全展開
        public int _expandDepthOverride = -1;
        public List<string> _includeComponents = new(); // 空=全部；短名或 FullName，含子類
        public List<string> _excludeComponents = new();
        public bool _showOverridesOnly = false;
        public bool _markOverrides = true;
        public bool _includeInactive = true; // false 時 inactive 子樹折成 "~Name (+N nodes)"
        public int _maxStringLength = 60;

        // note 是「為什麼這樣做」的唯一出處，比一般字串欄位值錢，給它自己的（比較寬的）上限
        public int _maxNoteLength = 120;
        public int _maxArrayElements = 8;
        public int _maxNestedDepth = 2;
        public int _maxFieldCharsPerComponent = 400; // 單一 component 欄位文字總量上限；<=0 不限

        public static HierarchyExportOptions Default => new();

        public static HierarchyExportOptions FullExpand =>
            new()
            {
                _foldKnownSubtrees = false,
                _foldBareTransformChains = false,
                _maxChildrenPerNode = int.MaxValue,
                _maxFieldCharsPerComponent = 0
            };

        // node 相對路徑 path 是否被強制展開
        public bool IsForcedExpand(string path)
        {
            if (_expandPaths == null || _expandPaths.Count == 0) return false;
            foreach (var e in _expandPaths)
            {
                if (string.IsNullOrEmpty(e)) continue;
                if (e == "*") return true;
                if (path == e) return true;
                if (e.EndsWith("/*"))
                {
                    var prefix = e.Substring(0, e.Length - 2);
                    if (path == prefix || path.StartsWith(prefix + "/")) return true;
                }
                // 祖先必須展開，才能讓後代展開路徑可見
                if (e.StartsWith(path + "/")) return true;
            }
            return false;
        }
    }
}
