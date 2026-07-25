using MonoFSM.FSM;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    public class LocalTickProvider : IMonoTickProvider
    {
        public int Tick => Time.frameCount;
        public float DeltaTime => Time.deltaTime; //FIXME: hmm?
        public bool IsStage => true;
    }
}