using MonoFSM.EditorExtension;
using MonoFSM.Runtime.Interact.EffectHit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    //IEventInvoker?
    public interface IActionParent //給GameObject結構Validate用的
    { }

    //FIXME: 這個其實沒有想要implement actionExecute? 要把Implement override蓋掉？
    public abstract class AbstractArgEventHandler<T>
        : AbstractStateAction,
            IArgEventReceiver<T>,
            IHierarchyValueInfo
    // where T : IEffectHitData
    {
        [AutoParent] [ShowInInspector] protected
            EffectResolver _effectResolver;
        void IArgEventReceiver<T>.ArgEventReceived(T arg)
        {
            //FIXME: 要做delay嗎？
            AddEventRecord();
            OnArgEventReceived(arg);
        }

        protected abstract void OnArgEventReceived(T arg);
#if UNITY_EDITOR
        public string ValueInfo => "evt:" + lastEventReceivedRecord;
        public bool IsDrawingValueInfo => _lastEventReceivedRecords.Count > 0;
#else
        public string ValueInfo => "";
        public bool IsDrawingValueInfo => false;
#endif
    }
}
