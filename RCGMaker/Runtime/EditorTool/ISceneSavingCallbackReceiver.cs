namespace RCGMaker.Core
{
    public interface ISceneSavingCallbackReceiver
    {
        void OnBeforeSceneSave();
    }

    public interface IBeforeBuildProcess
    {
        void OnBeforeBuildProcess();
    }

    public interface IGameStateOwner : ISceneSavingCallbackReceiver
    {
    }


    public interface IPrefabSavingCallbackReceiver
    {
        void OnBeforePrefabSave();
    }
}