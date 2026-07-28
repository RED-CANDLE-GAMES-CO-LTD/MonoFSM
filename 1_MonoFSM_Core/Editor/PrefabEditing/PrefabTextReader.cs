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
        public static string Export(
            string assetPath, string subPath = null, int depth = -1,
            bool fullExpand = true, int charBudget = DefaultCharBudget, bool includeFsm = false)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null) return $"# 找不到 prefab: {assetPath}";

            var root = asset.transform;
            if (!string.IsNullOrEmpty(subPath))
            {
                var found = root.Find(subPath);
                if (found == null) return DescribeChildren(root, subPath);
                root = found;
            }

            // 不印 prefab 路徑 —— HierarchyTextExporter 自己會印一行，兩邊都印就重複了
            var header = new StringBuilder();
            if (!string.IsNullOrEmpty(subPath)) header.AppendLine($"# subtree: {subPath}");

            var body = depth < 0 && charBudget > 0
                ? Layered(root.gameObject, fullExpand, charBudget, header)
                : Once(root.gameObject, fullExpand, depth);

            var sb = new StringBuilder();
            sb.Append(header);
            sb.AppendLine();
            sb.Append(body);

            if (includeFsm)
            {
                var fsm = FsmTextExporter.Export(root.gameObject);
                if (!string.IsNullOrEmpty(fsm) && !fsm.StartsWith("# (no FSM found"))
                {
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                    sb.Append(fsm);
                }
            }

            return sb.ToString();
        }

        private static string Once(GameObject root, bool fullExpand, int depth)
        {
            var options = Options(fullExpand);
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
            GameObject root, bool fullExpand, int charBudget, StringBuilder header)
        {
            const int maxProbe = 40;
            var options = Options(fullExpand);

            string best = null;
            var bestDepth = 0;
            for (var d = 1; d <= maxProbe; d++)
            {
                options._maxDepth = d;
                var text = HierarchyTextExporter.Export(root, options);

                if (text.Length > charBudget && best != null)
                {
                    header.AppendLine(
                        $"# 依 charBudget {charBudget} 摺到第 {bestDepth} 層" +
                        $"（下一層會到 {text.Length} 字元）。折疊行的 (+N nodes) 是展開成本，" +
                        "要細節用 --node 指定子樹下鑽。");
                    return best;
                }

                // 加深了但輸出沒變 = 已經到底，不用再試
                if (best != null && text.Length == best.Length)
                {
                    header.AppendLine($"# 全展開 {text.Length} 字元（在 charBudget {charBudget} 內）");
                    return text;
                }

                best = text;
                bestDepth = d;
            }

            header.AppendLine($"# 探到第 {maxProbe} 層就停了（結構異常地深）");
            return best;
        }

        private static HierarchyExportOptions Options(bool fullExpand)
        {
            var options = fullExpand
                ? HierarchyExportOptions.FullExpand
                : HierarchyExportOptions.Default;
            if (!fullExpand) options._excludeComponents.AddRange(VisualComponents);
            return options;
        }

        // 路徑打錯時，把該層實際有的子節點列出來，省一次來回
        private static string DescribeChildren(Transform root, string subPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# 找不到子樹: {subPath}");

            // 沿著路徑往下走到最後一個走得通的節點
            var cursor = root;
            var walked = "";
            foreach (var seg in subPath.Split('/'))
            {
                var next = cursor.Find(seg);
                if (next == null) break;
                cursor = next;
                walked = string.IsNullOrEmpty(walked) ? seg : $"{walked}/{seg}";
            }

            sb.AppendLine($"# 走到這裡為止: {(string.IsNullOrEmpty(walked) ? "(root)" : walked)}");
            sb.AppendLine("# 這層的子節點：");
            foreach (Transform child in cursor)
                sb.AppendLine($"  {child.name}  (+{CountDescendants(child)} nodes)");
            return sb.ToString();
        }

        private static int CountDescendants(Transform t)
        {
            var n = 0;
            foreach (Transform c in t) n += 1 + CountDescendants(c);
            return n;
        }
    }
}
