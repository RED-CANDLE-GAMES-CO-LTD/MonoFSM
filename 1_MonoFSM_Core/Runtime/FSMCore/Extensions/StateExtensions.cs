using System;

namespace Fusion.Addons.FSM
{
    public static class StateExtensions
    {
        // public static void AddTransition<TState>(this IOwnedState<TState> state, TState targetState, Transition<TState> transition, bool forced = false)
        // 	where TState : class, IState
        // {
        // 	state.AddTransition(new TransitionData<TState>(targetState, transition, forced));
        // }

        // public static void AddTransition<TState>(this IOwnedState<TState> state, TState targetState, Func<bool> transition, bool isForced = false)
        // 	where TState : class, IState
        // {
        // 	// Wrap simple transition into transition data
        // 	var transitionData = new TransitionData<TState>(targetState, (_, _) => transition.Invoke(), isForced);
        //
        // 	state.AddTransition(transitionData);
        // }
    }
}