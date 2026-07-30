using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGInputAction
{
    //把 IHintSpriteFinder（ex: PromptIconRegistry / DeviceIconMapConfig）注入 InputPromptUIData 的靜態 finder，場景裡放一顆即可
    //Unity 不能序列化 interface 欄位，所以用 ScriptableObject 欄位 + runtime cast
    public class HintSpriteFinderInstaller : MonoBehaviour, ISceneAwake
    {
        [Required]
        [SerializeField]
        [ValidateInput(nameof(ValidateFinder), "必須是實作 IHintSpriteFinder 的 ScriptableObject")]
        private ScriptableObject _finder;

        private bool ValidateFinder(ScriptableObject finder)
        {
            return finder == null || finder is IHintSpriteFinder;
        }

        public void EnterSceneAwake()
        {
            InputPromptUIData.SetSpriteFinder(_finder as IHintSpriteFinder);
        }
    }
}
