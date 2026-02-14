namespace MonoFSM.Core.Simulate
{
    public class DualPhaseValue<T>
    {
        public T SimValue;
        public T RenderValue;

        public T Value
        {
            get => WorldUpdateSimulator.CurrentPhase == SimPhase.Render ? RenderValue : SimValue;
            set
            {
                if (WorldUpdateSimulator.CurrentPhase == SimPhase.Render)
                    RenderValue = value;
                else
                    SimValue = value;
            }
        }

        public void CopySimToRender() => RenderValue = SimValue;

        public void Reset()
        {
            SimValue = default;
            RenderValue = default;
        }
    }
}
