using _1_MonoFSM_Core.Runtime._3_FlagData;
using MonoFSM.Localization;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGInputAction
{
    //一則操作提示：對應的 input action + 多語 prefix/postfix + 依裝置切換的 icon
    [CreateAssetMenu(
        menuName = "MonoFSM/Input/InputPromptUIData",
        fileName = "InputPromptUIData",
        order = 0
    )]
    public class InputPromptUIData : AbstractSOConfig
    {
        private static IHintSpriteFinder _spriteFinder;

        //看專案定義，ex: DeviceIconMapConfig（由 HintSpriteFinderInstaller 注入）
        public static void SetSpriteFinder(IHintSpriteFinder finder)
        {
            _spriteFinder = finder;
        }

        [FormerlySerializedAs("input")]
        [Required]
        public InputActionData _input;

        [FormerlySerializedAs("prompt_prefix")]
        public LocalizedString _promptPrefix;

        [FormerlySerializedAs("prompt_postfix")]
        public LocalizedString _promptPostfix;

        //找不到對照 icon 時的替代圖
        [FormerlySerializedAs("placeHolderIcon")]
        public Sprite _placeHolderIcon;

        public Sprite GetIcon()
        {
            if (_spriteFinder != null)
            {
                var icon = _spriteFinder.GetIcon(_input);
                if (icon != null)
                    return icon;
            }

            return _placeHolderIcon;
        }

        public string PrefixText => _promptPrefix.ToString();
        public string PostfixText => _promptPostfix.ToString();
    }

    public interface IHintSpriteFinder
    {
        public Sprite GetIcon(InputActionData input);
    }
}
