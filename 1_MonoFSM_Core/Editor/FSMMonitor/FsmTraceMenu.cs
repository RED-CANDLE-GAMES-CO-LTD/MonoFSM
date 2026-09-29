using System.Collections.Generic;
using MonoFSM.FSM;
using MonoFSM.Runtime;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace MonoFSM.Editor
{
    /// <summary>
    /// FsmTrace 的手動入口：選單 Tools/MonoFSM/FSM Trace（Enabled / Clear / Dump All / Dump Selected Entity）
    /// 跟主 toolbar 上的「Trace: On/Off (筆數)」按鈕，兩邊呼叫同一組 static method。
    /// 玩的時候自己開 trace、出事時 dump，再用 `up fsm-trace` 讀 Library/FsmTrace/ 底下的檔；
    /// CLI 也能透過 `up menu "Tools/MonoFSM/FSM Trace/..."` 叫同一組選單。
    /// </summary>
    [InitializeOnLoad]
    public static class FsmTraceMenu
    {
        private const string Root = "Tools/MonoFSM/FSM Trace/";
        private const string EnabledPath = Root + "Enabled";
        private const string ToolbarToggleId = "MonoFSM/FsmTrace";
        private const string ToolbarMenuId = "MonoFSM/FsmTraceMenu";
        private const double PollInterval = 1.0;

        private static bool _shownEnabled;
        private static int _shownCount = -1;
        private static double _lastPoll;

        static FsmTraceMenu()
        {
            EditorApplication.update -= PollToolbar;
            EditorApplication.update += PollToolbar;
        }

        // SHARED ACTIONS（選單跟 toolbar 共用）

        public static void SetEnabled(bool value)
        {
            FsmTrace.Enabled = value;
            Notify(value ? "FsmTrace 開始記錄" : "FsmTrace 停止記錄");
            RefreshToolbar();
        }

        public static void ClearTrace()
        {
            FsmTrace.Clear();
            Notify("FsmTrace 已清空");
            RefreshToolbar();
        }

        public static void DumpAllTrace()
        {
            var path = FsmTrace.Dump("manual");
            Notify("FsmTrace → " + path + "（用 up fsm-trace 讀）");
        }

        public static void DumpSelectedEntityTrace()
        {
            var entity = SelectedEntity();
            if (entity == null)
            {
                Notify("先在 Hierarchy 選一個 entity（或它底下的節點）");
                return;
            }

            var path = FsmTrace.Dump("manual_" + entity.name, entity);
            Notify("FsmTrace → " + path + "（用 up fsm-trace 讀）");
        }

        // MENU ITEMS

        [MenuItem(EnabledPath, false, 0)]
        private static void ToggleEnabled() => SetEnabled(!FsmTrace.Enabled);

        [MenuItem(EnabledPath, true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(EnabledPath, FsmTrace.Enabled);
            return true;
        }

        [MenuItem(Root + "Clear", false, 1)]
        private static void Clear() => ClearTrace();

        [MenuItem(Root + "Dump All", false, 20)]
        private static void DumpAll() => DumpAllTrace();

        [MenuItem(Root + "Dump Selected Entity", false, 21)]
        private static void DumpSelectedEntity() => DumpSelectedEntityTrace();

        [MenuItem(Root + "Dump Selected Entity", true)]
        private static bool DumpSelectedEntityValidate() => SelectedEntity() != null;

        // MAIN TOOLBAR（跟 AI: Check/Auto/Compile 同一套 MainToolbar API，停在右側）

        [MainToolbarElement(ToolbarToggleId, defaultDockPosition = MainToolbarDockPosition.Right)]
        private static IEnumerable<MainToolbarElement> CreateToolbarToggle()
        {
            yield return new MainToolbarToggle(GetToggleContent(), FsmTrace.Enabled, SetEnabled);
        }

        //MainToolbar 沒有 split button，Clear / Dump 放旁邊一顆下拉
        [MainToolbarElement(ToolbarMenuId, defaultDockPosition = MainToolbarDockPosition.Right)]
        private static IEnumerable<MainToolbarElement> CreateToolbarMenu()
        {
            yield return new MainToolbarDropdown(
                new MainToolbarContent { text = "Trace ▾", tooltip = "FsmTrace：Clear / Dump" }, ShowToolbarMenu);
        }

        private static MainToolbarContent GetToggleContent()
        {
            var on = FsmTrace.Enabled;
            _shownEnabled = on;
            _shownCount = FsmTrace.Count;
            return new MainToolbarContent
            {
                text = (on ? "Trace: On " : "Trace: Off ") + _shownCount,
                tooltip = (on ? "FsmTrace 記錄中，點一下停止" : "FsmTrace 沒在記，點一下開始") +
                          $"\nbuffer {_shownCount} / {FsmTrace.Capacity}（總共寫過 {FsmTrace.TotalWritten}）" +
                          "\n讀檔：Dump 之後跑 `up fsm-trace`",
                image = EditorGUIUtility.IconContent(on ? "d_Record On" : "d_Record Off").image as Texture2D
            };
        }

        private static void ShowToolbarMenu(Rect rect)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Enabled"), FsmTrace.Enabled, () => SetEnabled(!FsmTrace.Enabled));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Clear"), false, ClearTrace);
            menu.AddItem(new GUIContent("Dump All"), false, DumpAllTrace);
            if (SelectedEntity() != null)
                menu.AddItem(new GUIContent("Dump Selected Entity"), false, DumpSelectedEntityTrace);
            else
                menu.AddDisabledItem(new GUIContent("Dump Selected Entity（先選一個 entity）"));
            menu.DropDown(rect);
        }

        //筆數每 tick 都可能變，一秒最多刷一次
        private static void PollToolbar()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastPoll < PollInterval) return;
            _lastPoll = now;
            if (FsmTrace.Enabled == _shownEnabled && FsmTrace.Count == _shownCount) return;
            RefreshToolbar();
        }

        private static void RefreshToolbar()
        {
            MainToolbar.Refresh(ToolbarToggleId);
        }

        private static MonoEntity SelectedEntity()
        {
            var go = Selection.activeGameObject;
            return go != null ? go.GetComponentInParent<MonoEntity>(true) : null;
        }

        //不用 Debug.Log：用 editor 視窗的 toast，不洗 Console
        private static void Notify(string message)
        {
            var window = EditorWindow.focusedWindow != null
                ? EditorWindow.focusedWindow
                : SceneView.lastActiveSceneView;
            if (window != null)
                window.ShowNotification(new GUIContent(message), 2f);
        }
    }
}
