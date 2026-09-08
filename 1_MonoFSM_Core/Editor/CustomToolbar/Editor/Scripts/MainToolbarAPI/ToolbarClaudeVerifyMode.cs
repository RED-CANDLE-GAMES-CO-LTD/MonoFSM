using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityToolbarExtender.ToolbarElements
{
    /// <summary>
    ///     主 toolbar 上的「Claude 驗證模式」下拉選單（Check / Auto / Compile）。
    ///     狀態寫在 專案根目錄/.claude/.verify-mode，由 .claude/scripts/uverify.sh 讀取，
    ///     決定 agent 改完 C# 之後要不要 domain reload（domain reload 會打斷正在用 Editor 的人）。
    ///     Check   = 只做離線編譯檢查 + hot reload，絕不打斷；專心開發時用。
    ///     Auto    = 純寫 code 同 Check；要接著串 prefab 的工作（uverify.sh --wiring）才編譯。預設值。
    ///     Compile = 一律直接編譯；人離開電腦、讓 agent 自己跑完時用。
    ///     檔案可能被 agent 或別的 session 改掉，所以背景每秒重讀一次，變了就 Refresh。
    ///
    ///     另外讀 .claude/.needs-compile：uverify.sh 熱更沒完全落地（新 .cs / 新型別 / 序列化欄位變動）時會寫它，
    ///     此時按鈕轉成警示樣式，選單第一項可以直接編譯。domain reload 一發生就代表那批改動已落地，
    ///     所以 static ctor（每次 reload 都會跑）順手把標記刪掉。
    /// </summary>
    [InitializeOnLoad]
    public static class ToolbarClaudeVerifyMode
    {
        private const string ElementId = "MonoFSM/ClaudeVerifyMode";

        private const string Check = "check";
        private const string Auto = "auto";
        private const string Compile = "compile";
        private const double PollInterval = 1.0;

        private static string _cachedMode = Auto;
        private static string _cachedPending; // null = 沒有待編譯的改動
        private static double _lastPoll;

        static ToolbarClaudeVerifyMode()
        {
            // 走到這裡代表 domain reload 剛完成 → 所有改過的 .cs 都已編進來，待編譯標記失效
            ClearPending();
            _cachedMode = ReadMode();
            EditorApplication.update -= PollFile;
            EditorApplication.update += PollFile;
        }

        private static string ClaudeDir =>
            Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, ".claude");

        private static string ModeFilePath => Path.Combine(ClaudeDir, ".verify-mode");
        private static string PendingFilePath => Path.Combine(ClaudeDir, ".needs-compile");

        private static void PollFile()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastPoll < PollInterval) return;
            _lastPoll = now;

            var mode = ReadMode();
            var pending = ReadPending();
            if (mode == _cachedMode && pending == _cachedPending) return;
            _cachedMode = mode;
            _cachedPending = pending;
            MainToolbar.Refresh(ElementId);
        }

        /// <summary>uverify.sh 寫的待編譯標記；第一行是摘要（Skipped/Failed 統計），其餘是細節。</summary>
        private static string ReadPending()
        {
            try
            {
                var path = PendingFilePath;
                if (!File.Exists(path)) return null;
                var raw = File.ReadAllText(path).Trim();
                if (raw.Length == 0) return "熱更未完全生效";
                // uhot 的 Skipped 理由每行可以長達數百字，tooltip 塞不下，截成摘要
                var lines = raw.Split('\n');
                var sb = new System.Text.StringBuilder();
                for (var i = 0; i < lines.Length && i < 4; i++)
                {
                    var line = lines[i].TrimEnd();
                    if (line.Length > 120) line = line.Substring(0, 120) + "…";
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(line);
                }

                if (lines.Length > 4) sb.Append("\n… 共 ").Append(lines.Length).Append(" 行，詳見 .claude/.needs-compile");
                return sb.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void ClearPending()
        {
            _cachedPending = null;
            try
            {
                if (File.Exists(PendingFilePath)) File.Delete(PendingFilePath);
            }
            catch (Exception)
            {
                // 檔案被鎖住就算了，下次 reload 再刪
            }
        }

        private static void CompileNow()
        {
            // 新增的 .cs 可能還沒被 import（uloop 熱更期間會 hold auto refresh），先 Refresh 再要求編譯
            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
        }

        private static string ReadMode()
        {
            try
            {
                var path = ModeFilePath;
                if (!File.Exists(path)) return Auto;
                var raw = File.ReadAllText(path).Trim();
                return raw == Check || raw == Compile || raw == Auto ? raw : Auto;
            }
            catch (Exception)
            {
                return Auto; // 檔案被別人鎖住之類的，不要讓 toolbar 噴例外
            }
        }

        private static void WriteMode(string mode)
        {
            var path = ModeFilePath;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, mode + "\n");
                _cachedMode = mode;
                _lastPoll = EditorApplication.timeSinceStartup;
                MainToolbar.Refresh(ElementId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ClaudeVerifyMode] 寫入 {path} 失敗：{e.Message}");
            }
        }

        [MainToolbarElement(ElementId, defaultDockPosition = MainToolbarDockPosition.Right)]
        private static IEnumerable<MainToolbarElement> CreateModeDropdown()
        {
            yield return new MainToolbarDropdown(GetContent(), ShowMenu);
        }

        private static MainToolbarContent GetContent()
        {
            var mode = _cachedMode;
            string text, tooltip, icon;
            if (_cachedPending != null)
                return new MainToolbarContent
                {
                    text = "AI: 待編譯 ⚠",
                    tooltip = "Claude 改的 C# 有部分沒能熱更落地，需要編譯一次：\n" + _cachedPending +
                              "\n（點開選單可以立刻編譯）",
                    image = EditorGUIUtility.IconContent("d_console.erroricon.sml").image as Texture2D
                };

            if (mode == Check)
            {
                text = "AI: Check";
                tooltip = "Claude 只做離線編譯檢查 + hot reload，絕不 domain reload 打斷你";
                icon = "d_Valid";
            }
            else if (mode == Compile)
            {
                text = "AI: Compile";
                tooltip = "Claude 改完 C# 一律直接編譯，會 domain reload 打斷你";
                icon = "d_console.warnicon.sml";
            }
            else
            {
                text = "AI: Auto";
                tooltip = "純寫 code 不打斷；要接著串 prefab 的工作才編譯";
                icon = "d_UnityEditor.AnimationWindow";
            }

            return new MainToolbarContent
            {
                text = text,
                tooltip = tooltip,
                image = EditorGUIUtility.IconContent(icon).image as Texture2D
            };
        }

        private static void ShowMenu(Rect rect)
        {
            var menu = new GenericMenu();
            if (_cachedPending != null)
            {
                var reason = _cachedPending.Split('\n')[0];
                menu.AddDisabledItem(new GUIContent("待編譯：" + reason.Replace("/", "\u2215")));
                menu.AddItem(new GUIContent("⚠ 立刻編譯（會 domain reload）"), false, CompileNow);
                menu.AddItem(new GUIContent("忽略，清掉待編譯標記"), false, () =>
                {
                    ClearPending();
                    MainToolbar.Refresh(ElementId);
                });
                menu.AddSeparator("");
            }

            AddItem(menu, "Check（絕不打斷）", Check);
            AddItem(menu, "Auto（串 prefab 的工作才編譯）", Auto);
            AddItem(menu, "Compile（一律直接編譯）", Compile);
            menu.DropDown(rect);
        }

        private static void AddItem(GenericMenu menu, string label, string mode)
        {
            menu.AddItem(new GUIContent(label), _cachedMode == mode, () => WriteMode(mode));
        }
    }
}
