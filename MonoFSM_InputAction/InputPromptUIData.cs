using MonoFSM.Localization;
using UnityEngine;


namespace RCGInputAction
{
    [CreateAssetMenu(menuName = "RCG/Input/InputPromptUIData", fileName = "InputPromptUIData", order = 0)]
    public class InputPromptUIData : GameFlagBase
    {
        private static IHintSpriteFinder _spriteFinder;
        
        //看專案定義
        public static void SetSpriteFinder(IHintSpriteFinder finder)
        {
            _spriteFinder = finder;
        } 
        
        
        public InputActionData input;
        public LocalizedString prompt_prefix;
        public LocalizedString prompt_postfix;
        public Sprite placeHolderIcon;

        public Sprite GetIcon()
        {
            if (_spriteFinder != null)
                return _spriteFinder.GetIcon(input);
            return placeHolderIcon;
        }
    }

    public interface IHintSpriteFinder
    {
        public Sprite GetIcon(InputActionData input);
    }

}

