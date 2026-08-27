using System.Collections.Generic;

namespace MonoFSM.FSM
{
    /// <summary>
    /// 讓 StateMachine 的 state change log 能回報「這次是走哪一條 transition 過來的」。
    /// 由 State 端記錄最後一次通過條件的 transition 與當時的 tick，只在 log 時取用，允許組字串。
    /// </summary>
    public interface ILastTransitionRecord
    {
        string GetLastTransitionInfo(int currentTick);
    }

    public unsafe interface IMonoState
    {
        public int StateId { get; set; }
        public string Name { get; }

        public void Initialize();
        public void Deinitialize(bool hasState);

        public bool CanEnterState();
        public bool CanExitState(IMonoState nextState, bool isExplicitDeactivation);

        public void OnEnterState();
        public void OnFixedUpdate();
        public void OnExitState();

        public void OnEnterStateRender();
        public void OnRender();
        public void OnExitStateRender();

        // public IStateMachine[] ChildMachines { get; set; }
        internal void CollectChildStateMachines(List<IMonoStateMachine> stateMachines)
        {
        }

        //FIXME: remove netcode
        // Custom network data section
        public int GetWordCount()
        {
            return 0;
        }

    
        public unsafe void Read(int* ptr)
        {
        }

        public void Write(int* ptr)
        {
        }

        public void Interpolate(InterpolationData interpolationData) //沒有人用，可砍？
        {
        }
    }
}