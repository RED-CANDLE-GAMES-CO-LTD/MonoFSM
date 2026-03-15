using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    //FIXME: HitData裡應該放的是這個？這樣才可以拿到細節？
    //概念類似HitBoxTarget
    public abstract class BaseEffectDetectTarget : AbstractDescriptionBehaviour //實作
    {
        protected override bool IsIgnoreRename => true;

        protected override void Start()
        {
            base.Start();
            if (_detectable == null)
            {
                _detectable = GetComponentInParent<EffectDetectable>();
                if (_detectable == null)
                    Debug.LogError(
                        "BaseEffectDetectTarget requires an EffectDetectable component on the same GameObject.",
                        this
                    );
            }
        }

        [GUIColor(0.4f, 0.8f, 0.5f)]
        [ShowInInspector]
        [Required]
        [AutoParent] private EffectDetectable _detectable;

        public EffectDetectable Detectable => _detectable; //動態生成的沒有綁定到？
        // public GeneralEffectReceiver[] EffectReceivers => _detectable.EffectReceivers;
    }
}
