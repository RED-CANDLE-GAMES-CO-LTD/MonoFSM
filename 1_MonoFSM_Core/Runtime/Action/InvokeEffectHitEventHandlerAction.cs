using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Interact.EffectHit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    /// <summary>
    /// 將事件轉發到外部引用的 EventHandler。
    /// Simulate 路徑走 target 的 EventHandle()（condition 由 EventHandleImplement 把關）；
    /// Render 路徑是 render tier 直接打進來的，不經過 EventHandleImplement，
    /// 所以要自己補回 condition gate，否則 condition 不成立時 target 底下的 [Render] 照樣會播。
    /// </summary>
    public class InvokeEffectHitEventHandlerAction : AbstractStateAction,
        IArgEventReceiver<GeneralEffectHitData>, IRenderBehaiour
    {
        [DropDownRef] [SerializeField] private ManualEventHandler _targetHandler;

        public enum RenderSkipReason
        {
            Invoked, //正常轉發出去
            NoTarget,
            SelfConditionFailed, //自己節點下的 [Condition] 不成立（對齊 simulate 路徑的 IsValid）
            TargetConditionFailed, //target handler 自己的 [If] 不成立
        }

        [ShowInInspector] [PreviewInDebugMode] //PreviewInDebugMode 本身已帶 DisableIf，不用再掛 ReadOnly
        private RenderSkipReason _lastRenderSkipReason;

        protected override void OnActionExecuteImplement()
        {
            if (_targetHandler != null)
                _targetHandler.EventHandle();
        }

        public void ArgEventReceived(GeneralEffectHitData arg)
        {
            if (_targetHandler != null)
                _targetHandler.EventHandle(arg);
        }

        public void OnEnterRender()
        {
            if (_targetHandler == null)
            {
                _lastRenderSkipReason = RenderSkipReason.NoTarget;
                return;
            }

            //simulate 路徑是 eventReceiver.IsValid 把關，render 路徑補上同一道
            if (!IsValid)
            {
                _lastRenderSkipReason = RenderSkipReason.SelfConditionFailed;
                return;
            }

            if (!_targetHandler.IsConditionValid)
            {
                _lastRenderSkipReason = RenderSkipReason.TargetConditionFailed;
                return;
            }

            _lastRenderSkipReason = RenderSkipReason.Invoked;
            _targetHandler.EnterRenderInvoke();
        }

        public void OnRender()
        {
        }
    }
}
