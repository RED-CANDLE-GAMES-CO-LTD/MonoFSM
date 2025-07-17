using MonoFSM.Core;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    public abstract class AbstractEffectNode : AbstractEventHandler, IDefaultSerializable
    {
        // FIXME: 用EventHanlder?
        // public void OnEffectReceived(IEffectHitData data) // FIXME: 還需要interface嗎？ interface可以給別人寫...
        // {
        //     Debug.Log(" EffectEnterNode OnEffectReceived", this);
        //
        //     foreach (var receiver in _eventReceivers)
        //         if (receiver.IsValid)
        //             receiver.EventReceived(data);
        // }
    }

    // 用這個觸發action?
    public sealed class EffectEnterNode : AbstractEventHandler { }
}