using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// `up menu "&lt;menu path&gt;"` 的實作：從 CLI 觸發一個 Editor MenuItem（EditorApplication.ExecuteMenuItem）。
    /// 讓 agent / 外部 CLI 跟人按選單走同一條路，不用為每個 MenuItem 臨時寫 execute-dynamic-code。
    /// 找不到或被 validate 擋掉時回傳 "FAIL:" 開頭的字串，並列出最接近的幾個專案內 MenuItem path
    /// （只掃得到 [MenuItem] 宣告的，Unity 內建選單列不出來，但照樣可以執行）。
    /// </summary>
    public static class MenuEdit
    {
        private const int SuggestCount = 6;

        public static string Execute(string menuPath)
        {
            menuPath = (menuPath ?? "").Trim().Trim('/');
            if (menuPath.Length == 0)
                return "FAIL: menu path 是空的。例：up menu \"Tools/Meshy/包所有還沒包的 Prefab\"";

            if (EditorApplication.ExecuteMenuItem(menuPath))
                return $"OK 執行 {menuPath}";

            var all = CollectMenuPaths();
            var sb = new StringBuilder();
            sb.Append(all.Contains(menuPath)
                ? $"FAIL: {menuPath} 存在但沒有執行（validate 函式回 false，例如需要先選到某個 asset）"
                : $"FAIL: 找不到 menu item「{menuPath}」");
            var suggestions = Suggest(menuPath, all);
            if (suggestions.Count > 0)
            {
                sb.Append("\n# 你可能想要：");
                foreach (var s in suggestions) sb.Append("\n  up menu \"").Append(s).Append('"');
            }
            return sb.ToString();
        }

        /// <summary>專案 + package 裡所有 [MenuItem] 的路徑（去掉快捷鍵後綴、跳過 validate 函式）。</summary>
        private static HashSet<string> CollectMenuPaths()
        {
            var set = new HashSet<string>();
            foreach (var m in TypeCache.GetMethodsWithAttribute<MenuItem>())
            foreach (var attr in m.GetCustomAttributes<MenuItem>(false))
            {
                if (attr.validate || string.IsNullOrEmpty(attr.menuItem)) continue;
                set.Add(StripShortcut(attr.menuItem));
            }
            return set;
        }

        /// <summary>「Foo/Bar #_S」→「Foo/Bar」。快捷鍵是最後一個空白後、以 % # &amp; _ 開頭的 token。</summary>
        private static string StripShortcut(string path)
        {
            var i = path.LastIndexOf(' ');
            if (i < 0 || i == path.Length - 1) return path;
            var c = path[i + 1];
            return c is '%' or '#' or '&' or '_' ? path.Substring(0, i) : path;
        }

        private static List<string> Suggest(string query, HashSet<string> all)
        {
            var q = query.ToLowerInvariant();
            var slash = q.LastIndexOf('/');
            var leaf = slash >= 0 ? q.Substring(slash + 1) : q;
            var parent = slash >= 0 ? q.Substring(0, slash + 1) : null;
            var scored = new List<(int rank, int dist, string path)>();
            foreach (var p in all)
            {
                var lp = p.ToLowerInvariant();
                var candLeaf = LeafOf(lp);
                // 0 = 名字含查詢的最後一段；1 = 同一個父選單；2 = 查詢含候選的最後一段；3 = 其他，同 rank 比編輯距離
                var rank = lp.Contains(leaf) ? 0
                    : parent != null && lp.StartsWith(parent) ? 1
                    : candLeaf.Length > 0 && leaf.Contains(candLeaf) ? 2
                    : 3;
                scored.Add((rank, Levenshtein(q, lp), p));
            }
            scored.Sort((a, b) => a.rank != b.rank ? a.rank.CompareTo(b.rank) : a.dist.CompareTo(b.dist));
            var result = new List<string>();
            for (var i = 0; i < scored.Count && i < SuggestCount; i++) result.Add(scored[i].path);
            return result;
        }

        private static string LeafOf(string path)
        {
            var i = path.LastIndexOf('/');
            return i >= 0 ? path.Substring(i + 1) : path;
        }

        private static int Levenshtein(string a, string b)
        {
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++) prev[j] = j;
            for (var i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                (prev, cur) = (cur, prev);
            }
            return prev[b.Length];
        }
    }
}
