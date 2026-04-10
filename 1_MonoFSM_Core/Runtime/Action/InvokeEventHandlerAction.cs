using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    /// <summary>
    /// 將事件轉發到外部引用的 EventHandler。
    /// </summary>
    public class InvokeEventHandlerAction : AbstractStateAction, IArgEventReceiver<IEffectHitData>
    {
        [Required] //只限定manual?
        [SerializeField]
        private AbstractEventHandler _targetHandler;

        protected override void OnActionExecuteImplement()
        {
            if (_targetHandler != null)
                _targetHandler.EventHandle();
        }

        public void ArgEventReceived(IEffectHitData arg)
        {
            if (_targetHandler != null)
                _targetHandler.EventHandle(arg);
        }
    }
}
