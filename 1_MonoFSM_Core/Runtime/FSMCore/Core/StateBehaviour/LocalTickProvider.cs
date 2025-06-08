using Fusion.Addons.FSM;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    public class LocalTickProvider : ITickProvider
    {
        public int Tick => Time.frameCount;
        public float DeltaTime => Time.deltaTime;
        public bool IsStage => true;
    }
}