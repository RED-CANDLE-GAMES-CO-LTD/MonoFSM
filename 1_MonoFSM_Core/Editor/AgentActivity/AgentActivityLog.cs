using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MonoFSM.Editor.AgentActivity
{
    /// <summary>
    ///     讀 uprefab CLI 寫的 agent activity log（Library/AgentActivity/activity.jsonl）和「測試中」旗標
    ///     （.claude/.agent-testing），給 toolbar（<see cref="ToolbarAgentActivity" />）和
    ///     <see cref="AgentActivityWindow" /> 共用。
    ///     寫入端是 MonoFSM/Tools~/uprefab/activity.py：每次 up 碰 Unity 前後各 append 一行 begin / end。
    ///     只在檔案 mtime 變了才重新解析，呼叫端自己決定輪詢頻率（toolbar 0.5 秒一次）。
    /// </summary>
    public static class AgentActivityLog
    {
        /// <summary>begin 之後超過這麼久還沒 end，就當作那個 process 已經死掉，不算「正在跑」。</summary>
        public const double RunningTimeout = 60;

        /// <summary>沒有指令在跑時，最後一筆活動在這段時間內還會淡淡顯示在 toolbar 上。</summary>
        public const double RecentWindow = 10;

        /// <summary>測試中旗標超過這麼久沒被 touch 就視為過期（agent 掛掉、沒跑 up play stop）。</summary>
        public const double TestingFlagTimeout = 600;

        private const int MaxCalls = 500;

        /// <summary>jsonl 一行的 JSON DTO；欄位名必須跟 activity.py 寫出的 key 一樣，所以不套底線命名。</summary>
        [Serializable]
        private class Line
        {
            public string ev;
            public string id;
            public string session;
            public string argv;
            public string method;
            public string summary;
            public double t;
            public bool ok;
        }

        /// <summary>一次 Unity 呼叫（begin + end 合併）。</summary>
        public class Call
        {
            public string _id;
            public string _session;
            public string _argv;
            public string _method;
            public double _start;
            public double _end; // 0 = 還沒收到 end
            public bool _ok;
            public string _summary;

            public bool HasEnd => _end > 0;
            public double Duration => HasEnd ? _end - _start : Now - _start;
        }

        private static readonly List<Call> _calls = new();
        private static readonly Dictionary<string, Call> _byId = new();
        private static DateTime _logStamp;
        private static DateTime _flagStamp;
        private static bool _flagExists;
        private static bool _dirty = true;

        /// <summary>時間由舊到新。</summary>
        public static IReadOnlyList<Call> Calls => _calls;

        public static int ParsedLines { get; private set; }
        public static int BadLines { get; private set; }
        public static string LastError { get; private set; }

        public static bool TestingFlagExists => _flagExists;
        public static string TestingSession { get; private set; }
        public static string TestingSince { get; private set; }
        public static double TestingFlagAge => _flagExists ? (DateTime.UtcNow - _flagStamp).TotalSeconds : 0;
        public static bool TestingActive => _flagExists && TestingFlagAge < TestingFlagTimeout;

        public static double Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        public static string ProjectRoot => Directory.GetParent(Application.dataPath)!.FullName;
        public static string LogPath => Path.Combine(ProjectRoot, "Library", "AgentActivity", "activity.jsonl");
        public static string FlagPath => Path.Combine(ProjectRoot, ".claude", ".agent-testing");

        /// <summary>檢查兩個檔的 mtime，有變才重新解析。回傳 true = 內容變了。</summary>
        public static bool Poll()
        {
            var changed = false;
            try
            {
                var logPath = LogPath;
                var stamp = File.Exists(logPath) ? File.GetLastWriteTimeUtc(logPath) : default;
                if (_dirty || stamp != _logStamp)
                {
                    _logStamp = stamp;
                    Parse(logPath);
                    changed = true;
                }

                var flagPath = FlagPath;
                var exists = File.Exists(flagPath);
                var flagStamp = exists ? File.GetLastWriteTimeUtc(flagPath) : default;
                if (_dirty || exists != _flagExists || flagStamp != _flagStamp)
                {
                    _flagExists = exists;
                    _flagStamp = flagStamp;
                    ReadFlag(flagPath);
                    changed = true;
                }
            }
            catch (Exception e)
            {
                LastError = e.Message;
            }

            _dirty = false;
            return changed;
        }

        /// <summary>強制下次 Poll 重讀。</summary>
        public static void Invalidate()
        {
            _dirty = true;
        }

        private static void Parse(string path)
        {
            _calls.Clear();
            _byId.Clear();
            ParsedLines = 0;
            BadLines = 0;
            if (!File.Exists(path)) return;

            string text;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(fs, Encoding.UTF8))
            {
                text = reader.ReadToEnd();
            }

            var start = 0;
            while (start < text.Length)
            {
                var nl = text.IndexOf('\n', start);
                if (nl < 0) break; // 最後一行還沒寫完（沒有換行），下次再讀
                if (nl > start) ParseLine(text.Substring(start, nl - start));
                start = nl + 1;
            }

            if (_calls.Count > MaxCalls) _calls.RemoveRange(0, _calls.Count - MaxCalls);
            LastError = null;
        }

        private static void ParseLine(string raw)
        {
            Line line;
            try
            {
                line = JsonUtility.FromJson<Line>(raw);
            }
            catch (Exception)
            {
                BadLines++;
                return;
            }

            if (line == null || string.IsNullOrEmpty(line.id))
            {
                BadLines++;
                return;
            }

            ParsedLines++;
            if (line.ev == "begin")
            {
                var call = new Call
                {
                    _id = line.id, _session = line.session, _argv = line.argv, _method = line.method, _start = line.t
                };
                _byId[line.id] = call;
                _calls.Add(call);
            }
            else if (line.ev == "end" && _byId.TryGetValue(line.id, out var call))
            {
                call._end = line.t;
                call._ok = line.ok;
                call._summary = line.summary;
            }
        }

        private static void ReadFlag(string path)
        {
            TestingSession = null;
            TestingSince = null;
            if (!_flagExists) return;
            try
            {
                var raw = File.ReadAllText(path).Trim();
                var tab = raw.IndexOf('\t');
                TestingSession = tab >= 0 ? raw.Substring(0, tab) : raw;
                TestingSince = tab >= 0 ? raw.Substring(tab + 1) : "";
            }
            catch (Exception e)
            {
                LastError = e.Message;
            }
        }

        /// <summary>手動清掉測試中旗標（agent 掛掉沒跑 up play stop 時用）。</summary>
        public static void ClearTestingFlag()
        {
            try
            {
                if (File.Exists(FlagPath)) File.Delete(FlagPath);
            }
            catch (Exception e)
            {
                LastError = e.Message;
            }

            Invalidate();
            Poll();
        }

        /// <summary>最新一筆還在跑（有 begin 沒 end、而且 begin 不到 60 秒）的呼叫；沒有就 null。</summary>
        public static Call Running
        {
            get
            {
                var now = Now;
                for (var i = _calls.Count - 1; i >= 0; i--)
                {
                    var c = _calls[i];
                    if (now - c._start > RunningTimeout) return null;
                    if (!c.HasEnd) return c;
                }

                return null;
            }
        }

        public static Call Last => _calls.Count > 0 ? _calls[_calls.Count - 1] : null;

        /// <summary>最後一筆活動（begin 或 end）距今幾秒；沒有紀錄回 double.MaxValue。</summary>
        public static double SinceLastActivity
        {
            get
            {
                var last = Last;
                if (last == null) return double.MaxValue;
                return Now - (last.HasEnd ? last._end : last._start);
            }
        }

        /// <summary>把 "up prefab read Assets/…/Foo.prefab --node X" 縮成 "prefab read Foo.prefab…"。</summary>
        public static string Abbrev(string argv, int max)
        {
            if (string.IsNullOrEmpty(argv)) return "?";
            var s = argv.StartsWith("up ", StringComparison.Ordinal) ? argv.Substring(3) : argv;
            var parts = s.Split(' ');
            var sb = new StringBuilder();
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                var token = p.Trim('"');
                // 路徑只留檔名，省空間
                var slash = token.LastIndexOf('/');
                if (slash >= 0 && slash < token.Length - 1) token = token.Substring(slash + 1);
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(token);
                if (sb.Length >= max) break;
            }

            if (sb.Length > max)
            {
                sb.Length = max;
                sb.Append('…');
            }

            return sb.ToString();
        }

        /// <summary>toolbar / 視窗共用的一行狀態文字（不含 rich text 顏色）。</summary>
        public static string StatusLine()
        {
            var running = Running;
            var sb = new StringBuilder();
            if (TestingActive)
            {
                sb.Append("Play 測試中 [").Append(TestingSession).Append(']');
                if (running != null) sb.Append(" · ");
            }

            if (running != null)
                sb.Append(Abbrev(running._argv, 28)).Append(' ').Append((int)(Now - running._start)).Append('s');
            if (sb.Length == 0)
            {
                var last = Last;
                sb.Append(last == null ? "（沒有紀錄）" : "閒置，最後：" + Abbrev(last._argv, 40));
            }

            if (_flagExists && !TestingActive)
                sb.Append("  （測試中旗標已過期 ").Append((int)(TestingFlagAge / 60)).Append(" 分鐘）");
            return sb.ToString();
        }

        public static string FormatTime(double epoch)
        {
            if (epoch <= 0) return "-";
            return DateTimeOffset.FromUnixTimeMilliseconds((long)(epoch * 1000)).ToLocalTime().ToString("HH:mm:ss");
        }
    }
}
