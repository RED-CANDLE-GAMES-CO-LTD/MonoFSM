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
            if (_detectable == null && _detectableOverride == null)
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
        [AutoParent] private EffectDetectable _detectable;

        /// <summary>
        /// 跨 hierarchy 指定 Detectable：detect target 掛在美術／動畫節點底下，但邏輯上的
        /// <see cref="EffectDetectable" />（連同它的 receiver）住在另一顆模組 prefab 裡時用這顆。
        /// 例：拆件系統的 anchor 生成在宿主的 view mesh 子節點上（要跟著骨骼動畫走），
        /// receiver 則在 <c>[Module] 拆件 Dismantle Parts</c> 裡。
        ///
        /// 為什麼不能直接手填 <c>_detectable</c>：它是 <c>[AutoParent]</c>，而 AutoAttributeManager
        /// 每次跑（pool 生成、編輯期 description 更新）都是**無條件覆寫**，
        /// 手填的值會被往上找的結果（通常是 null）蓋掉。所以只能另開一顆不帶 Auto 的序列化欄位。
        /// </summary>
        [Tooltip("邏輯上的 EffectDetectable 不在自己的祖先鏈上時指過去（例：anchor 生在美術節點底下、receiver 在模組裡）。留空 = 用往上找到的那顆。")]
        [SerializeField] private EffectDetectable _detectableOverride;

        public EffectDetectable Detectable =>
            _detectableOverride != null ? _detectableOverride : _detectable;

        protected override bool HasError()
        {
            return base.HasError() || Detectable == null;
        }
        // public GeneralEffectReceiver[] EffectReceivers => _detectable.EffectReceivers;
    }
}
