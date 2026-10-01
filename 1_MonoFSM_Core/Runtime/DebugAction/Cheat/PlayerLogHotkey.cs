using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace MonoFSM.Core
{
    /// <summary>
    ///     按 Ctrl/Cmd + Shift + L：把 Player.log（<see cref="Application.consoleLogPath" />）路徑複製到剪貼簿，
    ///     並在 Finder / 檔案總管裡選取該檔。Editor 裡拿到的是 Editor.log。
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
        }

        [SerializeField] private Key _key = Key.L;
        [SerializeField] private CheatModifier _modifiers = CheatModifier.Ctrl | CheatModifier.Shift;

        [Sirenix.OdinInspector.ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private Status _status = Status.NotTriggered;

        [Sirenix.OdinInspector.ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private string _lastLogPath;

        private CheatEntry _cheatEntry;

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
            _cheatEntry ??= CheatRegistry.Register(_key, _modifiers,
                "複製 Player.log 路徑並在 Finder 顯示", nameof(PlayerLogHotkey), RevealLog,
                owner: gameObject);
        }

        private void OnDisable()
        {
            CheatRegistry.Unregister(_cheatEntry);
            _cheatEntry = null;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                _status = Status.NoKeyboard;
                return;
            }

            if (CheatRegistry.IsTriggered(_cheatEntry, keyboard))
                RevealLog();
        }

        [Sirenix.OdinInspector.Button]
        public void RevealLog()
        {
            var path = Application.consoleLogPath;
            _lastLogPath = path;
            if (string.IsNullOrEmpty(path))
            {
                _status = Status.EmptyLogPath;
                return;
            }

            GUIUtility.systemCopyBuffer = path;
            _status = TryReveal(path) ? Status.Revealed : Status.CopiedOnly_RevealFailed;
            Debug.Log($"[PlayerLogHotkey] {_status}: {path}");
        }

        private static bool TryReveal(string path)
        {
            try
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        Process.Start("open", $"-R \"{path}\"");
                        return true;
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                        Process.Start("explorer.exe", $"/select,\"{path.Replace('/', '\\')}\"");
                        return true;
                    default:
                        Application.OpenURL("file://" + Path.GetDirectoryName(path));
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
