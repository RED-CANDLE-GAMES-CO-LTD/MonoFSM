using MonoFSM.Core.Attributes;
using MonoFSMCore.Runtime.LifeCycle;

namespace MonoFSM.Core
{
    public class UpdateLoopManager : SingletonBehaviour<UpdateLoopManager>, ISceneAwake, IGameDestroy
    {
        [PreviewInInspector]
        public readonly UpdateList<IProxyUpdate> UpdateList = new(t => t.UpdateProxy());

        [PreviewInInspector]
        public readonly UpdateList<IProxyLateUpdate> LateUpdateList = new(t => t.LateUpdateProxy());

        private void Update() 
            => UpdateList.UpdateManual();

        private void LateUpdate() 
            => LateUpdateList.UpdateManual();

        public void EnterSceneAwake()
        {
            UpdateList.ClearNull();
            LateUpdateList.ClearNull();
        }
        

        public void OnGameDestroy()
        {
            UpdateList.ClearNull();
            LateUpdateList.ClearNull();
        }
    }
}