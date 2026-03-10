namespace MonoFSM.Core.Simulate
{
    public enum SimPhase
    {
        None,
        BeforeSimulate,
        Simulate,
        AfterSimulate,
        Render,
        AfterUpdate,
        BeforeRender,
        AfterRender,
    }
}
