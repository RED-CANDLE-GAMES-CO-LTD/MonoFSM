using System;
using System.Collections.Generic;
using System.Text;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 一行一個操作的迷你 DSL，讓「建 20 個節點 + 接 30 條引用」變成一次呼叫。
    ///
    /// 為什麼需要它：每次 uloop execute-dynamic-code 來回都要付一整份 JSON envelope 的
    /// context 成本。建一個 FSM 動輒幾十個原語，逐次呼叫的雜訊會比實際內容多一個數量級。
    ///
    /// 欄位分隔用 `|` 而不是空白 —— MonoFSM 的節點名慣例帶空白與 `[Tag]` 前綴
    /// （`[State] Player Idle`），中文名稱也很常見，空白分隔一定會炸。
    ///
    /// 語法（`#` 開頭是註解，空行忽略）：
    /// <code>
    /// add|&lt;parent&gt;|&lt;name&gt;|&lt;comp,comp&gt;      建節點並掛 component
    /// prefab|&lt;prefabPath&gt;|&lt;parent&gt;|&lt;name&gt;   放 prefab 實例（僅 scene）
    /// comp|&lt;node&gt;|&lt;comp,comp&gt;               對既有節點加 component
    /// set|&lt;node&gt;|&lt;comp&gt;|&lt;field&gt;|&lt;value&gt;    設值
    /// ref|&lt;node&gt;|&lt;comp&gt;|&lt;field&gt;|&lt;target&gt;[|&lt;targetComp&gt;]  指向另一個節點
    /// aref|&lt;node&gt;|&lt;comp&gt;|&lt;field&gt;|&lt;assetPath&gt;              指向 asset
    /// addel|&lt;node&gt;|&lt;comp&gt;|&lt;field&gt;             陣列/List 尾端加一個元素（回傳 index）
    /// pos|&lt;node&gt;|x,y,z                     設 localPosition（僅 scene）
    /// mv|&lt;node&gt;|&lt;newParent&gt;                 換 parent（僅 scene）
    /// del|&lt;node&gt;                            刪節點
    /// save                                  存檔（僅 scene；prefab 每次都自動存）
    /// </code>
    ///
    /// **第一個失敗就停**（回傳的那行以 `# 未修改` 開頭）—— 後面的操作通常依賴前面的結果，
    /// 硬跑下去只會產生一長串誤導性的錯誤。
    /// </summary>
    public static class EditBatch
    {
        internal delegate string Apply(string verb, string[] args);

        internal static string Run(string ops, Apply apply)
        {
            if (string.IsNullOrWhiteSpace(ops)) return "# 沒有操作";

            var lines = ops.Replace("\r\n", "\n").Split('\n');
            var sb = new StringBuilder();
            var done = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                var parts = line.Split('|');
                var verb = parts[0].Trim().ToLowerInvariant();
                var args = new string[parts.Length - 1];
                for (var j = 1; j < parts.Length; j++) args[j - 1] = parts[j];

                string result;
                try
                {
                    result = apply(verb, args);
                }
                catch (EditResolve.EditAbort abort)
                {
                    result = $"# 未修改：{abort.Message}";
                }
                catch (Exception e)
                {
                    result = $"# 未修改：{e.GetType().Name}: {e.Message}";
                }

                sb.AppendLine($"{i + 1}: {result}");
                if (result.StartsWith("# 未修改"))
                {
                    sb.AppendLine(
                        $"# 停在第 {i + 1} 行（`{line}`），前面 {done} 個操作已生效，" +
                        "後面的都沒跑。修好這行再重跑剩下的部分。");
                    return sb.ToString();
                }

                done++;
            }

            return sb.ToString();
        }

        /// <summary>args[i] 取值，超出範圍或空字串就回 null（讓選填參數走預設）。</summary>
        internal static string At(string[] args, int i)
        {
            if (i >= args.Length) return null;
            var v = args[i];
            return string.IsNullOrEmpty(v) ? null : v;
        }

        /// <summary>逗號分隔的 component 型別清單。</summary>
        internal static string[] Types(string[] args, int i)
        {
            var raw = At(args, i);
            if (raw == null) return Array.Empty<string>();
            var list = new List<string>();
            foreach (var t in raw.Split(','))
                if (!string.IsNullOrWhiteSpace(t))
                    list.Add(t.Trim());
            return list.ToArray();
        }

        internal static string Need(string[] args, int i, string verb, string what)
        {
            var v = At(args, i);
            if (v == null)
                throw new EditResolve.EditAbort($"`{verb}` 缺第 {i + 1} 個參數（{what}）");
            return v;
        }
    }
}
