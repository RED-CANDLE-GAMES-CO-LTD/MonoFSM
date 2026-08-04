using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace MonoFSM.Core
{
    public class CheatManager : AbstractDescriptionBehaviour
    {
        public void CheatKeyCheck()
        {

            if (Keyboard.current[Key.LeftMeta].isPressed ||
                Keyboard.current[Key.LeftCtrl].isPressed)
            {
                //重置關卡
                if (
                    Keyboard.current[Key.R].wasPressedThisFrame)
                {
                    if (
                        Keyboard.current[Key.LeftShift].isPressed)
                        WorldUpdateSimulator.ManualResetLevel(true);
                    else
                    {
                        WorldUpdateSimulator.ManualResetLevel();
                    }
                }
                // 在這裡執行作弊行為，例如增加分數、解鎖功能等
            }

            //切換語言（循環）
            if (Keyboard.current.digit9Key.wasPressedThisFrame)
                CycleLocale();

            if (Keyboard.current.digit0Key.IsPressed() || Mouse.current.middleButton.isPressed)
            {
                WorldUpdateSimulator.TimeScale = 5f;
                Debug.Log(" WorldUpdateSimulator.TimeScale = 5f;");
            }

            else
                WorldUpdateSimulator.TimeScale = 1f;
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
