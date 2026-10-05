using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace MonoFSM.Editor.Profiling
{
    /// <summary>
    /// `uprofile` CLI 的參數。shell wrapper（.claude/scripts/uprofile）→ uloop custom tool（UProfileTool）→ 這裡。
    /// 欄位預設值 0 / 空字串代表「用子指令自己的預設」。
    /// </summary>
    public class UProfileArgs
    {
        public string Command = "status";
        public string Frames = "";
        public string Thread = "main";
        public string Sort = "self";
        public int Limit;
        public float Threshold = 33f;
        public int Depth = 4;
        public float MinMs = 0.1f;
        public string File = "";
        public string Save = "";
        public bool Clear;
        public bool Editor;
        public bool Deep;
        public string Owner = "";
        public bool Force;
        public int MaxChars = 4000;
    }

    /// <summary>
    /// 把 Unity Profiler buffer（或 .data 檔）壓成給 agent 讀的精簡文字表格。
    /// 只用公開 Editor API：ProfilerDriver + HierarchyFrameDataView / RawFrameDataView。
    /// 純 Editor 工具，不在 runtime 跑，GC 不是重點但也不刻意浪費。
    /// 設計與踩坑紀錄見同資料夾 Progress.md。
    /// </summary>
    public static class UProfileReader
    {
        public const string Usage =
            "用法: uprofile <子指令> [參數]\n" +
            "  status                                  profiler 開關 / buffer frame 範圍 / target / deep profile\n" +
            "  start [--clear] [--editor] [--deep]     開始錄（--clear 先清 buffer；--editor 暫時把 target 切 Editor；--deep 開 deep profile；stop 都會還原）\n" +
            "  stop [--save <path>]                    停止並存 .data（預設 Library/uprofile/<時間>.data）\n" +
            "  top [--frames A-B] [--thread main] [--sort self|total|gc|calls] [--limit 30]\n" +
            "  spikes [--frames A-B] [--threshold 33] [--limit 10]\n" +
            "  frame <n> [--thread main] [--depth 4] [--min-ms 0.1]\n" +
            "  gc [--frames A-B] [--thread main] [--limit 30]\n" +
            "共用: --file x.data 先讀檔再分析；--max-chars 4000 輸出上限\n" +
            "--frames: A-B / A / A-（A 到最後）/ -N（最後 N 個 frame）；--thread: main / render / thread 名稱子字串";

        const string EmptyMsg =
            "profiler 沒資料，先 `uprofile start`，跑一陣子再 `uprofile stop`，或用 `--file x.data`";

        const string GcAllocName = "GC.Alloc";
        // 狀態一律放 SessionState：static 欄位會被 domain reload 清掉，SessionState 撐得過（Editor 關掉才清）
        internal const string RecKey = "uprofile.recording";
        internal const string StartFrameKey = "uprofile.startFrame";
        internal const string RestoreTargetKey = "uprofile.restoreTargetPlayMode";
        internal const string ReloadCountKey = "uprofile.reloadCount";
        internal const string ReloadAtKey = "uprofile.reloadAtFrame";
        internal const string ReloadBeforeKey = "uprofile.reloadBefore";
        internal const string ReloadAfterKey = "uprofile.reloadAfter";
        internal const string DeepByUsKey = "uprofile.deepByUs";
        const string LastSaveDeepKey = "uprofile.lastSaveDeep";
        const string OwnerKey = "uprofile.owner";
        const string OwnerStartKey = "uprofile.ownerStartUtc";
        const double StaleMinutes = 10;
        const string LastSaveKey = "uprofile.lastSave";
        // 不放 Temp/：Unity 正常關閉時會把整個 Temp/ 刪掉（2026-10-03 Editor 重開後存的 .data 全沒了）。Library/ 會留著且已 gitignore
        static readonly string DefaultSaveDir = Path.Combine("Library", "uprofile");
        const string LastSaveRangeKey = "uprofile.lastSaveRange";

        /// <summary>ResolveThread 對到多條時的提示，Analyze 會印在 header 前面。</summary>
        static string MultiMatchNote;

        static string s_loadedPath;
        static DateTime s_loadedMtime;
        static int s_loadedFirst = -1, s_loadedLast = -1;

        public static string Run(UProfileArgs a, out bool ok)
        {
            ok = true;
            if (a == null) a = new UProfileArgs();
            if (a.MaxChars <= 0) a.MaxChars = 4000;
            var cmd = (a.Command ?? "").Trim().ToLowerInvariant();
            try
            {
                switch (cmd)
                {
                    case "status": return Status();
                    case "start": return StartRec(a, out ok);
                    case "stop": return StopRec(a, out ok);
                    case "top":
                    case "gc":
                    case "spikes":
                    case "frame":
                        return Analyze(cmd, a, out ok);
                    default:
                        ok = false;
                        return $"未知子指令 '{a.Command}'\n{Usage}";
                }
            }
            catch (Exception e)
            {
                ok = false;
                return $"uprofile 例外 {e.GetType().Name}: {e.Message}";
            }
        }

        // ───────────────────────── status / start / stop ─────────────────────────

        static string Status()
        {
            var sb = new StringBuilder();
            sb.Append("profiler: ").Append(ProfilerDriver.enabled ? "錄製中" : "停止")
                .Append(" | target: ").Append(ProfilerDriver.profileEditor ? "Editor" : "Play Mode")
                .Append(" | deep profile: ").Append(DeepDesc())
                .Append(" | play mode: ").Append(EditorApplication.isPlaying ? (EditorApplication.isPaused ? "paused" : "playing") : "no")
                .Append('\n');
            sb.Append("buffer: ").Append(BufferDesc());
            if (!string.IsNullOrEmpty(s_loadedPath)) sb.Append(" | 最後讀檔: ").Append(s_loadedPath);
            sb.Append('\n');
            var lastSave = SessionState.GetString(LastSaveKey, "");
            if (lastSave.Length > 0)
                sb.Append("最後存檔: ").Append(lastSave).Append("（frames ").Append(SessionState.GetString(LastSaveRangeKey, "?")).Append("）\n");
            if (OwnerActive())
                sb.Append("owner: ").Append(OwnerDesc()).Append('\n');
            if (SessionState.GetBool(RecKey, false))
            {
                int reloads = SessionState.GetInt(ReloadCountKey, 0);
                if (!ProfilerDriver.enabled)
                    sb.Append("warn: ").Append(InterruptedDesc()).Append("。`uprofile stop` 收尾（會存檔、還原 target）\n");
                else if (reloads > 0)
                    sb.Append($"note: 錄製期間有 {reloads} 次 domain reload，profiler 沒被停掉\n");
            }
            if (!ProfilerDriver.profileEditor && !EditorApplication.isPlaying)
                sb.Append("note: target 是 Play Mode 又沒在 play → Edit Mode 錄不到任何 frame。錄 Editor 用 `uprofile start --editor`\n");
            return sb.ToString();
        }

        static string DeepDesc()
        {
            if (!ProfilerDriver.deepProfiling) return SessionState.GetBool(DeepByUsKey, false) ? "off（uprofile 要開，等 domain reload）" : "off";
            return SessionState.GetBool(DeepByUsKey, false) ? "on（uprofile 開的，stop 會關）" : "on（不是 uprofile 開的，stop 不會動它）";
        }

        const string DeepMsNote = "note: deep profile 資料的 ms 會嚴重失真（每個 C# 呼叫都插樁），只拿來看 GC alloc 從哪來、呼叫路徑長怎樣；量時間請關 deep 重錄";

        static string BufferDesc()
        {
            int f = ProfilerDriver.firstFrameIndex, l = ProfilerDriver.lastFrameIndex;
            if (f < 0 || l < 0) return "空";
            return $"frames {f}-{l}（{l - f + 1} frames）";
        }

        static string StartRec(UProfileArgs a, out bool ok)
        {
            ok = true;
            var ownerErr = CheckOwner(a, out string takeoverWarn);
            if (ownerErr != null)
            {
                ok = false;
                return ownerErr;
            }

            if (ProfilerDriver.enabled)
            {
                if (takeoverWarn == null)
                    return $"已經在錄了（buffer {BufferDesc()}）。要結束用 `uprofile stop`";
                ClaimOwner(a);
                SessionState.SetBool(RecKey, true);
                return $"{takeoverWarn}\n接手錄製（沒有重開）| buffer {BufferDesc()}。要結束用 `uprofile stop`";
            }

            // Play Mode 中一律不切 target（要切就先停 Play Mode）；deep 的同類檢查在 PrepareDeep
            if (a.Editor && EditorApplication.isPlaying && !ProfilerDriver.profileEditor)
            {
                ok = false;
                return "Play Mode 中不切 Profiler target（會在別人 play 的時候把整個 Editor 插樁）。`--editor` 只在 Edit Mode 用；Play Mode 直接 `uprofile start` 錄遊戲";
            }

            if (a.Deep)
            {
                var deepMsg = PrepareDeep(a, out bool deepOk);
                if (deepMsg != null)
                {
                    ok = deepOk;
                    return deepMsg;
                }
            }

            // 實測：target = Play Mode 又沒在 play 時，ProfilerDriver.enabled = true 讀回來是 true，但一個 frame 都不會進 buffer
            if (!a.Editor && !ProfilerDriver.profileEditor && !EditorApplication.isPlaying)
            {
                ok = false;
                return "沒開始錄：Profiler target 是 Play Mode 又沒在 play，Edit Mode 下一個 frame 都錄不到。\n" +
                       "  要錄 Editor 本身 → `uprofile start --editor`（暫時把 target 切 Editor，stop 時切回）\n" +
                       "  要錄遊戲 → 請使用者進 Play Mode 後再 `uprofile start`";
            }

            if (a.Clear) ProfilerDriver.ClearAllFrames();
            SessionState.SetInt(StartFrameKey, ProfilerDriver.lastFrameIndex + 1);
            SessionState.SetBool(RecKey, true);
            ClaimOwner(a);
            SessionState.EraseInt(ReloadCountKey);
            SessionState.EraseInt(ReloadAtKey);
            SessionState.EraseString(ReloadBeforeKey);
            SessionState.EraseString(ReloadAfterKey);
            if (a.Editor && !ProfilerDriver.profileEditor)
            {
                SessionState.SetBool(RestoreTargetKey, true);
                ProfilerDriver.profileEditor = true;
            }

            ProfilerDriver.enabled = true;
            var sb = new StringBuilder();
            if (takeoverWarn != null) sb.Append(takeoverWarn).Append('\n');
            sb.Append("開始錄 | target: ").Append(ProfilerDriver.profileEditor ? "Editor" : "Play Mode")
                .Append(SessionState.GetBool(RestoreTargetKey, false) ? "（暫時切的，stop 會切回 Play Mode）" : "")
                .Append(" | deep profile: ").Append(DeepDesc())
                .Append(a.Clear ? " | buffer 已清空" : $" | 目前 buffer {BufferDesc()}").Append('\n');
            sb.Append("owner: ").Append(OwnerDesc()).Append("。跑一陣子後 `uprofile stop`（Editor 失焦時 Edit Mode 幾乎不 tick，frame 會很少）");
            if (ProfilerDriver.deepProfiling) sb.Append('\n').Append(DeepMsNote).Append('\n').Append(DeepCrashWarn);
            return sb.ToString();
        }

        /// <summary>
        /// start --deep 的前置。實測（2026-10-03）：
        /// - Play Mode 中設 deepProfiling = true，旗標讀回 true，但這次 play 錄到的不是 deep 資料；reload 被延到離開 Play Mode。
        ///   這專案 Enter Play Mode Options 關了 domain reload，所以進 Play Mode 也不會讓它生效 → 只能在 Edit Mode 開。
        /// - deep + target Editor 在 Play Mode 中：Editor 卡到 uloop 指令 137 秒沒回應，之後 Editor 被關掉 → 禁止這個組合。
        /// 回傳 null = deep 已經開著、照常開始錄；否則回傳訊息直接結束這次 start（ok 表示是正常結果還是錯誤）。
        /// </summary>
        static string PrepareDeep(UProfileArgs a, out bool ok)
        {
            ok = false;
            if (a.Editor || ProfilerDriver.profileEditor)
                return "--deep 不能配 Editor target：整個 Editor 都會插樁，實測 Play Mode 中 Editor 直接卡到無回應。deep 只拿來錄 Play Mode 的遊戲";
            if (ProfilerDriver.deepProfiling) return null; // 已經開著（不管誰開的），照常錄

            if (EditorApplication.isPlaying)
                return "Play Mode 中切 deep 不會生效（實測：旗標變 true，但這次 play 錄到的不是 deep 資料，要等離開 Play Mode 的 reload）。\n" +
                       "  步驟：停 Play Mode → `uprofile start --deep`（只開 deep + domain reload）→ 進 Play Mode → `uprofile start`";

            ok = true;
            ClaimOwner(a);
            SessionState.SetBool(DeepByUsKey, true);
            ProfilerDriver.deepProfiling = true;
            EditorUtility.RequestScriptReload();
            return $"deep profile 打開了（deepProfiling={ProfilerDriver.deepProfiling}），已要求 domain reload；這次沒有開始錄。\n" +
                   DeepCrashWarn + "\n" +
                   "  下一步：等 `uprofile status` 回應且 deep 顯示 on → 進 Play Mode → `uprofile start`（不用再帶 --deep）→ `uprofile stop` 會把 deep 關掉\n" +
                   DeepMsNote;
        }

        // ───────────────────────── owner ─────────────────────────
        // Editor 只有一台、錄製狀態只有一份：多隻 agent 同時用時，不是 owner 的 start / stop / --deep 一律拒絕。
        // owner 由 wrapper 帶（--owner > $CLAUDE_AGENT_ID > $CLAUDE_CODE_SESSION_ID > claude PID）。

        static bool OwnerActive() =>
            SessionState.GetBool(RecKey, false) || SessionState.GetBool(DeepByUsKey, false);

        static string OwnerDesc()
        {
            var owner = SessionState.GetString(OwnerKey, "");
            if (owner.Length == 0) owner = "(不明)";
            var t = OwnerStart(out double mins);
            return t == null ? owner : $"{owner} 從 {t} 開始（{mins:0.#} 分鐘前）";
        }

        static string OwnerStart(out double minutesAgo)
        {
            minutesAgo = 0;
            var raw = SessionState.GetString(OwnerStartKey, "");
            if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var utc)) return null;
            minutesAgo = (DateTime.UtcNow - utc).TotalMinutes;
            return utc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        /// <summary>回傳 null = 可以動；否則是拒絕訊息。takeoverWarn 非 null = 允許但要印警告（--force 或超過 10 分鐘）。</summary>
        static string CheckOwner(UProfileArgs a, out string takeoverWarn)
        {
            takeoverWarn = null;
            if (!OwnerActive()) return null;
            var owner = SessionState.GetString(OwnerKey, "");
            var me = (a.Owner ?? "").Trim();
            if (owner.Length == 0 || owner == me) return null;
            var t = OwnerStart(out double mins);
            string who = t == null ? owner : $"{owner} 從 {t}";
            if (a.Force)
            {
                takeoverWarn = $"warn: --force 接手 {who} 開始的錄製（它的資料會被你 stop 掉）";
                return null;
            }

            if (mins > StaleMinutes)
            {
                takeoverWarn = $"warn: {who} 開始錄、已經 {mins:0} 分鐘沒 stop，當作被丟著不管，由 {me} 接手";
                return null;
            }

            return $"{who} 開始在錄（{mins:0.#} 分鐘前），等它 stop，或加 `--force`（超過 {StaleMinutes:0} 分鐘沒 stop 會自動讓人接手）";
        }

        static void ClaimOwner(UProfileArgs a)
        {
            SessionState.SetString(OwnerKey, (a.Owner ?? "").Trim());
            SessionState.SetString(OwnerStartKey, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        }

        static void ReleaseOwner()
        {
            SessionState.EraseString(OwnerKey);
            SessionState.EraseString(OwnerStartKey);
        }

        const string DeepCrashWarn =
            "警告：2026-10-03 Play Mode 中開著 deep profile 錄製後 Editor 卡死被關掉（懷疑記憶體吃爆）。錄 ≤5 秒、錄完馬上 `uprofile stop`，不要長時間開著";

        static string StopRec(UProfileArgs a, out bool ok)
        {
            ok = true;
            var ownerErr = CheckOwner(a, out string takeoverWarn);
            if (ownerErr != null)
            {
                ok = false;
                return ownerErr;
            }

            bool wasOn = ProfilerDriver.enabled;
            bool started = SessionState.GetBool(RecKey, false);
            string interrupted = started && !wasOn ? InterruptedDesc() : null;
            int reloads = SessionState.GetInt(ReloadCountKey, 0);
            ProfilerDriver.enabled = false;
            bool restored = false;
            if (SessionState.GetBool(RestoreTargetKey, false))
            {
                ProfilerDriver.profileEditor = false;
                SessionState.EraseBool(RestoreTargetKey);
                restored = true;
            }

            bool recordedDeep = ProfilerDriver.deepProfiling;
            var sb = new StringBuilder();
            if (takeoverWarn != null) sb.Append(takeoverWarn).Append('\n');
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
            sb.Append(wasOn ? "已停止" : interrupted != null ? "profiler 早就被停掉了" : "本來就沒在錄")
                .Append(" | buffer ").Append(BufferDesc());
            if (restored) sb.Append(" | target 切回 Play Mode");
            int startFrame = SessionState.GetInt(StartFrameKey, -1);
            if (wasOn && startFrame >= 0 && last >= startFrame)
                sb.Append(" | 這次錄到 ").Append(Math.Max(startFrame, first)).Append('-').Append(last);
            sb.Append('\n');
            if (interrupted != null) sb.Append("warn: ").Append(interrupted).Append('\n');
            else if (wasOn && reloads > 0) sb.Append($"note: 錄製期間有 {reloads} 次 domain reload，profiler 沒被停掉\n");
            SessionState.EraseBool(RecKey);
            if (!SessionState.GetBool(DeepByUsKey, false)) ReleaseOwner(); // deep 由 TurnOffDeepIfOurs 關完再放
            if (first < 0 || last < 0)
            {
                sb.Append("buffer 是空的，沒存檔。");
                sb.Append(TurnOffDeepIfOurs());
                return sb.ToString();
            }

            string path = string.IsNullOrWhiteSpace(a.Save)
                ? Path.Combine(DefaultSaveDir, DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + (recordedDeep ? "-deep" : "") + ".data")
                : a.Save.Trim();
            string full = Path.GetFullPath(path);
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            ProfilerDriver.SaveProfile(full);
            if (System.IO.File.Exists(full))
            {
                sb.Append("存檔: ").Append(full);
                RememberLoaded(full);
                SessionState.SetString(LastSaveKey, full);
                SessionState.SetString(LastSaveRangeKey, $"{first}-{last}");
                SessionState.SetBool(LastSaveDeepKey, recordedDeep);
                if (recordedDeep) sb.Append('\n').Append(DeepMsNote);
            }
            else
            {
                ok = false;
                sb.Append("SaveProfile 沒產生檔案: ").Append(full);
            }

            // 存完檔才關 deep：關 deep 也會 domain reload
            sb.Append(TurnOffDeepIfOurs());
            return sb.ToString();
        }

        static string TurnOffDeepIfOurs()
        {
            if (!SessionState.GetBool(DeepByUsKey, false)) return "";
            SessionState.EraseBool(DeepByUsKey);
            ReleaseOwner();
            if (!ProfilerDriver.deepProfiling) return "";
            ProfilerDriver.deepProfiling = false;
            if (EditorApplication.isPlaying)
                return "\ndeep profile 旗標關掉了（是 uprofile 開的）；插樁要等離開 Play Mode 的 domain reload 才真的拿掉";
            EditorUtility.RequestScriptReload();
            return "\ndeep profile 關掉了（是 uprofile 開的），已要求 domain reload 讓插樁拿掉";
        }

        /// <summary>start 過、profiler 卻已經關掉時的說明：錄到哪、domain reload 前後 Profiler 狀態（實測值）。</summary>
        static string InterruptedDesc()
        {
            int startFrame = SessionState.GetInt(StartFrameKey, -1);
            int reloadAt = SessionState.GetInt(ReloadAtKey, -1);
            int reloads = SessionState.GetInt(ReloadCountKey, 0);
            var sb = new StringBuilder();
            if (reloads > 0)
            {
                sb.Append($"錄製期間有 {reloads} 次 domain reload / 編譯，profiler 被停掉");
                if (startFrame >= 0 && reloadAt >= startFrame) sb.Append($"，錄到的只有 {startFrame}-{reloadAt}");
                else sb.Append("，reload 前一個 frame 都還沒錄到");
                var before = SessionState.GetString(ReloadBeforeKey, "");
                var after = SessionState.GetString(ReloadAfterKey, "");
                if (before.Length > 0 || after.Length > 0) sb.Append($"（reload 前 {before} → 後 {after}）");
            }
            else
                sb.Append("start 過但 profiler 現在是關的（不是 domain reload，可能被 Profiler 視窗或別的工具關掉）");

            return sb.ToString();
        }

        static void RememberLoaded(string full)
        {
            s_loadedPath = full;
            s_loadedMtime = System.IO.File.GetLastWriteTimeUtc(full);
            s_loadedFirst = ProfilerDriver.firstFrameIndex;
            s_loadedLast = ProfilerDriver.lastFrameIndex;
        }

        // ───────────────────────── 資料來源 / 參數 ─────────────────────────

        /// <summary>回傳 null = 可以分析；否則是給 agent 看的錯誤訊息。</summary>
        static string PrepareSource(UProfileArgs a)
        {
            if (!string.IsNullOrWhiteSpace(a.File))
            {
                string full = Path.GetFullPath(a.File.Trim());
                if (!System.IO.File.Exists(full))
                    return $"找不到 {full}。`uprofile stop` 預設存在 Library/uprofile/，ls 一下看檔名（Temp/ 底下的檔 Unity 關掉時會整個刪掉）";
                bool same = full == s_loadedPath
                            && System.IO.File.GetLastWriteTimeUtc(full) == s_loadedMtime
                            && ProfilerDriver.firstFrameIndex == s_loadedFirst
                            && ProfilerDriver.lastFrameIndex == s_loadedLast;
                if (!same)
                {
                    if (ProfilerDriver.enabled)
                        return "profiler 正在錄，--file 會蓋掉目前 buffer。先 `uprofile stop`（會自動存檔）再讀檔";
                    if (!ProfilerDriver.LoadProfile(full, false))
                        return $"LoadProfile 失敗：{full}（不是 Profiler .data，或是別的 Unity 版本存的）";
                    RememberLoaded(full);
                }
            }

            if (ProfilerDriver.firstFrameIndex < 0 || ProfilerDriver.lastFrameIndex < 0)
                return EmptyMsg;
            return null;
        }

        static bool ParseRange(string s, out int a, out int b, out string err, out string note)
        {
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
            a = first;
            b = last;
            err = null;
            note = null;
            s = (s ?? "").Trim();
            if (s.Length == 0) return true;
            string avail = $"可用範圍 {first}-{last}（{last - first + 1} frames）";

            if (s[0] == '-')
            {
                if (!int.TryParse(s.Substring(1), out int n) || n <= 0)
                {
                    err = $"--frames '{s}' 看不懂；格式 A-B / A / A-（A 到最後）/ -N（最後 N 個）。{avail}";
                    return false;
                }
                a = Math.Max(first, last - n + 1);
                return true;
            }

            int dash = s.IndexOf('-');
            bool okA, okB;
            if (dash < 0)
            {
                okA = int.TryParse(s, out a);
                b = a;
                okB = okA;
            }
            else
            {
                okA = int.TryParse(s.Substring(0, dash), out a);
                var tail = s.Substring(dash + 1).Trim();
                if (tail.Length == 0)
                {
                    b = last; // "A-" = 從 A 到最後
                    okB = true;
                }
                else okB = int.TryParse(tail, out b);
            }

            if (!okA || !okB || (b < a && a <= last))
            {
                err = $"--frames '{s}' 看不懂；格式 A-B / A / A-（A 到最後）/ -N（最後 N 個）。{avail}";
                return false;
            }

            if (b < first || a > last)
            {
                err = $"frame {s} 不在 buffer 裡。{avail}";
                return false;
            }

            if (a < first || b > last)
            {
                note = $"note: {s} 超出 buffer，夾到 {Math.Max(a, first)}-{Math.Min(b, last)}";
                a = Math.Max(a, first);
                b = Math.Min(b, last);
            }

            return true;
        }

        /// <summary>
        /// 解析 --thread。main 一律是 thread index 0；其他用名稱比對，回傳 thread 名稱（之後每個 frame 再用名稱找 index，
        /// 因為非 main thread 的 index 在不同 frame 不保證一樣）。threadName == null 代表 main。
        /// </summary>
        static bool ResolveThread(int frame, string q, out string threadName, out string err)
        {
            threadName = null;
            err = null;
            q = (q ?? "").Trim();
            if (q.Length == 0 || q.Equals("main", StringComparison.OrdinalIgnoreCase)) return true;

            var names = new List<string>();
            for (int i = 0; ; i++)
            {
                using (var raw = ProfilerDriver.GetRawFrameDataView(frame, i))
                {
                    if (raw == null || !raw.valid) break;
                    names.Add(raw.threadName);
                }
            }

            if (q.Equals("render", StringComparison.OrdinalIgnoreCase)) q = "Render Thread";

            // 比對優先序：完全相同 > 開頭相同 > 子字串（"worker" 要先對到 "Worker 0"，不是 "EnlightenWorker"）
            for (int pass = 0; pass < 3; pass++)
            {
                int hits = 0;
                foreach (var n in names)
                {
                    bool m = pass == 0 ? n.Equals(q, StringComparison.OrdinalIgnoreCase)
                        : pass == 1 ? n.StartsWith(q, StringComparison.OrdinalIgnoreCase)
                        : n.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!m) continue;
                    if (hits == 0) threadName = n;
                    hits++;
                }

                if (hits == 0) continue;
                if (hits > 1) MultiMatchNote = $"note: '{q}' 對到 {hits} 條 thread，只分析第一條 '{threadName}'；同名的只會取 index 最小的那條";
                return true;
            }

            // 沒命名的 thread 在 Profiler 裡叫 "#<thread id>"，幾百條全是雜訊，只算數量
            // 編號系列（Worker 0..17、Burst-CompilerThread-1..17）折成一筆 "Worker N(×18)"
            var series = new SortedDictionary<string, int>();
            var sample = new Dictionary<string, string>();
            int unnamed = 0;
            foreach (var n in names)
            {
                if (n.StartsWith("#", StringComparison.Ordinal))
                {
                    unnamed++;
                    continue;
                }

                string key = n.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
                if (key.Length == n.Length) key = n;
                series.TryGetValue(key, out int c);
                series[key] = c + 1;
                if (c == 0) sample[key] = n;
            }

            var shown = new List<string>(series.Count);
            foreach (var kv in series)
                shown.Add(kv.Value == 1 ? sample[kv.Key]
                    : sample[kv.Key] == kv.Key ? $"{kv.Key}(×{kv.Value})"
                    : $"{kv.Key}N(×{kv.Value})");
            err = $"frame {frame} 找不到 thread '{q}'。可用（--thread 接子字串，main = Main Thread，render = Render Thread）：{string.Join(", ", shown)}";
            if (err.Length > 1500) err = err.Substring(0, 1500) + "…";
            if (unnamed > 0) err += $"\n（另有 {unnamed} 條沒命名的 thread，名稱是 #<id>，通常不用看）";
            return false;
        }

        static int ThreadIndexInFrame(int frame, string threadName, ref int hint)
        {
            if (threadName == null) return 0;
            if (hint >= 0)
                using (var raw = ProfilerDriver.GetRawFrameDataView(frame, hint))
                    if (raw != null && raw.valid && raw.threadName == threadName)
                        return hint;
            for (int i = 0; ; i++)
                using (var raw = ProfilerDriver.GetRawFrameDataView(frame, i))
                {
                    if (raw == null || !raw.valid) return -1;
                    if (raw.threadName == threadName)
                    {
                        hint = i;
                        return i;
                    }
                }
        }

        static HierarchyFrameDataView OpenView(int frame, int threadIndex)
        {
            var v = ProfilerDriver.GetHierarchyFrameDataView(frame, threadIndex,
                HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                HierarchyFrameDataView.columnTotalTime, false);
            if (v != null && v.valid) return v;
            v?.Dispose();
            return null;
        }

        // ───────────────────────── 彙總 ─────────────────────────

        class Agg
        {
            public string Name;
            public double SelfSum, TotalSum, GcSum, CallsSum, AllocCountSum;
            public double SelfMax, TotalMax, GcMax;
            public int Frames;
            public Dictionary<string, int> Parents;

            // 單一 frame 內的累計
            public double CurSelf, CurTotal, CurGc, CurCalls, CurAllocCount;
            public bool Touched;
            public int Active; // 遞迴時同名 marker 巢狀，total 只算最外層

            public string TopParent()
            {
                if (Parents == null) return "";
                string best = "";
                int bestN = -1;
                foreach (var kv in Parents)
                    if (kv.Value > bestN)
                    {
                        best = kv.Key;
                        bestN = kv.Value;
                    }

                return best;
            }
        }

        class Walker
        {
            public readonly Dictionary<string, Agg> Aggs = new Dictionary<string, Agg>(512);
            public readonly List<Agg> Touched = new List<Agg>(256);
            readonly List<List<int>> _kidPool = new List<List<int>>();
            readonly List<List<string>> _namePool = new List<List<string>>();
            HierarchyFrameDataView _v;
            public double FrameGc;
            public int FramesWalked;

            public void WalkFrame(HierarchyFrameDataView v)
            {
                _v = v;
                FrameGc = 0;
                Touched.Clear();
                int root = v.GetRootItemID();
                VisitChildren(root, "(root)", 0);
            }

            List<int> Kids(int d)
            {
                while (_kidPool.Count <= d) _kidPool.Add(new List<int>(16));
                return _kidPool[d];
            }

            List<string> Names(int d)
            {
                while (_namePool.Count <= d) _namePool.Add(new List<string>(16));
                return _namePool[d];
            }

            void VisitChildren(int id, string parentName, int depth)
            {
                var kids = Kids(depth);
                var names = Names(depth);
                kids.Clear();
                names.Clear();
                _v.GetItemChildren(id, kids);
                for (int i = 0; i < kids.Count; i++) names.Add(_v.GetItemName(kids[i]));
                // 遞迴會用到更深層的 pool，自己這層的 list 不會被改到
                for (int i = 0; i < kids.Count; i++) Visit(kids[i], names[i], parentName, depth + 1);
            }

            void Visit(int id, string name, string parentName, int depth)
            {
                if (name == GcAllocName) return; // GC.Alloc 的量算進 parent 的 self GC

                double self = _v.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnSelfTime);
                double total = _v.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnTotalTime);
                double calls = _v.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnCalls);
                double gc = _v.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnGcMemory);

                // GC Alloc 欄是 inclusive（含子節點）。self GC = 自己 - 非 GC.Alloc 子節點的 GC
                var kids = Kids(depth);
                kids.Clear();
                _v.GetItemChildren(id, kids);
                double kidGc = 0, allocCount = 0;
                for (int i = 0; i < kids.Count; i++)
                {
                    int k = kids[i];
                    double kg = _v.GetItemColumnDataAsDouble(k, HierarchyFrameDataView.columnGcMemory);
                    if (_v.GetItemName(k) == GcAllocName)
                        allocCount += _v.GetItemColumnDataAsDouble(k, HierarchyFrameDataView.columnCalls);
                    else
                        kidGc += kg;
                }

                double selfGc = Math.Max(0, gc - kidGc);

                if (!Aggs.TryGetValue(name, out var ag))
                {
                    ag = new Agg { Name = name };
                    Aggs.Add(name, ag);
                }

                if (!ag.Touched)
                {
                    ag.Touched = true;
                    ag.CurSelf = ag.CurTotal = ag.CurGc = ag.CurCalls = ag.CurAllocCount = 0;
                    Touched.Add(ag);
                }

                ag.CurSelf += self;
                if (ag.Active == 0) ag.CurTotal += total;
                ag.CurGc += selfGc;
                ag.CurCalls += calls;
                ag.CurAllocCount += allocCount;
                FrameGc += selfGc;
                if (ag.Parents == null) ag.Parents = new Dictionary<string, int>();
                ag.Parents.TryGetValue(parentName, out int pc);
                ag.Parents[parentName] = pc + 1;

                if (kids.Count > 0)
                {
                    ag.Active++;
                    VisitChildren(id, name, depth);
                    ag.Active--;
                }
            }

            public void EndFrame()
            {
                FramesWalked++;
                foreach (var ag in Touched)
                {
                    ag.Touched = false;
                    ag.Frames++;
                    ag.SelfSum += ag.CurSelf;
                    ag.TotalSum += ag.CurTotal;
                    ag.GcSum += ag.CurGc;
                    ag.CallsSum += ag.CurCalls;
                    ag.AllocCountSum += ag.CurAllocCount;
                    if (ag.CurSelf > ag.SelfMax) ag.SelfMax = ag.CurSelf;
                    if (ag.CurTotal > ag.TotalMax) ag.TotalMax = ag.CurTotal;
                    if (ag.CurGc > ag.GcMax) ag.GcMax = ag.CurGc;
                }
            }
        }

        // ───────────────────────── 分析子指令 ─────────────────────────

        static string Analyze(string cmd, UProfileArgs a, out bool ok)
        {
            ok = false;
            var srcErr = PrepareSource(a);
            if (srcErr != null) return srcErr;

            string note;
            int fa, fb;
            if (cmd == "frame")
            {
                if (string.IsNullOrWhiteSpace(a.Frames))
                    return $"frame 要給 frame index：`uprofile frame <n>`。可用範圍 {ProfilerDriver.firstFrameIndex}-{ProfilerDriver.lastFrameIndex}";
                if (!ParseRange(a.Frames, out fa, out fb, out var e, out note)) return e;
                fb = fa;
            }
            else if (!ParseRange(a.Frames, out fa, out fb, out var e, out note)) return e;

            MultiMatchNote = null;
            if (!ResolveThread(fa, a.Thread, out var threadName, out var terr)) return terr;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sb = new StringBuilder(a.MaxChars + 256);
            string result;
            switch (cmd)
            {
                case "top": result = Top(a, fa, fb, threadName, sb, false); break;
                case "gc": result = Top(a, fa, fb, threadName, sb, true); break;
                case "spikes": result = Spikes(a, fa, fb, threadName, sb); break;
                default: result = Frame(a, fa, threadName, sb); break;
            }

            if (result != null) return result; // 錯誤訊息
            ok = true;
            if (MultiMatchNote != null) sb.Insert(0, MultiMatchNote + "\n");
            if (LooksDeep(a)) sb.Insert(0, DeepMsNote + "\n");
            if (string.IsNullOrWhiteSpace(a.File))
            {
                var lastSave = SessionState.GetString(LastSaveKey, "");
                // stop 剛存完、buffer 沒變 = buffer 就是那份檔，不用提示
                bool bufferIsLastSave = lastSave == s_loadedPath
                                        && ProfilerDriver.firstFrameIndex == s_loadedFirst
                                        && ProfilerDriver.lastFrameIndex == s_loadedLast;
                if (lastSave.Length > 0 && !bufferIsLastSave)
                    sb.Insert(0, $"note: 讀的是目前 buffer {BufferDesc()}；最後存的是 {lastSave}（frames {SessionState.GetString(LastSaveRangeKey, "?")}），要讀它加 --file\n");
            }
            if (note != null) sb.Insert(0, note + "\n");
            sb.Append($"\n({sw.ElapsedMilliseconds} ms)");
            return sb.ToString();
        }

        /// <summary>
        /// 資料是不是 deep profile 錄的：.data 檔本身看不出來，靠「uprofile stop 存 deep 時檔名帶 -deep」
        /// 以及 SessionState 記的最後一次存檔是不是 deep。
        /// </summary>
        static bool LooksDeep(UProfileArgs a)
        {
            if (!string.IsNullOrWhiteSpace(a.File))
                return Path.GetFileNameWithoutExtension(a.File).EndsWith("-deep", StringComparison.OrdinalIgnoreCase);
            if (ProfilerDriver.deepProfiling) return true;
            return SessionState.GetBool(LastSaveDeepKey, false)
                   && SessionState.GetString(LastSaveKey, "") == s_loadedPath
                   && ProfilerDriver.firstFrameIndex == s_loadedFirst
                   && ProfilerDriver.lastFrameIndex == s_loadedLast;
        }

        static string ThreadLabel(string threadName) => threadName ?? "Main Thread";

        static string Top(UProfileArgs a, int fa, int fb, string threadName, StringBuilder sb, bool gcMode)
        {
            int limit = a.Limit > 0 ? a.Limit : 30;
            string sort = gcMode ? "gc" : (a.Sort ?? "self").Trim().ToLowerInvariant();
            if (sort != "self" && sort != "total" && sort != "gc" && sort != "calls")
                return $"--sort '{a.Sort}' 不支援，可用 self|total|gc|calls\n{Usage}";

            var w = new Walker();
            int hint = -1, missing = 0;
            for (int f = fa; f <= fb; f++)
            {
                int ti = ThreadIndexInFrame(f, threadName, ref hint);
                var v = ti < 0 ? null : OpenView(f, ti);
                if (v == null)
                {
                    missing++;
                    continue;
                }

                using (v)
                {
                    w.WalkFrame(v);
                    w.EndFrame();
                }
            }

            int n = w.FramesWalked;
            if (n == 0) return $"frames {fa}-{fb} 在 {ThreadLabel(threadName)} 上沒有可讀的資料";

            var list = new List<Agg>(w.Aggs.Count);
            foreach (var ag in w.Aggs.Values)
                if (!gcMode || ag.GcSum > 0)
                    list.Add(ag);

            Comparison<Agg> cmp;
            switch (sort)
            {
                case "total": cmp = (x, y) => y.TotalSum.CompareTo(x.TotalSum); break;
                case "gc": cmp = (x, y) => y.GcSum.CompareTo(x.GcSum); break;
                case "calls": cmp = (x, y) => y.CallsSum.CompareTo(x.CallsSum); break;
                default: cmp = (x, y) => y.SelfSum.CompareTo(x.SelfSum); break;
            }

            list.Sort(cmp);

            sb.Append($"frames {fa}-{fb}（{n} frames");
            if (missing > 0) sb.Append($"，{missing} 個沒資料略過");
            sb.Append($"）| thread {ThreadLabel(threadName)} | sort {sort} | marker 依名稱合併，avg 是每 frame 平均\n");

            var rows = new List<string>(Math.Min(limit, list.Count));
            if (gcMode)
            {
                double totalGc = 0;
                foreach (var ag in list) totalGc += ag.GcSum;
                sb.Append($"範圍內 GC 總量 {Bytes(totalGc)}，平均 {Bytes(totalGc / n)}/frame。GC 算在呼叫 GC.Alloc 的那個 marker（self GC）\n");
                if (list.Count == 0)
                {
                    sb.Append("這段沒有 GC alloc。");
                    return null;
                }

                sb.Append("   GC/f   GC max  GC total  allocs/f frames  marker <- 最常見 parent\n");
                for (int i = 0; i < list.Count && i < limit; i++)
                {
                    var ag = list[i];
                    rows.Add(string.Format(CultureInfo.InvariantCulture, "{0,7} {1,8} {2,9} {3,8:0.#} {4,6}  {5}{6}",
                        Bytes(ag.GcSum / n), Bytes(ag.GcMax), Bytes(ag.GcSum), ag.AllocCountSum / n, ag.Frames,
                        Trunc(ag.Name, 60), ParentSuffix(ag)));
                }
            }
            else
            {
                sb.Append("self avg self max  tot avg  tot max    GC/f  calls/f frames  marker <- 最常見 parent\n");
                for (int i = 0; i < list.Count && i < limit; i++)
                {
                    var ag = list[i];
                    rows.Add(string.Format(CultureInfo.InvariantCulture, "{0,8:0.000} {1,8:0.00} {2,8:0.000} {3,8:0.00} {4,7} {5,8:0.#} {6,6}  {7}{8}",
                        ag.SelfSum / n, ag.SelfMax, ag.TotalSum / n, ag.TotalMax, Bytes(ag.GcSum / n), ag.CallsSum / n,
                        ag.Frames, Trunc(ag.Name, 60), ParentSuffix(ag)));
                }
            }

            AppendRows(sb, rows, list.Count, a.MaxChars);
            for (int i = 0; i < list.Count && i < 5; i++)
                if (list[i].Name == "EditorLoop")
                {
                    sb.Append('\n').Append(EditorLoopNote);
                    break;
                }

            return null;
        }

        const string EditorLoopNote =
            "note: EditorLoop 的 self = Editor 自己的開銷（Play Mode）或 Edit Mode 失焦時的節流等待（約 100ms/frame），不是遊戲 code";

        static string ParentSuffix(Agg ag)
        {
            var p = ag.TopParent();
            return string.IsNullOrEmpty(p) || p == "(root)" ? "" : " <- " + Trunc(p, 40);
        }

        struct FrameTime
        {
            public int Frame;
            public double Ms;
        }

        static string Spikes(UProfileArgs a, int fa, int fb, string threadName, StringBuilder sb)
        {
            int limit = a.Limit > 0 ? a.Limit : 10;
            float threshold = a.Threshold > 0 ? a.Threshold : 33f;

            // pass 1：只讀 frame 時間（RawFrameDataView 不建 hierarchy，便宜）
            var times = new List<FrameTime>(fb - fa + 1);
            int hint = -1;
            for (int f = fa; f <= fb; f++)
            {
                int ti = ThreadIndexInFrame(f, threadName, ref hint);
                if (ti < 0) continue;
                using (var raw = ProfilerDriver.GetRawFrameDataView(f, ti))
                    if (raw != null && raw.valid)
                        times.Add(new FrameTime { Frame = f, Ms = raw.frameTimeMs });
            }

            if (times.Count == 0) return $"frames {fa}-{fb} 在 {ThreadLabel(threadName)} 上沒有可讀的資料";
            double sum = 0;
            foreach (var t in times) sum += t.Ms;
            times.Sort((x, y) => y.Ms.CompareTo(x.Ms));
            int over = 0;
            foreach (var t in times)
                if (t.Ms >= threshold)
                    over++;

            sb.Append($"frames {fa}-{fb}（{times.Count} frames）| thread {ThreadLabel(threadName)} | avg {sum / times.Count:0.00} ms | 最慢 {times[0].Ms:0.00} ms | ≥{threshold:0.#}ms 有 {over} 個\n");
            int show;
            if (over == 0)
            {
                sb.Append($"沒有 frame 超過 {threshold:0.#} ms，改列最慢的 {Math.Min(limit, times.Count)} 個（--threshold 可調）\n");
                show = Math.Min(limit, times.Count);
            }
            else show = Math.Min(limit, over);

            sb.Append("  frame       ms       GC  self 最重的 3 個 marker\n");
            var rows = new List<string>(show);
            var w = new Walker();
            var top3 = new List<Agg>(4);
            bool sawEditorLoop = false;
            for (int i = 0; i < show; i++)
            {
                var t = times[i];
                int ti = ThreadIndexInFrame(t.Frame, threadName, ref hint);
                var v = ti < 0 ? null : OpenView(t.Frame, ti);
                string detail = "(讀不到 hierarchy)";
                double gc = 0;
                if (v != null)
                    using (v)
                    {
                        w.WalkFrame(v);
                        gc = w.FrameGc;
                        top3.Clear();
                        foreach (var ag in w.Touched) InsertTop3(top3, ag);
                        var d = new StringBuilder();
                        foreach (var ag in top3)
                        {
                            if (ag.Name == "EditorLoop") sawEditorLoop = true;
                            if (d.Length > 0) d.Append(", ");
                            d.Append(Trunc(ag.Name, 40)).Append(' ').Append(ag.CurSelf.ToString("0.00", CultureInfo.InvariantCulture));
                        }

                        detail = d.ToString();
                        w.EndFrame();
                    }

                rows.Add(string.Format(CultureInfo.InvariantCulture, "{0,7} {1,8:0.00} {2,8}  {3}", t.Frame, t.Ms, Bytes(gc), detail));
            }

            AppendRows(sb, rows, over == 0 ? show : over, a.MaxChars);
            if (sawEditorLoop) sb.Append('\n').Append(EditorLoopNote);
            return null;
        }

        static void InsertTop3(List<Agg> top, Agg ag)
        {
            int pos = top.Count;
            while (pos > 0 && top[pos - 1].CurSelf < ag.CurSelf) pos--;
            if (pos >= 3) return;
            top.Insert(pos, ag);
            if (top.Count > 3) top.RemoveAt(3);
        }

        static string Frame(UProfileArgs a, int frame, string threadName, StringBuilder sb)
        {
            int maxDepth = a.Depth > 0 ? a.Depth : 4;
            double minMs = a.MinMs >= 0 ? a.MinMs : 0.1;
            int hint = -1;
            int ti = ThreadIndexInFrame(frame, threadName, ref hint);
            if (ti < 0) return $"frame {frame} 沒有 thread {ThreadLabel(threadName)}";
            var v = OpenView(frame, ti);
            if (v == null) return $"frame {frame} 在 {ThreadLabel(threadName)} 上讀不到 hierarchy";
            using (v)
            {
                sb.Append($"frame {frame} | thread {ThreadLabel(threadName)} | {v.frameTimeMs:0.00} ms | depth≤{maxDepth} | 剪掉 total < {minMs:0.###} ms\n");
                sb.Append("total ms  self ms  calls      GC  marker\n");
                var rows = new List<string>(128);
                int pruned = 0;
                var stack = new List<int>(64);
                FrameTree(v, v.GetRootItemID(), 0, maxDepth, minMs, rows, ref pruned, stack);
                AppendRows(sb, rows, rows.Count, a.MaxChars);
                if (pruned > 0) sb.Append($"\n（另有 {pruned} 個 < {minMs:0.###} ms 的節點沒列，--min-ms 0 看全部）");
            }

            return null;
        }

        static void FrameTree(HierarchyFrameDataView v, int id, int depth, int maxDepth, double minMs, List<string> rows, ref int pruned, List<int> scratch)
        {
            var kids = new List<int>(8);
            v.GetItemChildren(id, kids); // view 建的時候已依 total time 降冪排序
            foreach (var k in kids)
            {
                double total = v.GetItemColumnDataAsDouble(k, HierarchyFrameDataView.columnTotalTime);
                if (total < minMs)
                {
                    pruned++;
                    continue;
                }

                double self = v.GetItemColumnDataAsDouble(k, HierarchyFrameDataView.columnSelfTime);
                double calls = v.GetItemColumnDataAsDouble(k, HierarchyFrameDataView.columnCalls);
                double gc = v.GetItemColumnDataAsDouble(k, HierarchyFrameDataView.columnGcMemory);
                string indent = new string(' ', depth * 2);
                rows.Add(string.Format(CultureInfo.InvariantCulture, "{0,8:0.00} {1,8:0.00} {2,6:0} {3,7}  {4}{5}",
                    total, self, calls, gc > 0 ? Bytes(gc) : "-", indent, Trunc(v.GetItemName(k), 70)));

                if (!v.HasItemChildren(k)) continue;
                if (depth + 1 >= maxDepth)
                {
                    scratch.Clear();
                    v.GetItemChildren(k, scratch);
                    double sumKids = 0;
                    foreach (var c in scratch) sumKids += v.GetItemColumnDataAsDouble(c, HierarchyFrameDataView.columnTotalTime);
                    rows.Add($"{"",33}{new string(' ', (depth + 1) * 2)}…({scratch.Count} 個子節點, {sumKids.ToString("0.00", CultureInfo.InvariantCulture)} ms)");
                    continue;
                }

                FrameTree(v, k, depth + 1, maxDepth, minMs, rows, ref pruned, scratch);
            }
        }

        // ───────────────────────── 格式 ─────────────────────────

        static void AppendRows(StringBuilder sb, List<string> rows, int totalAvailable, int maxChars)
        {
            int shown = 0;
            for (; shown < rows.Count; shown++)
            {
                var r = rows[shown];
                if (sb.Length + r.Length + 1 > maxChars - 80) break;
                sb.Append(r).Append('\n');
            }

            int rest = totalAvailable - shown;
            if (rest > 0) sb.Append($"…(還有 {rest} 筆，用 --limit 調；字數上限用 --max-chars)\n");
            if (sb.Length > 0 && sb[sb.Length - 1] == '\n') sb.Length--;
        }

        static string Trunc(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }

        static string Bytes(double b)
        {
            if (b <= 0) return "0";
            if (b < 1024) return b.ToString("0", CultureInfo.InvariantCulture) + "B";
            if (b < 1024 * 1024) return (b / 1024).ToString("0.0", CultureInfo.InvariantCulture) + "K";
            return (b / (1024 * 1024)).ToString("0.00", CultureInfo.InvariantCulture) + "M";
        }
    }

    /// <summary>
    /// 記錄「uprofile 錄製中」遇到 domain reload 的時間點與前後的 ProfilerDriver 狀態，讓 `uprofile stop` / `status`
    /// 能講清楚錄到哪裡被中斷。只在 SessionState 有 recording 旗標時動作。
    /// </summary>
    [InitializeOnLoad]
    static class UProfileReloadWatcher
    {
        static UProfileReloadWatcher()
        {
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            if (!SessionState.GetBool(UProfileReader.RecKey, false)) return;

            if (SessionState.GetInt(UProfileReader.ReloadCountKey, 0) == 0) return;
            // static ctor 在 reload 後第一時間跑；delayCall 再補一次，抓 Editor 初始化完之後的值
            SessionState.SetString(UProfileReader.ReloadAfterKey, Snapshot());
            EditorApplication.delayCall += () =>
                SessionState.SetString(UProfileReader.ReloadAfterKey, Snapshot());
        }

        static void BeforeReload()
        {
            if (!SessionState.GetBool(UProfileReader.RecKey, false)) return;
            int n = SessionState.GetInt(UProfileReader.ReloadCountKey, 0) + 1;
            SessionState.SetInt(UProfileReader.ReloadCountKey, n);
            if (n == 1)
            {
                SessionState.SetInt(UProfileReader.ReloadAtKey, ProfilerDriver.lastFrameIndex);
                SessionState.SetString(UProfileReader.ReloadBeforeKey, Snapshot());
            }
        }

        static string Snapshot() =>
            $"enabled={ProfilerDriver.enabled} target={(ProfilerDriver.profileEditor ? "Editor" : "PlayMode")} last={ProfilerDriver.lastFrameIndex}";
    }
}
