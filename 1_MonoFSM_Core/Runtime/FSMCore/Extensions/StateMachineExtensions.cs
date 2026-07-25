namespace MonoFSM.FSM
{
	//FIXME: 這邊是 fusion的assertion?
	public static class StateMachineExtensions
	{
        public static bool TryActivateState(this IMonoStateMachine stateMachine, IMonoState state,
            bool allowReset = false)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryActivateState(state.StateId, allowReset);
		}

        public static bool TryActivateState<T>(this IMonoStateMachine stateMachine,
            bool allowReset = false) where T : IMonoState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryActivateState(state.StateId, allowReset);
		}

        public static bool ForceActivateState(this IMonoStateMachine stateMachine, IMonoState state,
            bool allowReset = false)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.ForceActivateState(state.StateId, allowReset);
		}

        public static bool ForceActivateState<T>(this IMonoStateMachine stateMachine,
            bool allowReset = false) where T : IMonoState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.ForceActivateState(state.StateId, allowReset);
		}

        public static bool TryDeactivateState(this IMonoStateMachine stateMachine, IMonoState state)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryDeactivateState(state.StateId);
		}

        public static bool TryDeactivateState<T>(this IMonoStateMachine stateMachine)
            where T : IMonoState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryDeactivateState(state.StateId);
		}

        public static bool ForceDeactivateState(this IMonoStateMachine stateMachine,
            IMonoState state)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.ForceDeactivateState(state.StateId);
		}

        public static bool ForceDeactivateState<T>(this IMonoStateMachine stateMachine)
            where T : IMonoState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.ForceDeactivateState(state.StateId);
		}

        public static bool TryToggleState(this IMonoStateMachine stateMachine, IMonoState state,
            bool value)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryToggleState(state.StateId, value);
		}

        public static bool TryToggleState<T>(this IMonoStateMachine stateMachine, bool value)
            where T : IMonoState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryToggleState(state.StateId, value);
		}

        public static void ForceToggleState(this IMonoStateMachine stateMachine, IMonoState state,
            bool value)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			stateMachine.ForceToggleState(state.StateId, value);
		}

        public static void ForceToggleState<T>(this IMonoStateMachine stateMachine, bool value)
            where T : IMonoState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			stateMachine.ForceToggleState(state.StateId, value);
		}

        public static bool HasState(this IMonoStateMachine stateMachine, IMonoState state)
		{
			var states = stateMachine.States;

			for (int i = 0; i < states.Length; i++)
			{
				if (states[i].StateId == state.StateId && states[i] == state)
					return true;
			}

			return default;
		}

        public static IMonoState GetState<T>(this IMonoStateMachine stateMachine)
            where T : IMonoState
		{
			var states = stateMachine.States;

			for (int i = 0; i < states.Length; i++)
			{
				if (states[i] is T state)
					return state;
			}

			return default;
		}
	}
}
