using System.Collections.Generic;

namespace Fusion.Addons.FSM
{
    public unsafe interface IState
    {
        public int StateId { get; set; }
        public string Name { get; }

        public void Initialize();
        public void Deinitialize(bool hasState);

        public bool CanEnterState();
        public bool CanExitState(IState nextState, bool isExplicitDeactivation);

        public void OnEnterState();
        public void OnFixedUpdate();
        public void OnExitState();

        public void OnEnterStateRender();
        public void OnRender();
        public void OnExitStateRender();

        // public IStateMachine[] ChildMachines { get; set; }
        internal void CollectChildStateMachines(List<IStateMachine> stateMachines)
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