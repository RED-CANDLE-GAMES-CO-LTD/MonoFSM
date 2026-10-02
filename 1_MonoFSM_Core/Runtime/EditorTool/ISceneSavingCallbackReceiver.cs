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

    public interface IEditorResetToPlayTest
    {
        void OnEditorResetToPlayTest();
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

    /// <summary>
    /// ScriptableObject 被 CLI（`up asset create / set / set-ref / add-element / do`）改完、存檔之後呼叫。
    /// CLI 走 SerializedObject + SaveAssets，不保證觸發 OnValidate / import postprocessor，
    /// 靠那兩條做的自動維護（例如 GameEventTag 自動收進 GameEventRegistry）會靜默漏掉。
    /// 在這裡補做，回傳一行要印給呼叫方的狀態（null / 空字串 = 不印）。只在 Editor 被呼叫。
    /// </summary>
    public interface IAfterCliAssetEditCallbackReceiver
    {
        string OnAfterCliAssetEdit();
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
