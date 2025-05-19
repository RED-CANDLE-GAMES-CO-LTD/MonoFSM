using MonoFSM.Localization;
using UnityEngine;


namespace RCGInputAction
{
    [CreateAssetMenu(menuName = "RCG/Input/InputPromptUIData", fileName = "InputPromptUIData", order = 0)]
    public class InputPromptUIData : DescriptableData
    {
        public InputActionData input;
        public LocalizedString prompt_prefix;
        public LocalizedString prompt_postfix;
        public Sprite placeHolderIcon;

        public Sprite GetIcon()
        {
            Debug.LogError("要實作這個QQ");
        
            return placeHolderIcon;
        }
    }

}

