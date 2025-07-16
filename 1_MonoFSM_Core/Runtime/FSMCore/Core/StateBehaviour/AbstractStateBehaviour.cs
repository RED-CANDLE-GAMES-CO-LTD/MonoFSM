using System;
using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using Fusion.Addons.FSM;
#if UNITY_EDITOR
using MonoFSM.Editor;
#endif
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core
{
    public abstract class AbstractStateBehaviour<TState> : MonoBehaviour, IState, IOwnedState<TState>
        where TState : AbstractStateBehaviour<TState>
    {
        // PUBLIC MEMBERS

        public int StateId { get; set; }
        public StateMachine<TState> Machine { get; set; }
        public virtual string Name => gameObject.name;
        public int Priority => _priority;
        public float StateTime => _localStateTime;
        private float _localStateTime;
        [AutoParent] protected StateMachineLogic _context;
        public float DeltaTime => _context.DeltaTime;
        //  PRIVATE MEMBERS

        [SerializeField] private int _priority = 0;
        [SerializeField] private bool _checkPriorityOnExit = true;

        // private List<TransitionData<TState>> _transitions;

        // [AutoChildren] private AbstractStateAction[] _actions;

        [CompRef] [AutoChildren] private TransitionBehaviour<TState>[] _transitions;

        // public StateTransition[] Transitions => transitions;
        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private IRenderAction[] _renderActions;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private OnStateEnterHandler _onStateEnter;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private OnStateUpdateHandler _onStateUpdate;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private OnStateExitHandler _onStateExit;

        // Support for direct AbstractStateLifeCycleHandler children
        [CompRef] [AutoChildren(DepthOneOnly = true)]
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

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnDeinitialize(bool hasState)
        {
        }

        protected virtual bool CanEnterState()
        {
            return true;
        }

        protected virtual bool CanExitState(TState nextState)
        {
            return true;
        }

        protected virtual void OnEnterState()
        {
        }

        protected virtual void OnFixedUpdate()
        {
        }

        protected virtual void OnExitState()
        {
        }

        protected virtual void OnEnterStateRender()
        {
        }

        protected virtual void OnRender()
        {
        }

        protected virtual void OnExitStateRender()
        {
        }

        protected virtual void OnCollectChildStateMachines(List<IStateMachine> stateMachines)
        {
        }

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
                    var transition = t;

                    if (CanTransition(ref t._transitionData) == true)
                    {
                        Debug.Log($"[{Name}] ForceActivateState to {transition.TargetState.Name}", this);
                        Machine.ForceActivateState(t.TargetState);
                        return;
                    }
                }

            OnFixedUpdate();
        }

        bool IState.CanExitState(IState nextState, bool isExplicitDeactivation)
        {
            // During explicit deactivation (e.g. when user specifically calls TryDeactivateState) priority is not checked
            if (isExplicitDeactivation == false && _checkPriorityOnExit == true &&
                (nextState as TState).Priority < _priority)
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
                renderAction.OnEnterRender();
        }

        void IState.OnRender()
        {
            OnRender();
            foreach (var renderAction in _renderActions) renderAction.OnRender();
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
            try
            {
                if (transition.Transition(this as TState, transition.TargetState) == false)
                    return false;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Transition failed from {Name} to {transition.TargetState.Name}: {e.Message}{e.StackTrace}", this);
                return false;
            }
            

            // if (transition.IsForced == true)
            //     return true;

            if (CanExitState(transition.TargetState) == false)
                return false;

            if (transition.TargetState.CanEnterState() == false)
                return false;
            Debug.Log($"Can Transitioning from {Name} to {transition.TargetState.Name}", this);
            return true;
        }
    }
}