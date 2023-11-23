using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core;
using UnityEngine;

//存檔時，強迫設定這個物件的active狀態
public class GameObjectInitialActivator : MonoBehaviour, ILevelConfig, ISceneSavingCallbackReceiver
{
    public bool isActive = false;

    public void SetLevelConfig() //這個不好用... interface也不能serialize, 後面撈太晚了？
    {
        this.gameObject.SetActive(isActive);
    }


    public void OnBeforeSceneSave()
    {
        isActive = gameObject.activeSelf;
    }
}
