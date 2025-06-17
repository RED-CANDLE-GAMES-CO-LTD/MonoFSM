using System.Collections.Generic;
using UnityEngine;

//FIXME: 好像完全不需要這顆耶
namespace Fusion.Addons.FSM
{
    public abstract class NetworkStateBehaviour : NetworkStateBehaviour<NetworkStateBehaviour>
    {
    }


    public abstract class NetworkStateBehaviour<TState> : NetworkBehaviour, IState, IOwnedState<TState>
        where TState : NetworkStateBehaviour<TState>
    {
        // PUBLIC MEMBERS

        public int StateId { get; set; }
        public StateMachine<TState> Machine { get; set; }
        public virtual string Name => gameObject.name;
        public int Priority => _priority;

        //  PRIVATE MEMBERS

        [SerializeField] private int _priority = 0;
        [SerializeField] private bool _checkPriorityOnExit = true;

        private List<TransitionData<TState>> _transitions;

        // PUBLIC METHODS

        public void AddTransition(TransitionData<TState> transition)
        {
            if (_transitions == null) _transitions = new List<TransitionData<TState>>(16);

            _transitions.Add(transition);
        }

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
            if (_transitions != null)
                foreach (var trainstionData in _transitions)
                {
                    var transition = trainstionData;

                    if (TryTransition(ref transition) == true)
                    {
                        Machine.ForceActivateState(transition.TargetState);
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
            OnEnterState();
        }

        void IState.OnExitState()
        {
            OnExitState();
        }

        void IState.OnEnterStateRender()
        {
            OnEnterStateRender();
        }

        void IState.OnRender()
        {
            OnRender();
        }

        void IState.OnExitStateRender()
        {
            OnExitStateRender();
        }

        // IStateMachine[] IState.ChildMachines { get; set; }

        void IState.CollectChildStateMachines(List<IStateMachine> stateMachines)
        {
            OnCollectChildStateMachines(stateMachines);
        }

        // NetworkBehaviour INTERFACE

        public sealed override void FixedUpdateNetwork()
        {
            // Seal method to prevent unwanted usage. OnFixedUpdate should be used instead.
        }

        public sealed override void Render()
        {
            // Seal method to prevent unwanted usage. OnRender should be used instead.
        }

        // PRIVATE METHODS

        private bool TryTransition(ref TransitionData<TState> transition)
        {
            if (transition.Transition(this as TState, transition.TargetState) == false)
                return false;

            if (transition.IsForced == true)
                return true;

            if (CanExitState(transition.TargetState) == false)
                return false;

            if (transition.TargetState.CanEnterState() == false)
                return false;

            return true;
        }
    }
}