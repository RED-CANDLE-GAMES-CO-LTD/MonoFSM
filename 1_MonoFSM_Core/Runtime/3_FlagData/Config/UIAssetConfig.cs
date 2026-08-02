using _1_MonoFSM_Core.Runtime._3_FlagData;
using UnityEngine;

namespace MonoFSM.Core
{
    [CreateAssetMenu(fileName = "UIAssetConfig", menuName = "ScriptableObjects/UIAssetConfig", order = 1)]
    public class UIAssetConfig : GameFlagBase //AddressableSOSingleton<UIAssetConfig>

    {
        public static UIAssetConfig i;
        public Sprite EmptySprite;

        public override void FlagAwake(TestMode mode)
        {
            base.FlagAwake(mode);
            i = this;
        }
    }
}
