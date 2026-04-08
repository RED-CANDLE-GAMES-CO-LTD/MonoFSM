namespace MonoFSM.Core
{
    public interface IOnBuildSceneSavingCallbackReceiver
    {
        void OnBeforeBuildSceneSave();
    }

    public interface ICustomHeavySceneSavingCallbackReceiver
    {
        void OnHeavySceneSaving();
    }
    
    //FIXME: 
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

    public interface IAfterPrefabStageOpenCallbackReceiver
    {
        void OnAfterPrefabStageOpen();
    }

    public interface ICustomPrefabSaveCallbackReceiver
    {
        void OnCustomPrefabSave();
    }
}