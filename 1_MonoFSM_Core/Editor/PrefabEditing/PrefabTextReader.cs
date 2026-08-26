using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 把 prefab（或它的某個子樹）匯出成文字。
    ///
    /// 取代了原本的「落檔 cache + marker」機制 —— cache 的兩個問題壓過它省下的 context：
    /// 太容易過期（實測 5 份有 2 份比來源舊，照過期內容做的分析會給出已經不成立的結論），
    /// 而且要靠人記得掛 marker、記得掃新舊。
    ///
    /// 改成用 charBudget 現場分層：**先自動摺到塞得進預算的深度**，摺疊行帶 `(+N nodes)`
    /// 展開成本，要細節再用 subPath 下鑽。省 context 的效果一樣，但讀到的一定是當下真值。
    /// </summary>
    public static class PrefabTextReader
    {
        /// <summary>
        /// 純視覺 component：對「讀邏輯」沒貢獻，卻能佔掉三成以上的輸出。
        /// 走 IsAssignableFrom，所以填 base type 就涵蓋全部子類
        /// （Renderer 一項 = Mesh / Skinned / ParticleSystem / Line / Trail Renderer）。
        /// 專案特有的第三方型別由專案端在 [InitializeOnLoadMethod] 自己加，這裡只放 Unity 內建的。
        /// </summary>
        public static readonly List<string> VisualComponents = new()
        {
            "Renderer",
            "ParticleSystem",
            "AudioSource",
            "Light",
            "Cloth"
        };

        /// <summary>自動分層時的預設字元預算。約 5k tokens —— 一次讀進來還算划算的量。</summary>
        public const int DefaultCharBudget = 20000;

        /// <param name="assetPath">prefab asset path</param>
        /// <param name="subPath">子樹相對 root 的路徑；留空 = 整棵。找不到時列出該層子節點</param>
        /// <param name="depth">明確指定往下幾層；-1 = 交給 charBudget 決定</param>
        /// <param name="fullExpand">不摺疊已知子樹（StateFolder / VariableFolder …）、不排除視覺 component</param>
        /// <param name="charBudget">輸出上限；超標就自動加深摺疊。0 = 不限</param>
        /// <param name="includeFsm">附上 FSM markdown 段（states / transitions / conditions）</param>
        /// <param name="fsmOnly">只輸出 FSM，不重複 hierarchy</param>
        /// <param name="structureOnly">只輸出 hierarchy 結構，不輸出 component 欄位或 FSM</param>
        public static string Export(
            string assetPath, string subPath = null, int depth = -1,
            bool fullExpand = true, int charBudget = DefaultCharBudget, bool includeFsm = false,
            bool fsmOnly = false, bool structureOnly = false)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
                return HardCap($"# 找不到 prefab: {assetPath}", charBudget);
            if (fsmOnly && structureOnly)
                return HardCap("# fsmOnly 與 structureOnly 不能同時開啟\n", charBudget);

            var root = asset.transform;
            EditResolve.DrainNotes(); // 清掉別的呼叫留下的殘留，只報這次的
            string resolveNotes = null;
            if (!string.IsNullOrEmpty(subPath))
            {
                var found = EditResolve.TryNode(root, subPath);
                if (found == null) return HardCap(DescribeChildren(root, subPath), charBudget);
                root = found;
                // 走了自動命名容錯的話要講出來，不然使用者手上那條路徑會一直是舊的
                resolveNotes = EditResolve.DrainNotes();
            }

            // 不印 prefab 路徑 —— HierarchyTextExporter 自己會印一行，兩邊都印就重複了
            var header = new StringBuilder();
            if (!string.IsNullOrEmpty(subPath)) header.AppendLine($"# subtree: {subPath}");
            if (resolveNotes != null) header.AppendLine(resolveNotes);

            return ExportResolvedNode(root.gameObject, depth, fullExpand, charBudget, header,
                includeFsm, fsmOnly, structureOnly);
        }

        /// <summary>
        /// 匯出任一顆已經在手上的 GameObject（scene 上的、prefab asset 裡的都行），
        /// 沿用同一套 charBudget 自動分層。給 EditGid 這種「先靠別的方式定位到物件」的入口用。
        /// </summary>
        /// <param name="header">分層的說明會 append 進來，由呼叫端決定印在哪</param>
        public static string ExportNode(
            GameObject root, int depth = -1, bool fullExpand = true,
            int charBudget = DefaultCharBudget, StringBuilder header = null,
            bool includeFsm = false, bool fsmOnly = false, bool structureOnly = false)
        {
            if (root == null) return "";
            if (fsmOnly && structureOnly)
                return HardCap("# fsmOnly 與 structureOnly 不能同時開啟\n", charBudget);

            var prefix = header ?? new StringBuilder();
            var result = ExportResolvedNode(root, depth, fullExpand, charBudget, prefix,
                includeFsm, fsmOnly, structureOnly);

            // 舊 API 的契約是「header 由呼叫端印、這裡只回 body」。保留這個行為；
            // 新的內部入口 ExportResolvedNode 才回完整且計入 header 的 hard-capped 結果。
            var prefixText = prefix.ToString();
            if (prefixText.Length == 0) return result;
            if (result.StartsWith(prefixText))
            {
                var offset = prefixText.Length;
                if (offset < result.Length && result[offset] == '\n') offset++;
                return result.Substring(offset);
            }

            return result;
        }

        /// <summary>
        /// 已定位 GameObject 的完整輸出入口。header、hierarchy、FSM 與截斷提示全都計入
        /// charBudget；給 prefab / scene / GlobalObjectId 三條讀取路徑共用。
        /// </summary>
        internal static string ExportResolvedNode(
            GameObject root, int depth, bool fullExpand, int charBudget, StringBuilder header,
            bool includeFsm = false, bool fsmOnly = false, bool structureOnly = false)
        {
            if (root == null) return "";
            if (fsmOnly && structureOnly)
                return HardCap("# fsmOnly 與 structureOnly 不能同時開啟\n", charBudget);

            var wantStructure = !fsmOnly;
            var wantFsm = fsmOnly || (includeFsm && !structureOnly);
            var fsm = wantFsm ? FsmTextExporter.Export(root) : null;
            if (!fsmOnly && (string.IsNullOrEmpty(fsm) || fsm.StartsWith("# (no FSM found")))
                fsm = null;

            string hierarchy = null;
            if (wantStructure)
            {
                hierarchy = depth < 0 && charBudget > 0
                    ? Layered(root, fullExpand, structureOnly, charBudget, header, fsm)
                    : Once(root, fullExpand, depth, structureOnly);
            }

            var text = Compose(header, hierarchy, fsm);
            return HardCap(text, charBudget);
        }

        private static string Once(
            GameObject root, bool fullExpand, int depth, bool structureOnly)
        {
            var options = Options(fullExpand, structureOnly);
            options._maxDepth = depth;
            return HierarchyTextExporter.Export(root, options);
        }

        /// <summary>
        /// 由淺往深試，取「塞得進預算的最深一層」。
        ///
        /// 為什麼是由淺往深而不是先全展開再退：全展開一份 PPlayer 是 120KB 字串，
        /// 而淺層那幾次都很便宜，通常第 3～5 次就命中。
        /// </summary>
        private static string Layered(
            GameObject root, bool fullExpand, bool structureOnly, int charBudget,
            StringBuilder header, string fsm)
        {
            const int maxProbe = 40;
            var options = Options(fullExpand, structureOnly);

            string best = null;
            var bestDepth = 0;
            for (var d = 0; d <= maxProbe; d++)
            {
                options._maxDepth = d;
                var text = HierarchyTextExporter.Export(root, options);

                // 加深了但輸出沒變 = 已經到底，不用再試。
                if (best != null && text.Length == best.Length)
                {
                    var note = $"# 全展開 {text.Length} 字元（在 charBudget {charBudget} 內）";
                    if (Fits(header, note, text, fsm, charBudget)) header.AppendLine(note);
                    return text;
                }

                if (!Fits(header, null, text, fsm, charBudget))
                {
                    if (best == null)
                    {
                        header.AppendLine($"# 最淺結構仍超過 charBudget {charBudget}，輸出已截斷");
                        return text;
                    }

                    var note = $"# 依 charBudget {charBudget} 摺到第 {bestDepth} 層" +
                               $"（下一層完整輸出會到 {Compose(header, text, fsm).Length} 字元）。" +
                               "折疊行的 (+N nodes) 是展開成本，要細節用 --node 指定子樹下鑽。";
                    if (Fits(header, note, best, fsm, charBudget)) header.AppendLine(note);
                    else header.AppendLine($"# charBudget {charBudget}：摺到第 {bestDepth} 層");
                    return best;
                }

                best = text;
                bestDepth = d;
            }

            var stopped = $"# 探到第 {maxProbe} 層就停了（結構異常地深）";
            if (Fits(header, stopped, best, fsm, charBudget)) header.AppendLine(stopped);
            return best;
        }

        private static bool Fits(
            StringBuilder header, string note, string hierarchy, string fsm, int charBudget)
        {
            if (charBudget <= 0) return true;
            var probeHeader = new StringBuilder(header.ToString());
            if (!string.IsNullOrEmpty(note)) probeHeader.AppendLine(note);
            return Compose(probeHeader, hierarchy, fsm).Length <= charBudget;
        }

        private static string Compose(StringBuilder header, string hierarchy, string fsm)
        {
            var sb = new StringBuilder();
            if (header != null && header.Length > 0)
            {
                sb.Append(header);
                if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(hierarchy))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(hierarchy);
            }

            if (!string.IsNullOrEmpty(fsm))
            {
                if (!string.IsNullOrEmpty(hierarchy))
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
                else if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.Append(fsm);
            }

            return sb.ToString();
        }

        /// <summary>最終輸出的 hard cap。budget=0 表示不限；截斷提示本身也算在預算內。</summary>
        internal static string HardCap(string text, int charBudget)
        {
            text ??= "";
            if (charBudget <= 0 || text.Length <= charBudget) return text;
            if (charBudget == 1) return "…";

            var hint = $"\n# … 截斷（charBudget {charBudget}）\n";
            if (hint.Length >= charBudget)
                return ("#…" + new string('.', charBudget)).Substring(0, charBudget);
            return text.Substring(0, charBudget - hint.Length) + hint;
        }

        private static HierarchyExportOptions Options(bool fullExpand, bool structureOnly)
        {
            var options = fullExpand
                ? HierarchyExportOptions.FullExpand
                : HierarchyExportOptions.Default;
            if (!fullExpand) options._excludeComponents.AddRange(VisualComponents);
            // includeComponents 非空時只允許匹配型別；這個 sentinel 不可能是 Component 型別，
            // 因而只保留節點、Transform、inactive/prefab flags 與 note。
            if (structureOnly) options._includeComponents.Add("__uprefab_structure_only__");
            return options;
        }

        // 路徑打錯時，把該層實際有的子節點列出來，省一次來回
        private static string DescribeChildren(Transform root, string subPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# 找不到子樹: {subPath}");

            // 沿著路徑往下走到最後一個走得通的節點
            var cursor = EditResolve.WalkAsFarAsPossible(root, subPath, out var walked);

            sb.AppendLine($"# 走到這裡為止: {(string.IsNullOrEmpty(walked) ? "(root)" : walked)}");
            sb.AppendLine("# 這層的子節點：");
            foreach (var label in EditResolve.ChildLabels(cursor))
                sb.AppendLine($"  {label}");
            return sb.ToString();
        }
    }
}
