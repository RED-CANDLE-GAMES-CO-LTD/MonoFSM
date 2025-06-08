namespace Fusion.Addons.FSM
{
    public interface IOwnedState<TState> where TState : class, IState
    {
        public StateMachine<TState> Machine { get; set; }

        // public void AddTransition(TransitionData<TState> transition);
    }

    // Transitions

    public delegate bool Transition<in TState>(TState from, TState to) where TState : IState;

    /// <summary>
    /// pure data structure to hold transition information, assign in mono
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public struct TransitionData<TState> where TState : IState
    {
        public TState TargetState;
        public Transition<TState> Transition;
        public readonly bool IsForced;

        public TransitionData(TState targetState, Transition<TState> transition, bool isForced = false)
        {
            TargetState = targetState;
            Transition = transition;
            IsForced = isForced;
        }
    }
}