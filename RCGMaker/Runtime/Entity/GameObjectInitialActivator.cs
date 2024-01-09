using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core;
using UnityEngine;

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
    // public bool isActive = false;

    public void SetLevelConfig() //這個不好用... interface也不能serialize, 後面撈太晚了？
    {
        // gameObject.SetActive(IsActive);
    }


    public void OnBeforeSceneSave()
    {
        Debug.Log("GameObjectInitialActivator Save",this);
        gameObject.SetActive(IsActive);
    }

    public void OnBeforePrefabSave()
    {
        gameObject.SetActive(IsActive);
    }
}
