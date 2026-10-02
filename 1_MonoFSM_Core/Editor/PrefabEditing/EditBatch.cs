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
    /// rect|&lt;node&gt;|&lt;ax,ay&gt;|&lt;w,h&gt;|&lt;anchor&gt;|&lt;px,py&gt;  UI 的 anchoredPosition/sizeDelta/anchor/pivot（ApplyRect）
    /// mv|&lt;node&gt;|&lt;newParent&gt;                 換 parent（prefab / scene 皆可）
    /// dup|&lt;node&gt;|&lt;newName&gt;                 複製節點到同一個 parent、排在原節點後面（僅 scene）
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
    /// `asset do`（AssetEdit.Batch）用的是同一個 Run，但 asset 沒有節點概念，所以只吃
    /// `set|&lt;field&gt;|&lt;value&gt;`、`aref|&lt;field&gt;|&lt;assetPath&gt;`、`addel|&lt;field&gt;[|&lt;type&gt;]`
    /// —— 第一個參數直接是 fieldPath，少一層 node/comp。
    ///
    /// **`$` 代換**：任何參數寫 `$` = 上一個操作碰到的節點，`$label` = `mark` 標過的節點，
    /// 後面可以再接 `/子路徑`。MonoFSM 的節點路徑很長（`[StateFolder] StateFolder/[State] idle/
    /// [Event] OnStateEnter/[Action] X`），而 `add` 完緊接著 `ref` 是最常見的組合 ——
    /// 少了代換，同一條長路徑要在相鄰兩行各寫一次。要寫字面 `$` 就打 `$$`（參數任何位置都算，
    /// 包括 `$label/` 後面的子路徑）；單一 `$` 後面不是識別字（`$[Var]`、`${token}`）照原樣保留。
    ///
    /// **第一個失敗就停**（回傳的那行以 `# 未修改` 開頭）—— 後面的操作通常依賴前面的結果，
    /// 硬跑下去只會產生一長串誤導性的錯誤。
    /// </summary>
    public static class EditBatch
    {
        internal delegate string Apply(string verb, string[] args);

        /// <summary>
        /// 路徑 ⇄ 節點的換算，由呼叫端提供（prefab 是單 root、scene 是多 root）。
        /// 有它，`mark` / `$` 存的就是節點本身（Transform），用到時才算「現在的」路徑 ——
        /// 同一批先 mark 再 rename / mv，`$label` 照樣指到同一顆。
        /// 2026-09-30 發電鴿：mark `[State] idle` → rename 成 `待機 Waiting` → `trans|$S_WAIT…`
        /// 舊版存的是路徑，解析時被同層容錯對到 `[State] init`，transition 靜默建到錯的 state。
        /// asset batch 沒有節點，傳 null → 退回存字串（舊行為）。
        /// </summary>
        internal interface INodeSpace
        {
            /// <summary>精確解析（不走同層容錯）。找不到回 null，容錯猜得到的話放進 suggestion。</summary>
            Transform Resolve(string path, out string suggestion);

            /// <summary>節點現在的路徑（同名 sibling 補 `[n]`），餵回 Resolve 會解到同一顆。不在範圍內回 null。</summary>
            string PathOf(Transform node);
        }

        internal sealed class PrefabSpace : INodeSpace
        {
            private readonly Transform _root;
            internal PrefabSpace(Transform root) => _root = root;

            public Transform Resolve(string path, out string suggestion) =>
                EditResolve.TryNodeExact(_root, path, false, out suggestion);

            public string PathOf(Transform node) => EditResolve.PathOf(_root, node);
        }

        private sealed class MarkEntry
        {
            internal Transform Node; // 有 INodeSpace 時用這個
            internal string Path;    // mark 當下的路徑（無 INodeSpace 時就是代換值；有的話只拿來寫錯誤訊息）
        }

        /// <summary>上一個操作碰到的節點路徑（`$` 代換的來源）。由各 verb 用 Touch() 回報。</summary>
        private static string _last;
        /// <summary>_last 在那個操作跑完當下解成的節點（有 INodeSpace 時）。之後 rename / mv 也跟得上。</summary>
        private static Transform _lastNode;
        private static bool _lastFresh;
        private static INodeSpace _space;
        private static readonly Dictionary<string, MarkEntry> Marks = new();

        /// <summary>verb 回報「我建立/操作的是這個節點」，讓下一行可以用 `$` 指回來。</summary>
        internal static void Touch(string nodePath)
        {
            _last = nodePath ?? "";
            _lastNode = null;
            _lastFresh = true;
        }

        internal static string Run(string ops, Apply apply) => Run(ops, apply, out _);

        /// <summary>
        /// 跟 <see cref="Run(string,Apply)"/> 相同，另外回報實際成功執行的操作數。
        /// PrefabEdit 的 quiet 模式用這個數字取代逐行成功 log；錯誤時仍保留完整逐行輸出。
        /// </summary>
        /// <summary>
        ///     切欄位，但 `\|` 當成字面上的 `|`。
        ///     為什麼要：自動命名會生出含 `|` 的節點名（DistanceValueSource 的 Description 是
        ///     `|a - b|`、FloatMathValueSource 也會帶進去），不逃逸的話那些節點在 ops 裡永遠指不到
        ///     —— 而且失敗方式很難查（路徑被切斷，看起來像「這層明明就有」）。
        ///     只解 `\|` 一種，其餘反斜線（路徑的 `\/`、`\\n`）原樣留給下游的路徑解析。
        /// </summary>
        private static string[] SplitFields(string line)
        {
            if (line.IndexOf('|') < 0) return new[] { line };

            var parts = new List<string>();
            var cur = new StringBuilder();
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '\\' && i + 1 < line.Length && line[i + 1] == '|')
                {
                    cur.Append('|');
                    i++;
                    continue;
                }

                if (c == '|')
                {
                    parts.Add(cur.ToString());
                    cur.Clear();
                    continue;
                }

                cur.Append(c);
            }

            parts.Add(cur.ToString());
            return parts.ToArray();
        }

        internal static string Run(string ops, Apply apply, out int done) => Run(ops, apply, out done, null);

        /// <param name="space">路徑 ⇄ 節點換算；null = asset batch，mark 退回存字串</param>
        internal static string Run(string ops, Apply apply, out int done, INodeSpace space)
        {
            done = 0;
            if (string.IsNullOrWhiteSpace(ops)) return "# 沒有操作";

            _last = null;
            _lastNode = null;
            _lastFresh = false;
            _space = space;
            Marks.Clear();
            try
            {
                return RunLines(ops, apply, ref done);
            }
            finally
            {
                // 不要讓 static 欄位握著 LoadPrefabContents 的物件跨批次活著
                _space = null;
                _lastNode = null;
                Marks.Clear();
            }
        }

        private static string RunLines(string ops, Apply apply, ref int done)
        {
            EditResolve.DrainNotes(); // 上一次跑剩的殘留（唯讀查詢路徑不會 drain）不要算到這次頭上

            var lines = ops.Replace("\r\n", "\n").Split('\n');
            var sb = new StringBuilder();
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                var parts = SplitFields(line);
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
                    // 有 `$` / `$label` / `$$` 被代換過的話，印出實際送出去的參數 —— 不然「找不到節點」
                    // 只看得到原始寫法，看不出是代換（或跳脫）把路徑變成別的東西
                    var expandedNote = DescribeExpanded(parts, args);
                    if (expandedNote != null) sb.AppendLine(expandedNote);
                    // 刻意不說「前面已生效」—— prefab / asset 的批次是全成功才落地
                    // （PrefabEdit.Batch 不存檔、AssetEdit.Batch 不 Apply），只有 scene
                    // 是直接改在開著的場景上。落地與否由呼叫端在下一行講。
                    sb.AppendLine(
                        $"# 停在第 {i + 1} 行（`{line}`），前面 {done} 個操作執行成功、" +
                        "後面的都沒跑（是否落地看下一行）。修好這行再重跑剩下的部分。");
                    return sb.ToString();
                }

                done++;
                // `$` 也存節點：趁這個操作剛跑完、路徑一定對的時候解一次
                if (_lastFresh && _space != null && !string.IsNullOrEmpty(_last))
                {
                    _lastNode = _space.Resolve(_last, out _);
                    EditResolve.DrainNotes();
                }

                _lastFresh = false;
            }

            return sb.ToString();
        }

        // `$`、`$label`、`$/子路徑`、`$label/子路徑`。`${...}` / `$[` 這種接非識別字的單一 `$`
        // （prompt 的 smart string token、節點名裡的 `$[Var]`）原樣保留。
        // `$$` 在參數**任何位置**都是字面 `$` 的跳脫 —— 以前只在參數開頭處理，`$BR/…/Set $$[Var] x`
        // 的子路徑段會原樣帶著 `$$` 去找節點，回「找不到節點」卻看不出是跳脫沒生效（2026-10-01）。
        // 只解開使用者寫的部分（rest），basePath 是 `_last` / mark 存的真實節點路徑，不能再解一次。
        private static readonly Regex RefRe = new(@"^\$([A-Za-z_][A-Za-z0-9_]*)?(/.*)?$");

        private static string Unescape(string s) => s.IndexOf("$$", StringComparison.Ordinal) < 0 ? s : s.Replace("$$", "$");

        private static string Expand(string arg)
        {
            if (string.IsNullOrEmpty(arg)) return arg;
            if (arg[0] != '$' || arg.StartsWith("$$")) return Unescape(arg);

            var m = RefRe.Match(arg);
            if (!m.Success) return Unescape(arg);

            var label = m.Groups[1].Value;
            string basePath;
            if (label.Length == 0)
            {
                if (_last == null)
                    throw new EditResolve.EditAbort("`$` 沒有可代換的節點（前面還沒有任何建立/操作節點的操作）");
                // 解得到節點就用它現在的路徑（中間被 rename / mv 也對）；解不到退回當時記的字串
                basePath = _lastNode != null ? NodePath("$", _lastNode, _last) : _last;
            }
            else if (!Marks.TryGetValue(label, out var entry))
            {
                throw new EditResolve.EditAbort(
                    $"`${label}` 還沒被 mark 過。已有的：{(Marks.Count == 0 ? "(無)" : string.Join(", ", Marks.Keys))}");
            }
            else if (_space == null)
            {
                basePath = entry.Path;
            }
            else
            {
                // Unity 的 == 會把 Destroy 掉的物件判成 null
                if (entry.Node == null)
                    throw new EditResolve.EditAbort(
                        $"`${label}` 標的節點已經被前面的操作刪掉了（mark 時是 {EditResolve.Describe(entry.Path)}）");
                basePath = NodePath($"${label}", entry.Node, entry.Path);
            }

            var rest = Unescape(m.Groups[2].Value); // 含開頭的 '/'
            if (rest.Length == 0) return basePath;
            return basePath.Length == 0 ? rest.Substring(1) : basePath + rest;
        }

        /// <summary>
        /// 失敗行的除錯提示：列出被 `$` 代換 / `$$` 跳脫改過的參數（原始 → 實際）。都沒改過回 null。
        /// args 裡還是 null 的格子代表 Expand 自己丟了例外（例如 `$label` 沒 mark），那格不列。
        /// </summary>
        private static string DescribeExpanded(string[] parts, string[] args)
        {
            StringBuilder sb = null;
            for (var j = 1; j < parts.Length; j++)
            {
                var actual = args[j - 1];
                if (actual == null || actual == parts[j]) continue;
                sb ??= new StringBuilder("# 提示：這行有 `$` 代換 / `$$` 跳脫，實際送出的參數是：");
                sb.Append($"\n#   參數{j}：`{parts[j]}` → `{actual}`");
            }

            return sb?.ToString();
        }

        /// <summary>節點現在的路徑；跟 mark 當時不同（中間被 rename / mv）就留一行 note，不然看不出 `$label` 換了字面。</summary>
        private static string NodePath(string token, Transform node, string markedPath)
        {
            var now = _space.PathOf(node);
            if (now == null)
                throw new EditResolve.EditAbort(
                    $"`{token}` 標的節點 '{node.name}' 已經不在這個 prefab / scene 裡（mark 時是 {EditResolve.Describe(markedPath)}）");
            if (now != markedPath)
                EditResolve.Note($"`{token}` 跟著節點走：{EditResolve.Describe(markedPath)} → {EditResolve.Describe(now)}");
            return now;
        }

        private static string Mark(string[] args)
        {
            var label = Need(args, 0, "mark", "label");
            var path = At(args, 1);
            if (path == null)
            {
                if (_last == null)
                    throw new EditResolve.EditAbort("`mark` 沒有 node 參數時要接在一個建立/操作節點的操作後面");
                // `$` 已經解成節點的話直接拿節點，不再從字串重找
                if (_space != null && _lastNode != null)
                {
                    var lastNow = NodePath("$", _lastNode, _last);
                    Marks[label] = new MarkEntry { Node = _lastNode, Path = lastNow };
                    return $"${label} = {EditResolve.Describe(lastNow)}";
                }

                path = _last;
            }

            if (_space == null)
            {
                Marks[label] = new MarkEntry { Path = path };
                return $"${label} = {EditResolve.Describe(path)}";
            }

            // mark 當下就把節點抓住，而且不走同層容錯：標錯一顆，之後每個 `$label` 都跟著錯
            var node = _space.Resolve(path, out var suggestion);
            if (node == null)
                throw new EditResolve.EditAbort(
                    $"`mark` 找不到節點 {EditResolve.Describe(path)}（mark 不走同層容錯）" +
                    (suggestion != null ? $"。你可能想要：{suggestion}" : ""));
            var exact = _space.PathOf(node) ?? path;
            Marks[label] = new MarkEntry { Node = node, Path = exact };
            return $"${label} = {EditResolve.Describe(exact)}";
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

        /// <summary>"x,y" → Vector2。兩個分量都要有（UI 的 anchoredPosition / sizeDelta / pivot）。</summary>
        internal static Vector2 Vec2(string[] args, int i, string verb, string what)
        {
            var raw = Need(args, i, verb, $"{what} 的 x,y");
            var xy = raw.Split(',');
            if (xy.Length != 2)
                throw new EditResolve.EditAbort(
                    $"`{verb}` 的 {what} 要是 x,y 兩個分量，收到 '{raw}'");

            var v = new float[2];
            for (var n = 0; n < 2; n++)
                if (!float.TryParse(xy[n].Trim(), out v[n]))
                    throw new EditResolve.EditAbort(
                        $"`{verb}` 的 {what} 第 {n + 1} 個分量不是數字：'{xy[n]}'");
            return new Vector2(v[0], v[1]);
        }

        /// <summary>
        /// anchorMin/anchorMax。吃 Inspector 上那組 preset 名字（`center` / `top-left` /
        /// `stretch` / `stretch-bottom`…），或直接寫 `minX,minY,maxX,maxY` 四個數字。
        /// 之所以要 preset：手算 anchor 是 UI 改動最容易寫錯的一步，而名字對得上 Unity 的 UI。
        /// </summary>
        internal static (Vector2 min, Vector2 max) AnchorPreset(string spec, string verb)
        {
            var raw = (spec ?? "").Trim();
            var parts = raw.Split(',');
            if (parts.Length == 4)
            {
                var v = new float[4];
                for (var n = 0; n < 4; n++)
                    if (!float.TryParse(parts[n].Trim(), out v[n]))
                        throw new EditResolve.EditAbort(
                            $"`{verb}` 的 anchor 第 {n + 1} 個分量不是數字：'{parts[n]}'");
                return (new Vector2(v[0], v[1]), new Vector2(v[2], v[3]));
            }

            var key = raw.ToLowerInvariant().Replace(" ", "-").Replace("_", "-");
            switch (key)
            {
                case "bottom-left": return (Vector2.zero, Vector2.zero);
                case "bottom-center": case "bottom":
                    return (new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
                case "bottom-right": return (new Vector2(1f, 0f), new Vector2(1f, 0f));
                case "middle-left": case "left":
                    return (new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
                case "center": case "middle": case "middle-center":
                    return (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                case "middle-right": case "right":
                    return (new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                case "top-left": return (new Vector2(0f, 1f), new Vector2(0f, 1f));
                case "top-center": case "top":
                    return (new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
                case "top-right": return (Vector2.one, Vector2.one);
                case "stretch": case "stretch-all": case "full":
                    return (Vector2.zero, Vector2.one);
                case "stretch-top": return (new Vector2(0f, 1f), Vector2.one);
                case "stretch-bottom": return (Vector2.zero, new Vector2(1f, 0f));
                case "stretch-left": return (Vector2.zero, new Vector2(0f, 1f));
                case "stretch-right": return (new Vector2(1f, 0f), Vector2.one);
                case "stretch-h": case "stretch-horizontal":
                    return (new Vector2(0f, 0.5f), new Vector2(1f, 0.5f));
                case "stretch-v": case "stretch-vertical":
                    return (new Vector2(0.5f, 0f), new Vector2(0.5f, 1f));
                default:
                    throw new EditResolve.EditAbort(
                        $"`{verb}` 不認得 anchor preset '{spec}'。可用的：bottom-left bottom-center " +
                        "bottom-right middle-left center middle-right top-left top-center top-right " +
                        "stretch stretch-top stretch-bottom stretch-left stretch-right stretch-h " +
                        "stretch-v，或直接寫 minX,minY,maxX,maxY");
            }
        }

        /// <summary>
        /// `rect|&lt;node&gt;|&lt;ax,ay&gt;|&lt;w,h&gt;|&lt;anchor&gt;|&lt;px,py&gt;` 的共用本體（prefab / scene 兩邊都叫這支）。
        /// UI 節點的 localPosition 是 Canvas 佈局的**輸出**，anchoredPosition 才是輸入，所以 UI
        /// 位置一律走這裡。args[1..4] 各自留空 = 不動那一項。
        /// 只負責寫值：prefab 端要的 override 記錄 / 存檔驗證、scene 端的 dirty 由呼叫端各自處理，
        /// 所以把實際寫到的 serialized property 名丟進 <paramref name="touchedProps"/> 回報。
        /// </summary>
        internal static string ApplyRect(
            Transform node, string desc, string[] args, string verb, List<string> touchedProps)
        {
            if (!(node is RectTransform rect))
                throw new EditResolve.EditAbort(
                    $"'{desc}' 上是 {node.GetType().Name} 不是 RectTransform，`rect` 不適用；" +
                    "非 UI 節點請用 pos（prefab 另有 scale / rot）");

            var changed = new List<string>(4);
            if (!string.IsNullOrEmpty(At(args, 1)))
            {
                rect.anchoredPosition = Vec2(args, 1, verb, "anchoredPosition");
                touchedProps?.Add("m_AnchoredPosition");
                changed.Add($"anchoredPosition={rect.anchoredPosition}");
            }

            if (!string.IsNullOrEmpty(At(args, 2)))
            {
                rect.sizeDelta = Vec2(args, 2, verb, "sizeDelta");
                touchedProps?.Add("m_SizeDelta");
                changed.Add($"sizeDelta={rect.sizeDelta}");
            }

            var anchorSpec = At(args, 3);
            if (!string.IsNullOrEmpty(anchorSpec))
            {
                var (min, max) = AnchorPreset(anchorSpec, verb);
                rect.anchorMin = min;
                rect.anchorMax = max;
                touchedProps?.Add("m_AnchorMin");
                touchedProps?.Add("m_AnchorMax");
                changed.Add($"anchor={min}..{max}");
            }

            if (!string.IsNullOrEmpty(At(args, 4)))
            {
                rect.pivot = Vec2(args, 4, verb, "pivot");
                touchedProps?.Add("m_Pivot");
                changed.Add($"pivot={rect.pivot}");
            }

            if (changed.Count == 0)
                throw new EditResolve.EditAbort(
                    "`rect` 至少要給一項：rect|<node>|<anchoredX,Y>|<sizeW,H>|" +
                    "<anchor preset 或 minX,minY,maxX,maxY>|<pivotX,Y>");
            return $"{desc}.RectTransform " + string.Join(" ", changed);
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
