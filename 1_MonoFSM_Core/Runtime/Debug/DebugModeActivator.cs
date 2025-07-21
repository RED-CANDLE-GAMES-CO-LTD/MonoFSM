using System;
using System.Collections;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoDebugSetting;
using Sirenix.OdinInspector;
using UnityEngine;

//下面的節點，在特定測試模式下才會打開
public class DebugModeActivator : MonoBehaviour
{
// #if UNITY_EDITOR
    public Transform childNode;

    public enum DebugActivateWhen
    {
        DebugMode,
        SceneTestMode
    }

    public DebugActivateWhen ActivateWhen;
    [ShowInInspector] public bool IsDebugMode => DebugSetting.IsDebugMode;
    [ShowInInspector] public bool IsSceneTestMode => DebugSetting.IsSceneTestMode;

    // Update is called once per frame
    private void Start()
    {
        ActivateCheck();
    }

    private void ActivateCheck()
    {
#if !UNITY_EDITOR //production關掉？
        childNode.gameObject.SetActive(false);
        enabled = false;
#endif

        //可以砍掉？   
        switch (ActivateWhen)
        {
            case DebugActivateWhen.DebugMode:
                childNode.gameObject.SetActive(DebugSetting.IsDebugMode);
                break;
            case DebugActivateWhen.SceneTestMode:
                childNode.gameObject.SetActive(DebugSetting.IsSceneTestMode);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    private void Update()
    {
        ActivateCheck();
    }

}