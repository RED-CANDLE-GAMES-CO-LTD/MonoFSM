namespace MonoFSM.FSM
{
    public interface IMonoStateMachine
    {
        string Name { get; }
        IMonoState ActiveState { get; }
        IMonoState PreviousState { get; }

        IMonoState[] States { get; }

        // void Initialize(NetworkStateMachineController controller, NetworkRunner runner);
        void Initialize(StateMachineLogic logic, IMonoTickProvider tickProvider);
        void FixedUpdate();
        void Render();
        void Deinitialize(bool hasState);
        void SetDefaultState(int stateId);
        void Reset();

        bool TryActivateState(int stateId, bool allowReset = false);
        bool ForceActivateState(int stateId, bool allowReset = false);
        bool TryDeactivateState(int stateId);
        bool ForceDeactivateState(int stateId);
        bool TryToggleState(int stateId, bool value);
        void ForceToggleState(int stateId, bool value);

        bool? EnableLogging { get; set; }

        // Networking

        int WordCount { get; }
        unsafe void Read(int* ptr);
        unsafe void Write(int* ptr);
        void Interpolate(InterpolationData interpolationData);
    }
}
