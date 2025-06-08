namespace Fusion.Addons.FSM.Network
{
    public class NetworkTickProvider : ITickProvider
    {
        public NetworkTickProvider(NetworkRunner runner)
        {
            Runner = runner;
        }

        private NetworkRunner Runner { set; get; }
        public int Tick => Runner.Tick.Raw;

        public float DeltaTime => Runner.DeltaTime;

        // public object Stage => Runner.Stage;
        public bool IsStage => Runner.Stage != default;
    }
}