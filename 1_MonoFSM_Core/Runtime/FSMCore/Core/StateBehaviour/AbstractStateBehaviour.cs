using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using Fusion.Addons.FSM;
using MonoFSM_Core.Runtime.StateBehaviour;
using MonoFSM.Core.Attributes;
using MonoFSM.Editor;
using MonoFSM.Runtime;
using MonoFSM.Variable.Attributes;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace MonoFSM.Core
{
    //FIXME: TState還有意義嗎？直接確定是 MonoBehaviourState就好？
    public abstract class AbstractStateBehaviour<TState>
        : MonoBehaviour,
            IState,
            IOwnedState<TState>
        where TState : AbstractStateBehaviour<TState>
    {
        // PUBLIC MEMBERS

        [ShowInPlayMode]
        public int StateId { get; set; }
        public StateMachine<TState> Machine { get; set; }
        public virtual string Name => gameObject.name;
        public int Priority => _priority;
        public float StateTime => _localStateTime;
        private float _localStateTime;

        public StateMachineLogic context => _folder.bindingContext;
        [AutoParent] private StateFolder _folder;


        public MonoEntity ParentEntity =>
            context.ParentEntity; //extension method一路往上問？ vs直接GetComponentInParent?

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private CanEnterNode _canEnterNode;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private CanExitNode _canExitNode;

        public float DeltaTime => context.DeltaTime;

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
            return _canEnterNode.FinalResult;
        }

        protected virtual bool CanExitState(TState nextState)
        {
            return true;
        }

        protected virtual void OnEnterState() { }

        protected virtual void OnFixedUpdate() { }

        protected virtual void OnExitState() { }

        protected virtual void OnEnterStateRender() { }

        protected virtual void OnRender() { }

        protected virtual void OnExitStateRender() { }

        protected virtual void OnCollectChildStateMachines(List<IStateMachine> stateMachines) { }

        // IState INTERFACE

        void IState.OnFixedUpdate()
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

            _localStateTime += DeltaTime;
            if (_transitions != null)
                foreach (var t in _transitions)
                {
                    if (!t.isActiveAndEnabled)
                        continue;

                    if (CanTransition(ref t._transitionData) == true)
                    {
                        //try catch 抓問題？
                        if (Machine.TryActivateState(t.TargetState))
                            return;
                    }
                }

            //anyState? 放最後？其他優先嗎
            if (context.anyState != null)
            {
                var transitions = context.anyState._transitions;
                foreach (var t in transitions)
                {
                    if (!t.isActiveAndEnabled)
                        continue;
                    if (context.anyState.CanTransition(ref t._transitionData))
                        // Debug.Log($"[{Name}] ForceActivateState to {transition.TargetState.Name}", this);
                        if (Machine.TryActivateState(t.TargetState))
                            return;
                }
            }

            OnFixedUpdate();
        }

        bool IState.CanExitState(IState nextState, bool isExplicitDeactivation)
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

        void IState.Initialize()
        {
            OnInitialize();
        }

        void IState.Deinitialize(bool hasState)
        {
            OnDeinitialize(hasState);
        }

        bool IState.CanEnterState()
        {
            return CanEnterState();
        }

        void IState.OnEnterState()
        {
            _localStateTime = 0f;
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

        void IState.OnExitState()
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

        void IState.OnEnterStateRender()
        {
            OnEnterStateRender();
            foreach (var renderAction in _renderActions) //FIXME: 條件？
            {
                if (renderAction.isActiveAndEnabled)
                    renderAction.OnEnterRender();
            }
        }

        void IState.OnRender()
        {
            OnRender();
            foreach (var renderAction in _renderActions)
                if (renderAction.isActiveAndEnabled)
                    renderAction.OnRender();
        }

        void IState.OnExitStateRender()
        {
            OnExitStateRender();
            //FIXME: 需要這個嗎？
        }

        //FIXME: 先把childMachines拔掉？
        // IStateMachine[] IState.ChildMachines { get; set; }
        void IState.CollectChildStateMachines(List<IStateMachine> stateMachines)
        {
            OnCollectChildStateMachines(stateMachines);
        }

        // PRIVATE METHODS

        private bool CanTransition(ref TransitionData<TState> transition)
        {
            if (transition.TargetState == null)
                Debug.LogError($"Transition target state is null in {Name} to {transition}", this);
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
