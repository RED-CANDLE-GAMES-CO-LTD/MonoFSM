namespace MonoFSM_Core.Network
{
    public interface IUpdateSimulate //parent必須要有AbstractSimulator
    {
        void Simulate(float deltaTime);
    }
}