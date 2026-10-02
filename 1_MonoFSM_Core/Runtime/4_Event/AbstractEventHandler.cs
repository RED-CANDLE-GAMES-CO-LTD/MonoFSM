using System.Diagnostics;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using AbstractEntitySource = MonoFSM.Core.Runtime.AbstractEntitySource;
using Debug = UnityEngine.Debug; //避免和 System.Diagnostics.Debug 撞名

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
            _forceExecuteWithoutStateAuthority; //FIXME: 被MonoObj擋掉囉

        [Tooltip(
            "勾選後此 handler 的所有 action 只在 StateAuthority 執行；client 端（含 InputAuthority）完全不跑，" +
            "視覺表現交給 render sync（NetworkEventVisualSync）從網路觸發")]
        public bool _stateAuthorityOnly;
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

        /// <summary>
        /// 一次性初始化事件（OnResetStartHandler）覆寫為 true：parent MonoObj 被 simulation culling 時照樣觸發。
        /// 開場 WorldInit 時玩家還沒生成、遠方物件一定是 cull 狀態，不跳過的話開場那發初始化永遠跑不到。
        /// 其他 gate（inactive、condition、authority）照舊。
        /// </summary>
        protected virtual bool IgnoreCulling => false;

        [InfoBox("目前不是所有EntityProvider都是合法的喔")]
        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        //FIXME: 要有篩選機制？靠Drawer去找囉？
        private AbstractEntitySource[] _entityProviders;

        /// <summary>
        /// render sync（hub / 1:1）寫 entry 時呼叫：問直屬 render action 裡第一顆啟用中的
        /// IRenderSyncAmountProvider 要數值。呼叫點在 EventHandleImplement 的 simulate actions 之前，
        /// 所以讀得到這次 hit 的 hitEntity，也還沒被同一顆 event 底下的 despawn / consume 改掉。
        /// </summary>
        public bool TryGetRenderSyncAmount(out float amount)
        {
            amount = 0f;
            if (_renderActions == null)
                return false;
            for (var i = 0; i < _renderActions.Length; i++)
            {
                var action = _renderActions[i];
                if (action is IRenderSyncAmountProvider provider && action.isActiveAndEnabled)
                    return provider.TryGetRenderSyncAmount(out amount);
            }

            return false;
        }

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

            // Generic variance 不適用 value type。Fusion 的 RenderEventSyncData 是 struct，
            // 因此在一次性 VFX event 進入時 boxing 一次，讓 Core 的 IRenderHitData receiver 能消費它。
            // 這不在每-frame Render loop，且避免 Core 依賴任何網路 package payload 型別。
            var renderHitData = arg is IRenderHitData hitData ? hitData : null;
            foreach (var action in _renderActions)
            {
                if (action is IArgRenderBehaviour<T> argAction)
                {
                    if (argAction.isActiveAndEnabled)
                        argAction.OnArgEnterRender(arg);
                }
                else if (renderHitData != null && action is IArgRenderBehaviour<IRenderHitData> hitAction)
                {
                    if (hitAction.isActiveAndEnabled)
                        hitAction.OnArgEnterRender(renderHitData);
                }
                else
                {
                    if (action.isActiveAndEnabled)
                        action.OnEnterRender();
                }
            }
        }

        public override string ValueInfo =>
            _parentObj.IsCulling ? "Culled" : "Sim" + ShouldSimulate + _lastSkipReason;

        //提前顯示 重要資訊？_stateAuthorityOnly？_forceExecuteWithoutStateAuthority？
        public override bool IsDrawingValueInfo => Application.isPlaying;

        private bool ShouldSimulate => _stateAuthorityOnly
            ? _parentObj.HasStateAuthority
            :
            _parentObj.ShouldSimulte || _forceExecuteWithoutStateAuthority;
        [PreviewInDebugMode] protected float _lastSimulateEventTime = -1f;

        [PreviewInDebugMode] protected float _lastRenderEventTime = -1f;

        //事件被叫到了卻沒往下跑 action 時，記下是哪一道 gate 擋的（up peek / Inspector 都看得到）
        //全部用常數字串，不做 concat，不產生 GC
        [ShowInInspector] [PreviewInDebugMode] public string _lastSkipReason;

        [ShowInInspector] [PreviewInDebugMode] public float _lastSkipTime = -1f;

        //FsmTrace 去重用：同一個原因連續擋只記第一次（OnStateUpdate 這種每 tick 叫的才不會洗掉 buffer）
        [System.NonSerialized] private MonoFSM.FSM.FsmTraceSkipReason _lastTracedSkip;

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private void MarkSkipped(string reason, MonoFSM.FSM.FsmTraceSkipReason traceReason)
        {
#if UNITY_EDITOR
            _lastSkipReason = reason;
            _lastSkipTime = Time.time;
            this.Log(reason); //父層掛 DebugProvider 才會印
#endif
            if (traceReason != MonoFSM.FSM.FsmTraceSkipReason.None && MonoFSM.FSM.FsmTrace.Enabled && _lastTracedSkip != traceReason)
            {
                _lastTracedSkip = traceReason;
                MonoFSM.FSM.FsmTrace.RecordEventSkipped(this, traceReason);
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private void ClearSkipReason()
        {
#if UNITY_EDITOR
            _lastSkipReason = null;
#endif
            _lastTracedSkip = MonoFSM.FSM.FsmTraceSkipReason.None;
        }

        //FIXME: override怎麼處理？
        private void EventHandleImplement<T>(T arg, bool ignoreArg = false)
        {
            if (!gameObject.activeSelf)
            {
                MarkSkipped("gameObject inactive", MonoFSM.FSM.FsmTraceSkipReason.None); //不進 trace：inactive 節點是刻意關掉的，靜態讀 prefab 就看得到（~）
                return;
            }
            if (!IgnoreCulling && _parentObj.IsCulling) //FIXME: 有需要分visual和logic culling?
            {
                MarkSkipped("parentObj culling", MonoFSM.FSM.FsmTraceSkipReason.Culling);
                return;
            }

            if (_conditionFolder.IsValid == false)
            {
                MarkSkipped("condition invalid", MonoFSM.FSM.FsmTraceSkipReason.ConditionInvalid);
                return;
            }

            if (_stateAuthorityOnly && !_parentObj.HasStateAuthority)
            {
                MarkSkipped("stateAuthorityOnly: not state authority", MonoFSM.FSM.FsmTraceSkipReason.NotStateAuthority);
                return;
            }

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

            if (!ShouldSimulate)
            {
                //最常見的坑：非網路的場景物件沒人 push ShouldSimulte，事件會靜靜地不執行
                MarkSkipped("ShouldSimulate false (no state authority)", MonoFSM.FSM.FsmTraceSkipReason.NotSimulating);
                return;
            }

            ClearSkipReason(); //有跑到就清掉，避免看到過期的原因
            _lastSimulateEventTime = Time.time;
            foreach (var eventReceiver in _eventReceivers)
            {
                try
                {
                    //有參數的介面時
                    if (!ignoreArg && eventReceiver is IArgEventReceiver<T> argEventReceiver)
                    {
                        //FIXME: 沒有紀錄callback時間？
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

        [AutoNested]
        [SerializeField] private ConditionGroup _conditionFolder;

        /// <summary>
        /// 給「繞過 EventHandleImplement、直接呼 EnterRenderInvoke」的外部轉發者
        /// （例如 InvokeEffectHitEventHandlerAction 的 render 路徑）自己補上這道 gate 用，
        /// 否則 condition 不成立時 render 表現照樣會播、只有 action 被擋掉。
        /// </summary>
        public bool IsConditionValid => _conditionFolder.IsValid;
        public void ResetStateRestore(bool isHardReset)
        {
            _lastSimulateEventTime = -1;
            _lastRenderEventTime = -1;
        }
    }
}
