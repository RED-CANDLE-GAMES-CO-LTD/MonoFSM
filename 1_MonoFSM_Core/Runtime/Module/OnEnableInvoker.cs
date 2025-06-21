using MonoFSM.Variable.Attributes;
using MonoFSM.Core.Module;
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


public class OnEnableInvoker : MonoBehaviour
{
    [CompRef] [AutoChildren] private OnEnableNode _onEnableNode;
    [CompRef] [AutoChildren] private OnDisableNode _onDisableNode;

    private void OnEnable()
    {
        this.Log("OnEnable");
        if (_onEnableNode.gameObject.activeSelf)
            _onEnableNode.EventHandle();
    }

    private void OnDisable()
    {
        this.Log("OnDisable");
        if (_onDisableNode && _onDisableNode.gameObject.activeSelf)
            _onDisableNode.EventHandle();
    }
}