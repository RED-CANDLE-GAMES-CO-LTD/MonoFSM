using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
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
        private static double _lastPoll;

        static ToolbarClaudeVerifyMode()
        {
            _cachedMode = ReadMode();
            EditorApplication.update -= PollFile;
            EditorApplication.update += PollFile;
        }

        private static string ModeFilePath =>
            Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, ".claude", ".verify-mode");

        private static void PollFile()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastPoll < PollInterval) return;
            _lastPoll = now;

            var mode = ReadMode();
            if (mode == _cachedMode) return;
            _cachedMode = mode;
            MainToolbar.Refresh(ElementId);
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
