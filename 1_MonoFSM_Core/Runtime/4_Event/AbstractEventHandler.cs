using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core
{
    //各種事件的進入節點
    //ex: OnStateEnter, OnStateUpdate, OnStateExit
    //ex: OnEffectEnter, OnEffectExit
    //ex: OnPointerClick

    /// <summary>
    /// An abstract class that handles events and distributes them to registered event receivers.
    /// </summary>
    /// <remarks>
    /// This class is responsible for managing a collection of <see cref="IEventReceiver"/> components
    /// and triggering their event handling methods when an event occurs. It automatically finds
    /// and registers child event receivers through the <see cref="CompRef"/> and <see cref="AutoChildren"/>
    /// attributes.
    /// </remarks>
    /// <seealso cref="IEventReceiver"/>
    /// <seealso cref="IEventReceiver{T}"/>
    /// <seealso cref="IActionParent"/>
    public abstract class AbstractEventHandler : MonoBehaviour, IActionParent
    {
        [CompRef] [AutoChildren(DepthOneOnly = true)]
        protected IEventReceiver[] _eventReceivers; //IActions

        /// <summary>
        /// Call all event receivers' <see cref="IEventReceiver.EventReceived"/> method.
        /// </summary>
        public virtual void EventHandle()
        {
            if (!isActiveAndEnabled)
                return;
            _lastEventHandledTime = Time.time;
            foreach (var eventReceiver in _eventReceivers)
            {
                if (eventReceiver.IsValid)
                    eventReceiver.EventReceived();
            }
        }

        [PreviewInInspector] private float _lastEventHandledTime = -1f;

        /// <summary>
        /// Call all event receivers' <see cref="IEventReceiver{T}.EventReceived"/> method with the given argument.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="arg">The argument to pass to the event receivers.</param>
        public virtual void EventHandle<T>(T arg)
        {
            if (!isActiveAndEnabled)
                return;
            _lastEventHandledTime = Time.time;
            foreach (var eventReceiver in _eventReceivers)
            {
                //有參數的介面時
                if (eventReceiver is IArgEventReceiver<T> argEventReceiver)
                {
                    if (argEventReceiver.IsValid)
                        argEventReceiver.ArgEventReceived(arg);
                }
                else
                {
                    eventReceiver.EventReceived();
                }
            }
                
        }
    }
}