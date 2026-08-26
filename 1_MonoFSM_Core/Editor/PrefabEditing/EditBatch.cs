using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

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
    /// prefab|&lt;prefabPath&gt;|&lt;parent&gt;|&lt;name&gt;   放 prefab 實例（prefab / scene 皆可）
    /// comp|&lt;node&gt;|&lt;comp,comp&gt;               對既有節點加 component
    /// set|&lt;node&gt;|&lt;comp&gt;|&lt;field&gt;|&lt;value&gt;    設值
    /// ref|&lt;node&gt;|&lt;comp&gt;|&lt;field&gt;|&lt;target&gt;[|&lt;targetComp&gt;]  指向另一個節點
    /// aref|&lt;node&gt;|&lt;comp&gt;|&lt;field&gt;|&lt;assetPath&gt;              指向 asset
    /// addel|&lt;node&gt;|&lt;comp&gt;|&lt;field&gt;             陣列/List 尾端加一個元素（回傳 index）
    /// pos|&lt;node&gt;|x,y,z                     設 localPosition
/// scale|&lt;node&gt;|x,y,z                   設 localScale（僅 prefab）
/// rot|&lt;node&gt;|x,y,z                     設 localEulerAngles（僅 prefab）
    /// mv|&lt;node&gt;|&lt;newParent&gt;                 換 parent（僅 scene）
    /// del|&lt;node&gt;                            刪節點
    /// save                                  存檔（僅 scene；prefab 每次都自動存）
    ///
    /// # FSM 複合操作（一行取代三到四行原語，見 EditFsm）
    /// state|&lt;folder&gt;|&lt;name&gt;[|&lt;type&gt;]        建 `[State] name`（預設 GeneralState）
    /// trans|&lt;from&gt;|&lt;to&gt;[|&lt;name&gt;]           建 `[Transition] =&gt; to` 並接上 _target
    /// if|&lt;node&gt;|&lt;name&gt;|&lt;condType&gt;[|&lt;field&gt;|&lt;target&gt;]  建 `[If] name`，順手接一條引用
    /// act|&lt;state&gt;|&lt;phase&gt;|&lt;name&gt;|&lt;actionType&gt;         確保 `[Event] On…` 在，掛 `[Action] name`
    ///
    /// # 路徑代換
    /// mark|&lt;label&gt;[|&lt;node&gt;]                 給節點取名；不給 node 就標記上一個操作碰到的節點
    /// </code>
    ///
    /// **`$` 代換**：任何參數寫 `$` = 上一個操作碰到的節點，`$label` = `mark` 標過的節點，
    /// 後面可以再接 `/子路徑`。MonoFSM 的節點路徑很長（`[StateFolder] StateFolder/[State] idle/
    /// [Event] OnStateEnter/[Action] X`），而 `add` 完緊接著 `ref` 是最常見的組合 ——
    /// 少了代換，同一條長路徑要在相鄰兩行各寫一次。要寫字面 `$` 就打 `$$`。
    ///
    /// **第一個失敗就停**（回傳的那行以 `# 未修改` 開頭）—— 後面的操作通常依賴前面的結果，
    /// 硬跑下去只會產生一長串誤導性的錯誤。
    /// </summary>
    public static class EditBatch
    {
        internal delegate string Apply(string verb, string[] args);

        /// <summary>上一個操作碰到的節點路徑（`$` 代換的來源）。由各 verb 用 Touch() 回報。</summary>
        private static string _last;
        private static readonly Dictionary<string, string> Marks = new();

        /// <summary>verb 回報「我建立/操作的是這個節點」，讓下一行可以用 `$` 指回來。</summary>
        internal static void Touch(string nodePath) => _last = nodePath ?? "";

        internal static string Run(string ops, Apply apply) => Run(ops, apply, out _);

        /// <summary>
        /// 跟 <see cref="Run(string,Apply)"/> 相同，另外回報實際成功執行的操作數。
        /// PrefabEdit 的 quiet 模式用這個數字取代逐行成功 log；錯誤時仍保留完整逐行輸出。
        /// </summary>
        internal static string Run(string ops, Apply apply, out int done)
        {
            done = 0;
            if (string.IsNullOrWhiteSpace(ops)) return "# 沒有操作";

            _last = null;
            Marks.Clear();
            EditResolve.DrainNotes(); // 上一次跑剩的殘留（唯讀查詢路徑不會 drain）不要算到這次頭上

            var lines = ops.Replace("\r\n", "\n").Split('\n');
            var sb = new StringBuilder();
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                var parts = line.Split('|');
                var verb = parts[0].Trim().ToLowerInvariant();
                var args = new string[parts.Length - 1];

                string result;
                try
                {
                    for (var j = 1; j < parts.Length; j++) args[j - 1] = Expand(parts[j]);
                    // mark 只動代換表，不碰資料，所以在這裡處理 —— prefab / scene 兩邊都免費拿到
                    result = verb == "mark" ? Mark(args) : apply(verb, args);
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
                // 解析層的容錯提示（自動命名對應）要跟著那一行出現，不然看不出是哪個操作觸發的
                var notes = EditResolve.DrainNotes();
                if (notes != null) sb.AppendLine(notes);
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

        // `$`、`$label`、`$/子路徑`、`$label/子路徑`。`${...}` 這種不是識別字的（prompt 的
        // smart string token）不動，`$$` 是字面 `$` 的跳脫。
        private static readonly Regex RefRe = new(@"^\$([A-Za-z_][A-Za-z0-9_]*)?(/.*)?$");

        private static string Expand(string arg)
        {
            if (string.IsNullOrEmpty(arg) || arg[0] != '$') return arg;
            if (arg.StartsWith("$$")) return arg.Substring(1);

            var m = RefRe.Match(arg);
            if (!m.Success) return arg;

            var label = m.Groups[1].Value;
            string basePath;
            if (label.Length == 0)
            {
                if (_last == null)
                    throw new EditResolve.EditAbort("`$` 沒有可代換的節點（前面還沒有任何建立/操作節點的操作）");
                basePath = _last;
            }
            else if (!Marks.TryGetValue(label, out basePath))
            {
                throw new EditResolve.EditAbort(
                    $"`${label}` 還沒被 mark 過。已有的：{(Marks.Count == 0 ? "(無)" : string.Join(", ", Marks.Keys))}");
            }

            var rest = m.Groups[2].Value; // 含開頭的 '/'
            if (rest.Length == 0) return basePath;
            return basePath.Length == 0 ? rest.Substring(1) : basePath + rest;
        }

        private static string Mark(string[] args)
        {
            var label = Need(args, 0, "mark", "label");
            var path = At(args, 1);
            if (path == null)
            {
                if (_last == null)
                    throw new EditResolve.EditAbort("`mark` 沒有 node 參數時要接在一個建立/操作節點的操作後面");
                path = _last;
            }

            Marks[label] = path;
            return $"${label} = {EditResolve.Describe(path)}";
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

        /// <summary>true / false（大小寫不拘）。缺參數或打錯字都直接停，不要猜預設值。</summary>
        internal static bool Bool(string[] args, int i, string verb)
        {
            var raw = Need(args, i, verb, "true/false");
            if (bool.TryParse(raw, out var value))
                return value;
            throw new EditResolve.EditAbort($"`{verb}` 的第 {i + 1} 個參數要是 true 或 false，收到 '{raw}'");
        }

        /// <summary>整數。缺參數或打錯字都直接停，不要猜預設值。</summary>
        internal static int Int(string[] args, int i, string verb, string what)
        {
            var raw = Need(args, i, verb, what);
            if (int.TryParse(raw.Trim(), out var value))
                return value;
            throw new EditResolve.EditAbort($"`{verb}` 的 {what} 要是整數，收到 '{raw}'");
        }

        /// <summary>"x,y,z" → Vector3。三個分量都要有，少一個就停（別猜 0）。</summary>
        internal static Vector3 Vec3(string[] args, int i, string verb, string what)
        {
            var raw = Need(args, i, verb, $"{what} 的 x,y,z");
            var xyz = raw.Split(',');
            if (xyz.Length != 3)
                throw new EditResolve.EditAbort(
                    $"`{verb}` 的 {what} 要是 x,y,z 三個分量，收到 '{raw}'");

            var v = new float[3];
            for (var n = 0; n < 3; n++)
                if (!float.TryParse(xyz[n].Trim(), out v[n]))
                    throw new EditResolve.EditAbort(
                        $"`{verb}` 的 {what} 第 {n + 1} 個分量不是數字：'{xyz[n]}'");
            return new Vector3(v[0], v[1], v[2]);
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
