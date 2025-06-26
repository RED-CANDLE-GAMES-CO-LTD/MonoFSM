namespace Fusion.Addons.FSM
{
	//FIXME: 這邊是 fusion的assertion?
	public static class StateMachineExtensions
	{
		public static bool TryActivateState(this IStateMachine stateMachine, IState state, bool allowReset = false)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryActivateState(state.StateId, allowReset);
		}

		public static bool TryActivateState<T>(this IStateMachine stateMachine, bool allowReset = false) where T : IState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryActivateState(state.StateId, allowReset);
		}

		public static bool ForceActivateState(this IStateMachine stateMachine, IState state, bool allowReset = false)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.ForceActivateState(state.StateId, allowReset);
		}

		public static bool ForceActivateState<T>(this IStateMachine stateMachine, bool allowReset = false) where T : IState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.ForceActivateState(state.StateId, allowReset);
		}

		public static bool TryDeactivateState(this IStateMachine stateMachine, IState state)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryDeactivateState(state.StateId);
		}

		public static bool TryDeactivateState<T>(this IStateMachine stateMachine) where T : IState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryDeactivateState(state.StateId);
		}

		public static bool ForceDeactivateState(this IStateMachine stateMachine, IState state)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.ForceDeactivateState(state.StateId);
		}

		public static bool ForceDeactivateState<T>(this IStateMachine stateMachine) where T : IState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.ForceDeactivateState(state.StateId);
		}

		public static bool TryToggleState(this IStateMachine stateMachine, IState state, bool value)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryToggleState(state.StateId, value);
		}

		public static bool TryToggleState<T>(this IStateMachine stateMachine, bool value) where T : IState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			return stateMachine.TryToggleState(state.StateId, value);
		}

		public static void ForceToggleState(this IStateMachine stateMachine, IState state, bool value)
		{
			// Assert.Check(stateMachine.HasState(state), $"State {state.Name} not present in the state machine {stateMachine.Name}");

			stateMachine.ForceToggleState(state.StateId, value);
		}

		public static void ForceToggleState<T>(this IStateMachine stateMachine, bool value) where T : IState
		{
			var state = stateMachine.GetState<T>();
			// Assert.Check(state != null, $"State of type {typeof(T).Name} not present in the state machine {stateMachine.Name}");

			stateMachine.ForceToggleState(state.StateId, value);
		}

		public static bool HasState(this IStateMachine stateMachine, IState state)
		{
			var states = stateMachine.States;

			for (int i = 0; i < states.Length; i++)
			{
				if (states[i].StateId == state.StateId && states[i] == state)
					return true;
			}

			return default;
		}

		public static IState GetState<T>(this IStateMachine stateMachine) where T : IState
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
