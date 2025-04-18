using System;
using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using UnityEngine;
using UnityEngine.Events;

internal interface IUnityEventHolder
{
    void PrepareUnityEvent();
}

[Serializable]
public class TransformEvent : UnityEvent<Transform>
{
}

public class OnEnableInvoker : MonoBehaviour, IUnityEventHolder
{
    public UnityEvent OnEnableEvent;
    public UnityEvent OnDisableEvent;
    [PreviewInInspector] [AutoChildren] private IArgEventReceiver<bool>[] _eventReceivers;

    private void Awake()
    {
        OnAwakeEvent?.Invoke();
    }

    private void OnEnable()
    {
        InvokeEvent();
        if (_eventReceivers == null)
            return;
        foreach (var eventReceiver in _eventReceivers) eventReceiver.ArgEventReceived(true);
    }

    public void InvokeEvent()
    {
        //這個都會GC?
        OnEnableEvent?.Invoke();
        OnEnableTransformEvent?.Invoke(transform);
    }

    private void OnDisable()
    {
        OnDisableEvent?.Invoke();
        if (_eventReceivers == null)
            return;
        foreach (var eventReceiver in _eventReceivers) eventReceiver.ArgEventReceived(false);
    }

    //FIXME: 還有在用這個嗎？沒有dependency checker很難用
    [Header("Deprecated")] public TransformEvent OnEnableTransformEvent;

    public UnityEvent OnAwakeEvent; //這個不是很好，串邏輯又會掉到外面去，模組爆炸

    public void PrepareUnityEvent()
    {
        // OnEnableEvent.PrepareInvoke();
        // use reflection to get the event and call PrepareInvoke
        var type = OnEnableEvent.GetType();
        var prepareInvokeMethod = type.GetMethod("PrepareInvoke"); //GC??? 反正一起
        if (prepareInvokeMethod == null)
            return;
        prepareInvokeMethod.Invoke(OnEnableEvent, null);
        prepareInvokeMethod.Invoke(OnDisableEvent, null);
    }
}