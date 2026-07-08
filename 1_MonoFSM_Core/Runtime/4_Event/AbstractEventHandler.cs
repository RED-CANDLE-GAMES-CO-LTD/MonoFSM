using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
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
    public abstract class AbstractEventHandler : AbstractDescriptionBehaviour, IActionParent,
        IResetStateRestore, IRenderInvoker
    {
        protected override string DescriptionTag => "Event";

        public override string Description => GetType().Name.Replace("Handler", "");

        // GetType().Name.Replace("Handler", ""); //要都叫做 OnXXX ?
        //FIXME: 先做一個繞開
        public bool
            _forceExecuteWithoutStateAuthority;
        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        protected IEventReceiver[] _eventReceivers; //IActions

        [CompRef] [ShowInInspector] [AutoChildren(DepthOneOnly = true)]
        protected IRenderBehaiour[] _renderActions;

        [CompRef] [ShowInInspector] [Auto] protected IRenderSyncProvider _renderSyncProvider;

        //陣列版 render sync（NetworkEventVisualSyncArray）在 Spawned 時注入，優先權低於同物件的 _renderSyncProvider
        private IRenderSyncHub _renderSyncHub;
        public void SetRenderSyncHub(IRenderSyncHub hub) => _renderSyncHub = hub;

        /// <summary>
        /// 是否為 Simulate 階段觸發的 handler（FUN 中觸發、被 StateAuthority gate）。
        /// 這類 handler 的 Render 表現在 proxy 上收不到，需要 render sync 網路同步。
        /// Render 驅動的 handler（OnStateEnterRenderHandler 等）覆寫為 false。
        /// </summary>
        public virtual bool IsSimulateEventHandler => true;

        [InfoBox("目前不是所有EntityProvider都是合法的喔")]
        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        //FIXME: 要有篩選機制？靠Drawer去找囉？
        private AbstractEntityProvider[] _entityProviders;

        public void EnterRenderInvoke()
        {
            _lastRenderEventTime = Time.time;
            //如果有T可以自己留著？好像不行...沒地方接 object 硬轉
            foreach (var action in _renderActions)
            {
                action.OnEnterRender();
            }
        }

        public void EnterArgRenderInvoke<T>(T arg)
        {
            _lastRenderEventTime = Time.time;
            if (_renderActions == null)
                return;
            foreach (var action in _renderActions)
            {
                if (action is IArgRenderBehaviour<T> argAction)
                {
                    argAction.OnArgEnterRender(arg);
                }
                else
                {
                    action.OnEnterRender();
                }
            }
        }



        [PreviewInDebugMode] protected float _lastSimulateEventTime = -1f;

        [PreviewInDebugMode] protected float _lastRenderEventTime = -1f;

        //FIXME: override怎麼處理？
        private void EventHandleImplement<T>(T arg, bool ignoreArg = false)
        {
            if (_conditionFolder.IsValid == false)
                return;
            if (_parentObj.IsCulling) //FIXME: 有需要分visual和logic culling?
                return;
            if (!gameObject.activeSelf)
                return;

            // 如果有掛載網路同步組件，就交由它接管 Render 觸發 (這解決了 Proxy 沒特效與本地重複觸發的問題)
            if (_renderSyncProvider != null)
            {
                if (ignoreArg)
                    _renderSyncProvider.RequestRenderSync();
                else
                    _renderSyncProvider.RequestRenderSync(arg);
            }
            else if (_renderSyncHub != null) //root 上的陣列版 render sync
            {
                if (ignoreArg)
                    _renderSyncHub.RequestRenderSync(this);
                else
                    _renderSyncHub.RequestRenderSync(this, arg);
            }
            else
            {
                if (ignoreArg)
                {
                    EnterRenderInvoke();
                }
                else
                {
                    EnterArgRenderInvoke(arg);
                }
            }

            if (_parentObj == null)
            {
                Debug.LogError("No ParentObj" + name, this);
            }
            if (!_parentObj.HasStateAuthority && !_forceExecuteWithoutStateAuthority)
                return;
            _lastSimulateEventTime = Time.time;
            foreach (var eventReceiver in _eventReceivers)
            {
                try
                {
                    //有參數的介面時
                    if (!ignoreArg && eventReceiver is IArgEventReceiver<T> argEventReceiver)
                    {
                        if (argEventReceiver.IsValid)
                            argEventReceiver.ArgEventReceived(arg); //在這裡delay?
                    }
                    else
                    {
                        if (eventReceiver.IsValid)
                            eventReceiver.EventReceived(); //在這裡delay?
                    }
                }
                catch (System.Exception e) //因為eventhandle有error會導致後面觸發都壞掉
                {
                    Debug.LogError(
                        $"Exception occurred while handling event in {eventReceiver.GetType().Name}: {e.Message}\n{e.InnerException?.Message}\n{e.StackTrace}",
                        eventReceiver as Object);
                }
            }

        }

        /// <summary>
        /// Call all event receivers' <see cref="IEventReceiver{T}.EventReceived"/> method with the given argument.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="arg">The argument to pass to the event receivers.</param>
        public void EventHandle<T>(T arg)
        {
            //FIXME:會需要condition嗎?
            // if (!isActiveAndEnabled) //哇....整個關掉就沒了...要開洞嗎？還是要保持關掉就不觸發？
            //     return;

            EventHandleImplement(arg);
        }

        /// <summary>
        /// Call all event receivers' <see cref="IEventReceiver.EventReceived"/> method.
        /// </summary>
        public virtual void EventHandle()
        {
            EventHandleImplement(0, true);
        }
        [SerializeField] private ConditionGroup _conditionFolder;
        public void ResetStateRestore(bool isHardReset)
        {
            _lastSimulateEventTime = -1;
            _lastRenderEventTime = -1;
        }
    }
}
