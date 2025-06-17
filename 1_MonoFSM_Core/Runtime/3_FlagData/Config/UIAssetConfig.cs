using UnityEngine;

namespace MonoFSM.Core
{
    [CreateAssetMenu(fileName = "UIAssetConfig", menuName = "ScriptableObjects/UIAssetConfig", order = 1)]
    public class UIAssetConfig : AddressableSOSingleton<UIAssetConfig>

    {
        public Sprite EmptySprite;
    }
}