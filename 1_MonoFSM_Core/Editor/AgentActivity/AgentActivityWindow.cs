using System.Collections.Generic;
using MonoFSM.FSM;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.AgentActivity
{
    /// <summary>
    ///     Tools/MonoFSM/Agent Activity：看 agent 最近透過 uprefab CLI 對 Editor 做了什麼（最近 50 筆，新的在上面），
    ///     可以手動清掉 agent 掛掉後留下的「Play 測試中」旗標，也順便放 FsmTrace 的開關 / Clear / Dump。
    ///     資料來源與解析見 <see cref="AgentActivityLog" />；主 toolbar 的狀態鈕點下去也會開這個視窗。
    /// </summary>
    public class AgentActivityWindow : OdinEditorWindow
    {
        private const int ListCount = 50;
        private const double PollInterval = 0.5;

        [MenuItem("Tools/MonoFSM/Agent Activity")]
        public static void Open()
        {
            GetWindow<AgentActivityWindow>("Agent Activity").Show();
        }

        private readonly HashSet<string> _expanded = new();
        private double _lastPoll;
        private GUIStyle _failStyle;
        private GUIStyle _wrapStyle;

        // ---- 目前狀態 ----

        [Title("目前狀態")]
        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        [HideLabel]
        private string Status => AgentActivityLog.StatusLine();

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        [LabelText("測試中旗標")]
        private string FlagInfo =>
            AgentActivityLog.TestingFlagExists
                ? "[" + AgentActivityLog.TestingSession + "] " + AgentActivityLog.TestingSince +
                  "，" + (int)AgentActivityLog.TestingFlagAge + " 秒前更新" +
                  (AgentActivityLog.TestingActive ? "" : "（已過期）")
                : "（沒有）";

        [Button("清除測試中旗標")]
        [EnableIf(nameof(HasFlag))]
        private void ClearTestingFlag()
        {
            AgentActivityLog.ClearTestingFlag();
        }

        // ---- 除錯資訊 ----

        [FoldoutGroup("解析狀態")]
        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private string LogPath => AgentActivityLog.LogPath;

        [FoldoutGroup("解析狀態")]
        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private int ParsedLines => AgentActivityLog.ParsedLines;

        [FoldoutGroup("解析狀態")]
        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private int BadLines => AgentActivityLog.BadLines;

        [FoldoutGroup("解析狀態")]
        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private int CallCount => AgentActivityLog.Calls.Count;

        [FoldoutGroup("解析狀態")]
        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private string LastError => AgentActivityLog.LastError ?? "-";

        // ---- FsmTrace ----

        [Title("FsmTrace")]
        [ShowInInspector]
        [LabelText("記錄中")]
        private bool TraceEnabled
        {
            get => FsmTrace.Enabled;
            set => FsmTrace.Enabled = value;
        }

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        [LabelText("目前筆數")]
        private string TraceCount => FsmTrace.Count + " / " + FsmTrace.Capacity + "（累計寫入 " + FsmTrace.TotalWritten + "）";

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        [LabelText("最後一次 Dump")]
        private string _lastDumpPath = "-";

        [ButtonGroup("Trace")]
        [Button("Clear")]
        private void TraceClear()
        {
            FsmTrace.Clear();
        }

        [ButtonGroup("Trace")]
        [Button("Dump All")]
        private void TraceDumpAll()
        {
            _lastDumpPath = FsmTrace.Dump("manual");
        }

        [ButtonGroup("Trace")]
        [Button("Dump Selected")]
        [EnableIf(nameof(HasSelectedEntity))]
        private void TraceDumpSelected()
        {
            var entity = SelectedEntity();
            if (entity == null)
            {
                _lastDumpPath = "（沒有選到 MonoEntity）";
                return;
            }

            _lastDumpPath = FsmTrace.Dump("manual_" + entity.name, entity);
        }

        private bool HasFlag => AgentActivityLog.TestingFlagExists;
        private bool HasSelectedEntity => SelectedEntity() != null;

        private static MonoEntity SelectedEntity()
        {
            var go = Selection.activeGameObject;
            return go != null ? go.GetComponentInParent<MonoEntity>(true) : null;
        }

        // ---- 呼叫紀錄 ----

        [Title("最近呼叫（新的在上面，點一下展開）")]
        [OnInspectorGUI]
        private void DrawCalls()
        {
            _failStyle ??= new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
            _wrapStyle ??= new GUIStyle(EditorStyles.label) { wordWrap = true };

            var calls = AgentActivityLog.Calls;
            if (calls.Count == 0)
            {
                EditorGUILayout.HelpBox("還沒有紀錄。up 碰 Unity 的指令才會記（離線指令和直接叫 uloop 的不會）。",
                    MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("時間", GUILayout.Width(60));
                GUILayout.Label("session", GUILayout.Width(60));
                GUILayout.Label("指令", GUILayout.ExpandWidth(true));
                GUILayout.Label("結果", GUILayout.Width(44));
                GUILayout.Label("耗時", GUILayout.Width(56));
            }

            var shown = 0;
            for (var i = calls.Count - 1; i >= 0 && shown < ListCount; i--, shown++)
            {
                var c = calls[i];
                var failed = c.HasEnd && !c._ok;
                var style = failed ? _failStyle : EditorStyles.label;
                var result = !c.HasEnd ? (c.Duration > AgentActivityLog.RunningTimeout ? "無回應" : "執行中") : c._ok ? "OK" : "失敗";

                var rect = EditorGUILayout.BeginHorizontal();
                GUILayout.Label(AgentActivityLog.FormatTime(c._start), style, GUILayout.Width(60));
                GUILayout.Label(c._session, style, GUILayout.Width(60));
                GUILayout.Label(AgentActivityLog.Abbrev(c._argv, 80), style, GUILayout.ExpandWidth(true));
                GUILayout.Label(result, style, GUILayout.Width(44));
                GUILayout.Label(c.Duration.ToString("0.0") + "s", style, GUILayout.Width(56));
                EditorGUILayout.EndHorizontal();

                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    if (!_expanded.Remove(c._id)) _expanded.Add(c._id);
                    Event.current.Use();
                }

                if (_expanded.Contains(c._id))
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.SelectableLabel(c._argv, _wrapStyle,
                            GUILayout.Height(_wrapStyle.CalcHeight(new GUIContent(c._argv), position.width - 40)));
                        EditorGUILayout.LabelField("method", c._method);
                        EditorGUILayout.LabelField("摘要", string.IsNullOrEmpty(c._summary) ? "-" : c._summary,
                            failed ? _failStyle : _wrapStyle);
                    }
            }
        }

        private void Update()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastPoll < PollInterval) return;
            _lastPoll = now;
            // 有指令在跑時經過秒數會變，也要重畫
            if (AgentActivityLog.Poll() || AgentActivityLog.Running != null) Repaint();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            AgentActivityLog.Invalidate();
            AgentActivityLog.Poll();
        }
    }
}
