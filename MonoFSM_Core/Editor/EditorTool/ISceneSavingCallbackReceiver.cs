namespace RCGMaker.Core
{
    public interface IOnBuildSceneSavingCallbackReceiver
    {
        void OnBeforeBuildSceneSave();
    }
    
    public interface ISceneSavingCallbackReceiver
    {
        void OnBeforeSceneSave();
    }
    public interface ISceneSavingAfterCallbackReceiver
    {
        void OnAfterSceneSave();
    }

    public interface IBeforeBuildProcess
    {
        void OnBeforeBuildProcess();
    }

    public interface IGameStateOwner : ISceneSavingCallbackReceiver
    {
    }

    public interface IBeforePrefabSaveCallbackReceiver
    {
        void OnBeforePrefabSave();
    }
}