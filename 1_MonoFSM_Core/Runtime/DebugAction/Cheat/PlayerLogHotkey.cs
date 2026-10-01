using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace MonoFSM.Core
{
    /// <summary>
    ///     Build 裡找 log 用的熱鍵，路徑會複製到剪貼簿並在 Finder / 檔案總管打開：
    ///     Ctrl/Cmd + Shift + L → Player.log（<see cref="Application.consoleLogPath" />，Editor 裡是 Editor.log）；
    ///     Ctrl/Cmd + Shift + K → crash 資料夾（Windows：%TEMP%/公司名/產品名/Crashes，itch 會把 TEMP 換到自己的 temp；
    ///     Mac：~/Library/Logs/DiagnosticReports）。
    ///     開場時由 <see cref="RuntimeInitializeOnLoadMethodAttribute" /> 自己生成一個 DontDestroyOnLoad 物件，
    ///     不用在 scene 上擺，build 出去也一定有。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerLogHotkey : MonoBehaviour
    {
        public enum Status
        {
            NotTriggered,
            Revealed,
            CopiedOnly_RevealFailed,
            NoKeyboard,
            EmptyLogPath,
            CrashFolderMissing_OpenedParent,
        }

        [SerializeField] private Key _logKey = Key.L;
        [SerializeField] private Key _crashKey = Key.K;
        [SerializeField] private CheatModifier _modifiers = CheatModifier.Ctrl | CheatModifier.Shift;

        [Sirenix.OdinInspector.ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private Status _status = Status.NotTriggered;

        [Sirenix.OdinInspector.ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private string _lastPath;

        private CheatEntry _logEntry;
        private CheatEntry _crashEntry;

        /// <summary>Unity player crash dump 的資料夾。Windows 走 %TEMP%（itch 會改寫 TEMP），Mac 是系統 crash report 位置。</summary>
        public static string CrashFolderPath
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        return Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                            "Library/Logs/DiagnosticReports");
                    default:
                        return Path.Combine(Path.GetTempPath(), Application.companyName,
                            Application.productName, "Crashes");
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindAnyObjectByType<PlayerLogHotkey>() != null)
                return;
            var go = new GameObject(nameof(PlayerLogHotkey));
            DontDestroyOnLoad(go);
            go.AddComponent<PlayerLogHotkey>();
        }

        private void OnEnable()
        {
            _logEntry ??= CheatRegistry.Register(_logKey, _modifiers,
                "複製 Player.log 路徑並在 Finder 顯示", nameof(PlayerLogHotkey), RevealLog,
                owner: gameObject);
            _crashEntry ??= CheatRegistry.Register(_crashKey, _modifiers,
                "複製 crash 資料夾路徑並打開", nameof(PlayerLogHotkey), RevealCrashFolder,
                owner: gameObject);
        }

        private void OnDisable()
        {
            CheatRegistry.Unregister(_logEntry);
            CheatRegistry.Unregister(_crashEntry);
            _logEntry = null;
            _crashEntry = null;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                _status = Status.NoKeyboard;
                return;
            }

            if (CheatRegistry.IsTriggered(_logEntry, keyboard))
                RevealLog();
            else if (CheatRegistry.IsTriggered(_crashEntry, keyboard))
                RevealCrashFolder();
        }

        [Sirenix.OdinInspector.Button]
        public void RevealLog()
        {
            var path = Application.consoleLogPath;
            _lastPath = path;
            if (string.IsNullOrEmpty(path))
            {
                _status = Status.EmptyLogPath;
                return;
            }

            GUIUtility.systemCopyBuffer = path;
            _status = TryReveal(path, true) ? Status.Revealed : Status.CopiedOnly_RevealFailed;
            Debug.Log($"[PlayerLogHotkey] {_status}: {path}", this);
        }

        [Sirenix.OdinInspector.Button]
        public void RevealCrashFolder()
        {
            var path = CrashFolderPath;
            _lastPath = path;
            GUIUtility.systemCopyBuffer = path;

            //還沒 crash 過的話資料夾不存在，退一層打開（Windows 上就是 Player 的 temp 資料夾）
            if (!Directory.Exists(path))
            {
                var parent = Path.GetDirectoryName(path);
                _status = parent != null && Directory.Exists(parent) && TryReveal(parent, false)
                    ? Status.CrashFolderMissing_OpenedParent
                    : Status.CopiedOnly_RevealFailed;
            }
            else
            {
                _status = TryReveal(path, false) ? Status.Revealed : Status.CopiedOnly_RevealFailed;
            }

            Debug.Log($"[PlayerLogHotkey] {_status}: {path}", this);
        }

        /// <param name="selectFile">true = 打開所在資料夾並選取該檔；false = 直接打開該資料夾</param>
        private static bool TryReveal(string path, bool selectFile)
        {
            try
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        Process.Start("open", selectFile ? $"-R \"{path}\"" : $"\"{path}\"");
                        return true;
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                        var winPath = path.Replace('/', '\\');
                        Process.Start("explorer.exe", selectFile ? $"/select,\"{winPath}\"" : $"\"{winPath}\"");
                        return true;
                    default:
                        Application.OpenURL("file://" + (selectFile ? Path.GetDirectoryName(path) : path));
                        return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerLogHotkey] reveal 失敗：{e.Message}");
                return false;
            }
        }
    }
}
