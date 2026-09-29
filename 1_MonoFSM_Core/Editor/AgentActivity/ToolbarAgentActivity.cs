using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace MonoFSM.Editor.AgentActivity
{
    /// <summary>
    ///     主 toolbar 上的「agent 現在在用 Editor 做什麼」狀態鈕，跟 AI: Check/Auto/Compile 同一套 MainToolbarElement 機制。
    ///     資料來自 uprefab CLI 寫的 Library/AgentActivity/activity.jsonl 與 .claude/.agent-testing（見 <see cref="AgentActivityLog" />）。
    ///     顯示優先序：Play 測試中旗標（醒目）＞ 正在跑的 up 指令（縮寫 + 經過秒數）＞ 最近 10 秒的最後一筆（淡色）＞ 只剩小圖示。
    ///     0.5 秒輪詢一次檔案 mtime，有變才解析；顯示文字變了才 Refresh。點下去開 <see cref="AgentActivityWindow" />。
    /// </summary>
    [InitializeOnLoad]
    public static class ToolbarAgentActivity
    {
        private const string ElementId = "MonoFSM/AgentActivity";
        private const double PollInterval = 0.5;
        private const int AbbrevMax = 24;

        private const string TestingColor = "#FF7A45";
        private const string DimColor = "#FFFFFF73";

        private static double _lastPoll;
        private static string _cachedText = "";
        private static string _cachedIcon = "";

        static ToolbarAgentActivity()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastPoll < PollInterval) return;
            _lastPoll = now;

            AgentActivityLog.Poll();
            // 經過秒數每秒會變，所以不只看 Poll 的回傳值，而是比對算出來的文字
            Compute(out var text, out var icon, out _);
            if (text == _cachedText && icon == _cachedIcon) return;
            _cachedText = text;
            _cachedIcon = icon;
            MainToolbar.Refresh(ElementId);
        }

        private static void Compute(out string text, out string icon, out string tooltip)
        {
            var running = AgentActivityLog.Running;
            if (AgentActivityLog.TestingActive)
            {
                var label = "🤖 Play 測試中 [" + AgentActivityLog.TestingSession + "]";
                if (running != null)
                    label += " · " + AgentActivityLog.Abbrev(running._argv, AbbrevMax) + " " +
                             (int)(AgentActivityLog.Now - running._start) + "s";
                text = "<color=" + TestingColor + "><b>" + label + "</b></color>";
                icon = "d_Record On";
                tooltip = "agent 正在 Play Mode 測試（" + AgentActivityLog.TestingSince +
                          " 開始），先別動 Editor。\n點開 Agent Activity 視窗可手動清除旗標。";
                return;
            }

            if (running != null)
            {
                text = "🤖 " + AgentActivityLog.Abbrev(running._argv, AbbrevMax) + " " +
                       (int)(AgentActivityLog.Now - running._start) + "s";
                icon = "d_WaitSpin00";
                tooltip = "[" + running._session + "] " + running._argv + "\n→ " + running._method;
                return;
            }

            var last = AgentActivityLog.Last;
            if (last != null && AgentActivityLog.SinceLastActivity < AgentActivityLog.RecentWindow)
            {
                text = "<color=" + DimColor + ">🤖 " + AgentActivityLog.Abbrev(last._argv, AbbrevMax) + "</color>";
                icon = last.HasEnd && !last._ok ? "d_console.erroricon.sml" : "d_UnityEditor.ConsoleWindow";
                tooltip = "[" + last._session + "] " + last._argv + "\n" + (last._ok ? "OK" : "失敗") +
                          (string.IsNullOrEmpty(last._summary) ? "" : "：" + last._summary);
                return;
            }

            text = "";
            icon = "d_UnityEditor.ConsoleWindow";
            tooltip = "Agent Activity：沒有 agent 在用 Editor。點開看最近的呼叫紀錄";
        }

        [MainToolbarElement(ElementId, defaultDockPosition = MainToolbarDockPosition.Right)]
        private static IEnumerable<MainToolbarElement> CreateButton()
        {
            Compute(out var text, out var icon, out var tooltip);
            var content = new MainToolbarContent
            {
                text = text,
                tooltip = tooltip,
                image = EditorGUIUtility.IconContent(icon).image as Texture2D
            };
            yield return new MainToolbarButton(content, AgentActivityWindow.Open);
        }
    }
}
