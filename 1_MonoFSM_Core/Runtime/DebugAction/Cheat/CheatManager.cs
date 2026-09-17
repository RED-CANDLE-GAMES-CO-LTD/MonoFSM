using System.Collections.Generic;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace MonoFSM.Core
{
    public class CheatManager : AbstractDescriptionBehaviour
    {
        //自己登錄的那幾筆（CheatKeyCheck 只輪詢這些；其他系統的 cheat 由它們自己判定，避免重複觸發）
        private readonly List<CheatEntry> _ownEntries = new(8);

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        [ListDrawerSettings(IsReadOnly = true)]
        [PropertyTooltip("目前登錄在 CheatRegistry 的所有 cheat（含其他系統登錄的）")]
        private List<string> CheatEntries => CheatRegistry.GetDebugLines();

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        [ListDrawerSettings(IsReadOnly = true)]
        [PropertyTooltip("同一組 key + modifier 被不同來源登錄超過一筆")]
        private List<string> CheatConflicts => CheatRegistry.GetConflictLines();

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private CheatManagerStatus _status = CheatManagerStatus.NotRunYet;

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private bool _isTimeScaleBoosting;

        public enum CheatManagerStatus
        {
            NotRunYet,
            Polling,
            NoKeyboard,
        }

        private void EnsureRegistered()
        {
            if (_ownEntries.Count > 0)
                return;

            const string source = nameof(CheatManager);

            //Cmd/Ctrl + R：soft reset。Cmd + Alt + R 也走這裡（額外的瞬移由 PlayerStartSpawnPoint 自己攔 Alt），
            //所以只把 Shift 列為 forbidden
            _ownEntries.Add(CheatRegistry.Register(Key.R, CheatModifier.Ctrl, "Soft reset 關卡", source,
                SoftResetLevel, forbiddenModifiers: CheatModifier.Shift, owner: gameObject));

            _ownEntries.Add(CheatRegistry.Register(Key.R, CheatModifier.Ctrl | CheatModifier.Shift,
                "Hard reset 關卡", source, HardResetLevel, owner: gameObject));

            //原本是 else if：按著 Ctrl/Cmd 時 F5 不生效
            _ownEntries.Add(CheatRegistry.Register(Key.F5, CheatModifier.None, "Soft reset 關卡", source,
                SoftResetLevel, forbiddenModifiers: CheatModifier.Ctrl, owner: gameObject));

            _ownEntries.Add(CheatRegistry.Register(Key.Digit9, CheatModifier.None, "循環切換語言", source,
                CycleLocale, owner: gameObject));

            //按住型：只登錄給 Palette / Inspector 看，實際判定在 UpdateTimeScaleHold
            _ownEntries.Add(CheatRegistry.Register(Key.Digit0, CheatModifier.None,
                "按住 0 或滑鼠中鍵：TimeScale = 5", source, null, true, owner: gameObject));
        }

        private void UnregisterAll()
        {
            for (var i = 0; i < _ownEntries.Count; i++)
                CheatRegistry.Unregister(_ownEntries[i]);
            _ownEntries.Clear();
        }

        private void OnDisable()
        {
            UnregisterAll();
        }

        private static void SoftResetLevel()
        {
            WorldUpdateSimulator.ManualResetLevel();
        }

        private static void HardResetLevel()
        {
            WorldUpdateSimulator.ManualResetLevel(true);
        }

        public void CheatKeyCheck()
        {
            EnsureRegistered();

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                _status = CheatManagerStatus.NoKeyboard;
                return;
            }

            _status = CheatManagerStatus.Polling;

            for (var i = 0; i < _ownEntries.Count; i++)
            {
                var entry = _ownEntries[i];
                if (!CheatRegistry.IsTriggered(entry, keyboard))
                    continue;
                entry._invoke?.Invoke();
            }

            UpdateTimeScaleHold(keyboard);
        }

        private void UpdateTimeScaleHold(Keyboard keyboard)
        {
            var isBoosting = keyboard.digit0Key.isPressed ||
                             (Mouse.current != null && Mouse.current.middleButton.isPressed);
            _isTimeScaleBoosting = isBoosting;
            WorldUpdateSimulator.TimeScale = isBoosting ? 5f : 1f;
        }

        private static void CycleLocale()
        {
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                Debug.Log("[Cheat] Localization 還沒初始化完成，忽略切換語言");
                return;
            }

            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if (locales == null || locales.Count == 0)
            {
                Debug.Log("[Cheat] 找不到可用的 Locale，忽略切換語言");
                return;
            }

            var current = LocalizationSettings.SelectedLocale;
            var index = current == null ? -1 : locales.IndexOf(current);
            var next = locales[(index + 1) % locales.Count];
            LocalizationSettings.SelectedLocale = next;
            Debug.Log($"[Cheat] 切換語言: {current?.Identifier.Code} -> {next.Identifier.Code}");
            DumpLocaleDiagnostic(next);
        }

        //診斷用：切完語言後直接問 StringDatabase 拿一筆，用來區分「Localization 層沒換到（多半是 Addressables
        //content 沒重 build）」和「換到了但 UI binder 沒 refresh」。build 版看 Player.log。
        private static void DumpLocaleDiagnostic(Locale locale)
        {
            var op = LocalizationSettings.StringDatabase.GetTableAsync(DiagnosticTableName, locale);
            op.WaitForCompletion();
            var table = op.Result;
            if (table == null)
            {
                Debug.LogError(
                    $"[Cheat] StringTable '{DiagnosticTableName}' 在 {locale.Identifier.Code} 載不到（status={op.Status}）" +
                    "，多半是 Addressables content 沒重 build");
                return;
            }

            var count = 0;
            string sampleKey = null;
            string sampleValue = null;
            foreach (var entry in table.Values)
            {
                count++;
                if (sampleKey != null)
                    continue;
                sampleKey = entry.Key;
                sampleValue = entry.LocalizedValue;
            }

            Debug.Log(
                $"[Cheat] StringTable '{table.TableCollectionName}' locale={locale.Identifier.Code} entries={count} " +
                $"sample: {sampleKey}=\"{sampleValue}\"");
        }

        //改成你實際在畫面上看的那張 table
        private const string DiagnosticTableName = "GameplayUI";

        public void Update()
        {
            CheatKeyCheck();
        }
    }
}
