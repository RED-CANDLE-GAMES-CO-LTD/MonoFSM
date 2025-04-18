using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core
{
    //各種事件的進入節點
    //ex: OnStateEnter, OnStateUpdate, OnStateExit
    //ex: OnEffectEnter, OnEffectExit
    //ex: OnPointerClick
    //還是不需要interface, 應該直接用abstractClass?
    public interface IEventHandler //IEffectReceivedHandler?
    {
        // void HandleEvent(IEvent e);
    }

    public abstract class AbstractEventHandler : MonoBehaviour, IEventHandler, IActionParent
    {
        [CompRef] [AutoChildren(DepthOneOnly = true)]
        protected IEventReceiver[] _eventReceivers;

        public void EventHandle()
        {
            foreach (var eventReceiver in _eventReceivers)
                if (ShouldHandleEvent(eventReceiver))
                    eventReceiver.EventReceived();
        }

        public void EventHandle<T>(T arg)
        {
            foreach (var eventReceiver in _eventReceivers)
                if (ShouldHandleEvent(eventReceiver))
                    eventReceiver.EventReceived(arg);
        }

        protected virtual bool ShouldHandleEvent(IEventReceiver eventReceiver)
        {
            return eventReceiver.isActiveAndEnabled;
        }
    }
}