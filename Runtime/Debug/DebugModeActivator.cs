using System;
using System.Collections;
using System.Collections.Generic;
using RCGSetting;
using Sirenix.OdinInspector;
using UnityEngine;

public class DebugModeActivator : MonoBehaviour
{
    public Transform childNode;

    public enum DebugActivateWhen
    {
        DebugMode,
        SceneTestMode
    }

    public DebugActivateWhen ActivateWhen;
    [ShowInInspector] public bool IsDebugMode => DebugSetting.IsDebugMode;
    [ShowInInspector] public bool IsSceneTestMode => DebugSetting.IsSceneTestMode;

    private void OnValidate()
    {
        
    }

    // Update is called once per frame
    private void Start()
    {
        ActivateCheck();
    }

    private void ActivateCheck()
    {
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
#if UNITY_EDITOR
    private void Update()
    {
        ActivateCheck();
    }
#endif
}