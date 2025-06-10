using System.Collections.Generic;

namespace Fusion.Addons.FSM
{
    public abstract class State : State<State>
    {
    }

    public abstract unsafe class State<TState> : IState, IOwnedState<TState> where TState : State<TState>
    {
        // PUBLIC MEMBERS

        public int StateId { get; set; }
        public StateMachine<TState> Machine { get; set; }
        public string Name { get; set; }
        public int Priority { get; set; }

        public bool CheckPriorityOnExit = true;

        // PRIVATE MEMBERS

        private List<TransitionData<TState>> _transitions;

        // CONSTRUCTORS

        protected State(string name, int priority = 0) : this()
        {
            Name = name;
            Priority = priority;
        }

        protected State()
        {
        }

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

        protected virtual bool CanExitState(IState nextState)
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

        protected virtual void CollectChildStateMachines(List<IStateMachine> stateMachines)
        {
        }

        //FIXME: netcode想拔掉
        protected virtual int GetNetworkDataWordCount()
        {
            return 0;
        }

        protected virtual void ReadNetworkData(int* ptr)
        {
        }

        protected virtual void WriteNetworkData(int* ptr)
        {
        }

        // IState INTERFACE

        string IState.Name => string.IsNullOrEmpty(Name) == false ? Name : GetType().Name;

        void IState.OnFixedUpdate()
        {
            if (_transitions != null)
                for (var i = 0; i < _transitions.Count; i++)
                {
                    var transition = _transitions[i];

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
            if (isExplicitDeactivation == false && CheckPriorityOnExit == true &&
                (nextState as TState).Priority < Priority)
                return false;

            return CanExitState(nextState);
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
            CollectChildStateMachines(stateMachines);
        }

        int IState.GetWordCount()
        {
            return GetNetworkDataWordCount();
        }

        void IState.Read(int* ptr)
        {
            ReadNetworkData(ptr);
        }

        void IState.Write(int* ptr)
        {
            WriteNetworkData(ptr);
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