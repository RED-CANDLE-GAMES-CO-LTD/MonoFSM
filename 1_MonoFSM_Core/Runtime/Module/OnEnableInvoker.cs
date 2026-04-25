using MonoFSM.Core.Module;
using MonoFSM.Core.Simulate;
using MonoFSM.Variable.Attributes;
using UnityEngine;

//XX EnableInoker 自動命名？

//持續檢查, 觀察 Enable Handle的狀態
//拆得很細，但有點太胖？
public class OnEnableInvoker : MonoBehaviour, IUpdateSimulate
{
    [CompRef]
    [AutoChildren]
    private OnEnableHandler _onEnableHandler;

    [CompRef]
    [AutoChildren]
    private OnDisableHandler _onDisableHandler;
    //
    // private bool _isCachedEnabled;
    // private bool _isCachedDisabled;

    // [CompRef]
    [SerializeField]
    EnableHandle _enableHandle;

    private bool isTriggeringEnable =>
        _enableHandle != null && _enableHandle.isCachedEnabled;

    private bool isTriggeringDisable =>
        _enableHandle != null && _enableHandle.isCachedDisabled;

    // private void OnEnable() //這個東西的事件反而不穩定？用update自己檢查？
    // {
    //     this.Log("OnEnable");
    //     _isCachedEnabled = true;
    // }
    //
    // private void OnDisable()
    // {
    //     _isCachedDisabled = true;
    //     this.Log("OnDisable");
    // }

    public void Simulate(float deltaTime)
    {
        //FIXME: 應該要下個frame做？ 先記下來？
        if (isTriggeringEnable && _onEnableHandler != null)
        {
            if (_enableHandle != null)
                _enableHandle._isCachedEnabled = false;

            if (_onEnableHandler.gameObject.activeSelf)
                _onEnableHandler.EventHandle();
            else
                Debug.LogError("OnEnableNode is not active", this);
        }

        if (isTriggeringDisable && _onDisableHandler != null)
        {
            if (_enableHandle != null)
                _enableHandle._isCachedDisabled = false;

            if (_onDisableHandler.gameObject.activeSelf)
                _onDisableHandler.EventHandle();
            else
                Debug.LogError("OnDisableNode is not active", this);
        }
    }
}
