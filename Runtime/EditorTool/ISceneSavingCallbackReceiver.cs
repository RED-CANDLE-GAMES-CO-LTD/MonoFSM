namespace RCGMaker.Core
{
    public interface ISceneSavingCallbackReceiver
    {
        void OnBeforeSceneSave();
    }

    public interface IGameStateOwner : ISceneSavingCallbackReceiver
    {
    }
}