using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.FSM;
using MonoDebugSetting;
using MonoFSM_Core.Runtime.StateBehaviour;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Editor;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace MonoFSM.Core
{
    //FIXME: TState還有意義嗎？直接確定是 MonoBehaviourState就好？
    public abstract class AbstractStateBehaviour<TState>
        : AbstractDescriptionBehaviour,
            IMonoState,
            IOwnedState<TState>, IRenderInvoker,
            ILastTransitionRecord
        where TState : AbstractStateBehaviour<TState>
    {
        // PUBLIC MEMBERS
        public override string Description => ReformatedName;

        protected override string DescriptionTag => "State";


        [ShowInPlayMode]
        public int StateId { get; set; }
        public StateMachine<TState> Machine { get; set; }
        public virtual string Name => gameObject.name;
        public int Priority => _priority;
        public float StateTime => Machine == null
            ? 0f
            : (WorldUpdateSimulator.CurrentTick - Machine.StateChangeTick) *
              WorldUpdateSimulator.DeltaTime;

        [AutoParent] MonoFSMOwner _owner;
        public MonoFSMOwner Owner => _owner;

        StateFolder bindingFolder => _parentfolder.bindingRootFolder
            ? _parentfolder.bindingRootFolder
            : _parentfolder;

        [AutoParent] protected StateFolder _parentfolder;


        public MonoEntity ParentEntity => BindEntity;
        // context.ParentEntity; //extension method一路往上問？ vs直接GetComponentInParent?

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private CanEnterNode _canEnterNode;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private CanExitNode _canExitNode;


        //  PRIVATE MEMBERS

        [SerializeField]
        private int _priority = 0;

        [SerializeField]
        private bool _checkPriorityOnExit = true;

        // private List<TransitionData<TState>> _transitions;

        // [AutoChildren] private AbstractStateAction[] _actions;

        // [CompRef] [AutoChildren] private TransitionBehaviour<TState>[] _transitions;
        [CompRef]
        [AutoChildren]
        private TransitionBehaviour<TState>[] _transitions;

        // public StateTransition[] Transitions => transitions;
        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private IRenderBehaiour[] _renderActions;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private OnStateEnterHandler _onStateEnter;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private OnStateUpdateHandler _onStateUpdate;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private OnStateExitHandler _onStateExit;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private OnStateEnterRenderHandler _onStateEnterRender;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private OnStateExitRenderHandler _onStateExitRender;

        // Support for direct AbstractStateLifeCycleHandler children
        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private AbstractStateLifeCycleHandler[] _lifeCycleHandlers;

        //FIXME: EnterStateRender

        // PUBLIC METHODS

        // public void AddTransition(TransitionData<TState> transition)
        // {
        //     if (_transitions == null) _transitions = new List<TransitionData<TState>>(16);
        //
        //     _transitions.Add(transition);
        // }

        // PROTECTED METHODS

        protected virtual void OnInitialize() { }

        protected virtual void OnDeinitialize(bool hasState) { }

        protected virtual bool CanEnterState()
        {
            if (!gameObject.activeSelf) //關著不可以
                return false;
            if (_canEnterNode == null)
                return true;
            var result = _canEnterNode.FinalResult;
            if (result)
            {
                this.Log("Can Enter State: ", Name);
            }

            return result;
        }

        protected virtual bool CanExitState(TState nextState)
        {
            return true;
        }

        protected virtual void OnEnterState() { }

        protected virtual void OnFixedUpdate() { }

        protected virtual void OnExitState() { }

        //FIXME: 要實作這個
        protected virtual void OnEnterStateRender() { }

        protected virtual void OnRender() { }

        protected virtual void OnExitStateRender() { }

        protected virtual void OnCollectChildStateMachines(List<IMonoStateMachine> stateMachines)
        {
        }

        // IState INTERFACE

        void IMonoState.OnFixedUpdate()
        {
            // Traditional Handler approach
            _onStateUpdate?.EventHandle();

            // New LifeCycleHandler approach
            if (_lifeCycleHandlers != null)
            {
                foreach (var handler in _lifeCycleHandlers)
                {
                    if (handler != null && handler.isActiveAndEnabled)
                        handler.TriggerStateUpdate();
                }
            }

            if (_transitions != null)
                foreach (var t in _transitions)
                {
                    if (!t.isActiveAndEnabled)
                        continue;

                    if (CanTransition(ref t._transitionData))
                    {
                        //try catch 抓問題？
                        RecordLastTransition(t);
                        if (Machine.TryActivateState(t.TargetState))
                            return;
                    }
                }

            //anyState? 放最後？其他優先嗎
            foreach (var anyState in bindingFolder.AllAnyStates)
            {
                var transitions = anyState._transitions;
                foreach (var t in transitions)
                {
                    if (!t.isActiveAndEnabled)
                        continue;
                    if (anyState.CanTransition(ref t._transitionData))
                    {
                        if (t.TargetState == this)
                            continue; //anyState不應該轉自己，避免無限迴圈)
                        // Debug.Log($"anyState ForceActivateState to {t.TargetState.Name} with " + t,
                        //     t);
                        //記在「現在這個 state」上，log 的 previous state 才查得到
                        RecordLastTransition(t);
                        if (Machine.TryActivateState(t.TargetState))
                            return;
                    }

                }
            }

            OnFixedUpdate();
        }
        [ShowInInspector]
        private AbstractDescriptionBehaviour _lastTransition;

        //最後一次通過 transition 條件的 tick，用來分辨「這次 state change 是不是走 transition 來的」
        private int _lastTransitionTick = int.MinValue;

        private void RecordLastTransition(AbstractDescriptionBehaviour transition)
        {
            _lastTransition = transition;
            _lastTransitionTick = Machine?.TickProvider?.Tick ?? WorldUpdateSimulator.CurrentTick;
        }

        //只在 state change log 時被呼叫，允許組字串
        public string GetLastTransitionInfo(int currentTick)
        {
            if (_lastTransition == null)
                return "無 transition 紀錄(直接 ActivateState)";

            if (_lastTransitionTick != currentTick)
                return $"非 transition 觸發(直接 ActivateState)，最後一次 transition: {_lastTransition.name}@tick{_lastTransitionTick}";

            return $"{_lastTransition.name} [{_lastTransition.GetType().Name}]";
        }

        bool IMonoState.CanExitState(IMonoState nextState, bool isExplicitDeactivation)
        {
            // During explicit deactivation (e.g. when user specifically calls TryDeactivateState) priority is not checked
            if (
                isExplicitDeactivation == false
                && _checkPriorityOnExit == true
                && (nextState as TState).Priority < _priority
            )
                return false;

            return CanExitState(nextState as TState);
        }

        void IMonoState.Initialize()
        {
            OnInitialize();
        }

        void IMonoState.Deinitialize(bool hasState)
        {
            OnDeinitialize(hasState);
        }

        bool IMonoState.CanEnterState()
        {
            return CanEnterState();
        }

        void IMonoState.OnEnterState()
        {
            OnEnterState();

            // Traditional Handler approach
            _onStateEnter?.EventHandle();

            // New LifeCycleHandler approach
            if (_lifeCycleHandlers != null)
            {
                foreach (var handler in _lifeCycleHandlers)
                {
                    if (handler != null && handler.isActiveAndEnabled)
                        handler.TriggerStateEnter();
                }
            }

#if UNITY_EDITOR
            EditorFsmEventManager.NotifyStateChanged(Machine.Logic);
#endif
        }

        void IMonoState.OnExitState()
        {
            OnExitState();

            // Traditional Handler approach
            _onStateExit?.EventHandle();

            // New LifeCycleHandler approach
            if (_lifeCycleHandlers != null)
            {
                foreach (var handler in _lifeCycleHandlers)
                {
                    if (handler != null && handler.isActiveAndEnabled)
                        handler.TriggerStateExit();
                }
            }
        }

        void IMonoState.OnEnterStateRender()
        {
            OnEnterStateRender();
            foreach (var renderAction in _renderActions) //FIXME: 條件？
            {
                if (renderAction.isActiveAndEnabled)
                    renderAction.OnEnterRender();
            }

            if (_onStateEnterRender != null && _onStateEnterRender.isActiveAndEnabled)
                _onStateEnterRender.EnterRenderInvoke();
        }

        void IMonoState.OnRender()
        {
            OnRender();
            foreach (var renderAction in _renderActions)
                if (renderAction.isActiveAndEnabled)
                    renderAction.OnRender();
        }

        void IMonoState.OnExitStateRender()
        {
            OnExitStateRender();

            if (_onStateExitRender != null && _onStateExitRender.isActiveAndEnabled)
                _onStateExitRender.EnterRenderInvoke();
        }

        //FIXME: 先把childMachines拔掉？
        // IStateMachine[] IState.ChildMachines { get; set; }
        void IMonoState.CollectChildStateMachines(List<IMonoStateMachine> stateMachines)
        {
            OnCollectChildStateMachines(stateMachines);
        }

        // PRIVATE METHODS

        private bool CanTransition(ref TransitionData<TState> transition)
        {
            if (transition.TargetState == null)
            {
                if (RuntimeDebugSetting.IsDebugMode)
                    Debug.LogError($"Transition target state is null in {Name} to {transition}",
                        this);
                return false;
            }
            // try
            // {
            if (!transition.Transition(this as TState, transition.TargetState))
                return false;
            // // }
            // // catch (Exception e)
            // // {
            //     Debug.LogError(
            //         $"Transition failed from {Name} to {transition.TargetState.Name}: {e.Message}{e.StackTrace}", this);
            //     return false;
            // }
            // if (transition.IsForced == true)
            //     return true;


            //FIXME: 這裡也判了？
            if (CanExitState(transition.TargetState) == false)
                return false;


            if (transition.TargetState.CanEnterState() == false)
                return false;
            // Debug.Log($"Can Transitioning from {Name} to {transition.TargetState.Name}", this);
            return true;
        }
    }
}
