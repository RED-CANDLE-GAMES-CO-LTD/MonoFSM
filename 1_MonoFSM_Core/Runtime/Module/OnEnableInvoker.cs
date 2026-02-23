using MonoFSM.Core.Module;
using MonoFSM.Core.Simulate;
using MonoFSM.Variable.Attributes;
using UnityEngine;

// internal interface IUnityEventHolder
// {
//     void PrepareUnityEvent();
// }
//
// [Serializable]
// public class TransformEvent : UnityEvent<Transform>
// {
// }


public class OnEnableInvoker : MonoBehaviour, IUpdateSimulate
{
    [CompRef]
    [AutoChildren]
    private OnEnableNode _onEnableNode;

    [CompRef]
    [AutoChildren]
    private OnDisableNode _onDisableNode;

    private bool _isCachedEnabled;
    private bool _isCachedDisabled;

    [CompRef]
    [SerializeField]
    EnableHandle _enableHandle;

    private bool isTriggeringEnable =>
        _enableHandle != null ? _enableHandle.isCachedEnabled : _isCachedEnabled;

    private bool isTriggeringDisable =>
        _enableHandle != null ? _enableHandle.isCachedDisabled : _isCachedDisabled;

    private void OnEnable() //這個東西的事件反而不穩定？用update自己檢查？
    {
        this.Log("OnEnable");
        _isCachedEnabled = true;
    }

    private void OnDisable()
    {
        _isCachedDisabled = true;
        this.Log("OnDisable");
    }

    public void Simulate(float deltaTime)
    {
        //FIXME: 應該要下個frame做？ 先記下來？
        if (isTriggeringEnable && _onEnableNode != null)
        {
            if (_enableHandle != null)
                _enableHandle._isCachedEnabled = false;

            _isCachedEnabled = false;
            if (_onEnableNode.gameObject.activeSelf)
                _onEnableNode.EventHandle();
            else
                Debug.LogError("OnEnableNode is not active", this);
        }

        if (_isCachedDisabled && _onDisableNode != null)
        {
            if (_enableHandle != null)
                _enableHandle._isCachedDisabled = false;

            _isCachedDisabled = false;
            if (_onDisableNode.gameObject.activeSelf)
                _onDisableNode.EventHandle();
            else
                Debug.LogError("OnDisableNode is not active", this);
        }
    }
}
