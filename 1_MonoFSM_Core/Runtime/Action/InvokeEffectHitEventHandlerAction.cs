using MonoFSM.Core;
using MonoFSM.Runtime.Interact.EffectHit;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    /// <summary>
    /// 將事件轉發到外部引用的 EventHandler。
    /// </summary>
    public class InvokeEffectHitEventHandlerAction : AbstractStateAction,
        IArgEventReceiver<GeneralEffectHitData>
    {
        [DropDownRef] [SerializeField] private ManualEventHandler _targetHandler;

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
    }
}
