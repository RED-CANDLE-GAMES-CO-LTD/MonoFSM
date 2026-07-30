#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace RCGInputAction
{
    //自動填表：從專案裡所有 InputPromptUIData 反查這台裝置需要的 binding path，補進 _entries 並猜好 sprite 名稱。
    //猜錯或猜不出來的照樣可以在 Inspector 用下拉選單改，這裡只負責把「手打路徑」這段苦工拿掉。
    public partial class DeviceIconMapConfig
    {
        //一次補表的結果，給呼叫端組 log（InputPromptUIData 那端要彙總多台裝置）
        public struct FillReport
        {
            public int _added;
            public int _filledSpriteName;
            public int _filledIcon;
            public List<string> _unmatched; //猜不出 sprite 名稱、要人工補的
        }

        [PropertyOrder(-1)]
        [InfoBox("這份 config 沒被任何 PromptIconRegistry 指到，無法判斷是哪台裝置，自動填表會不知道要填哪些 sprite",
            InfoMessageType.Warning, "@!" + nameof(HasOwnerFamily))]
        [Button("從專案的 InputPromptUIData 補齊 binding path（含 sprite 名稱建議）", ButtonSizes.Medium)]
        private void FillEntriesFromPrompts()
        {
            if (!PromptIconMapEditorUtility.TryFindOwnerFamily(this, out var family))
            {
                Debug.LogError(
                    "[DeviceIconMapConfig] 找不到這份 config 對應的 PromptDeviceFamily，" +
                    "請先在 PromptIconRegistry 把它掛到某個 family", this);
                return;
            }

            var usages = PromptIconMapEditorUtility.CollectBindingUsages();
            var report = FillEntriesFor(family, usages);

            var message =
                $"[DeviceIconMapConfig] {name}（{family}）新增 {report._added} 筆、" +
                $"填好 {report._filledSpriteName} 個 sprite 名稱、{report._filledIcon} 張 icon";
            if (report._unmatched.Count > 0)
                message += $"\n猜不出 sprite 名稱、要人工補的 {report._unmatched.Count} 筆：\n  " +
                           string.Join("\n  ", report._unmatched);
            Debug.Log(message, this);
        }

        //補進指定 family 該負責的 binding path：缺的 entry 補上、sprite 名稱用建議值猜、icon 從 sprite sheet 反查。
        //已填過的欄位一律不覆蓋，避免蓋掉人工調整。
        public FillReport FillEntriesFor(
            PromptDeviceFamily family,
            IEnumerable<PromptIconMapEditorUtility.BindingUsage> usages
        )
        {
            var report = new FillReport { _unmatched = new List<string>() };

            //沒指定就用 family 同名的 sprite asset（專案慣例：Xbox / KeyboardMouse …）
            if (string.IsNullOrEmpty(_spriteAssetName))
                _spriteAssetName = family.ToString();

            var validSpriteNames = new HashSet<string>(
                PromptIconMapEditorUtility.GetSpriteNames(_spriteAssetName));
            if (validSpriteNames.Count == 0)
                Debug.LogWarning(
                    $"[DeviceIconMapConfig] 找不到名為 {_spriteAssetName} 的 TMP Sprite Asset，" +
                    "這次只會補 binding path，sprite 名稱留空", this);

            foreach (var usage in usages)
            {
                if (!PromptIconMapEditorUtility.IsLayoutOfFamily(usage._layout, family))
                    continue;

                var entry = _entries.FirstOrDefault(e =>
                    string.Equals(e._bindingPath, usage._path, System.StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                {
                    entry = new BindingIconEntry { _bindingPath = usage._path, _owner = this };
                    _entries.Add(entry);
                    report._added++;
                }

                entry._owner = this;

                if (string.IsNullOrEmpty(entry._spriteName))
                {
                    var suggestion = PromptSpriteNameSuggestion.Suggest(family, usage._path);
                    if (suggestion != null &&
                        (validSpriteNames.Count == 0 || validSpriteNames.Contains(suggestion)))
                    {
                        entry._spriteName = suggestion;
                        report._filledSpriteName++;
                    }
                    else
                    {
                        report._unmatched.Add($"{usage._path}（{usage._actionName}）");
                    }
                }

                if (entry._icon != null)
                    continue;

                var sprite = entry.ResolveSpriteFromTmpAsset();
                if (sprite == null)
                    continue;
                entry._icon = sprite;
                report._filledIcon++;
            }

            _lookup = null; //表變了，查表要重建
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            return report;
        }

        private bool HasOwnerFamily => PromptIconMapEditorUtility.TryFindOwnerFamily(this, out _);
    }
}
#endif
