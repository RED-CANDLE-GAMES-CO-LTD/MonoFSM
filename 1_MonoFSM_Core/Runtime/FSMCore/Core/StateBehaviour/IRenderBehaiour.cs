namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    public interface IRenderBehaiour //區分 oneshot?
    {
        public void OnEnterRender();
        public void OnRender();
        bool isActiveAndEnabled { get; }
    }

    public interface IArgRenderBehaviour<in T> : IRenderBehaiour
    {
        public void OnArgEnterRender(T arg);
        public void OnArgRender(T arg);
    }
}
