using UnityEngine;
using MonoFSM.Core;

//存檔時，強迫設定這個物件的active狀態
public class ReadOnlyActivator : MonoBehaviour
{
    //只能被Editor改，不能被code改
    [SerializeField] private bool isActive = false;
    protected bool IsActive => isActive;
}

public class GameObjectInitialActivator : ReadOnlyActivator, ILevelConfig, ISceneSavingCallbackReceiver,
    IBeforePrefabSaveCallbackReceiver
{
    //這個不好用... interface也不能serialize, 後面撈太晚了？
    public void SetLevelConfig() { }

    public void OnBeforeSceneSave()
        => gameObject.SetActive(IsActive);

    public void OnBeforePrefabSave() 
        => gameObject.SetActive(IsActive);
}
