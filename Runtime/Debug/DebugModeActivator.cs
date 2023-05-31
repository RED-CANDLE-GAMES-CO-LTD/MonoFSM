using System;
using System.Collections;
using System.Collections.Generic;
using RCGSetting;
using Sirenix.OdinInspector;
using UnityEngine;

public class DebugModeActivator : MonoBehaviour
{
    public Transform childNode;
    [ShowInInspector] public bool IsDebugMode => DebugSetting.IsDebugMode;

    private void OnValidate()
    {
        childNode.gameObject.SetActive(DebugSetting.IsDebugMode);
    }

    // Update is called once per frame
    private void Start()
    {
        childNode.gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void Update()
    {
        childNode.gameObject.SetActive(DebugSetting.IsDebugMode);
    }
#endif
}