using System;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    //用這個觸發action?
    public class EffectEnterNode : MonoBehaviour, IEffectReceivedHandler, IDefaultSerializable
    {
        [Component]
        [PreviewInInspector] [AutoChildren]
        private IRCGArgEventReceiver<IEffectHitData>[] _effectReceivedProcessor = Array.Empty<IRCGArgEventReceiver<IEffectHitData>>();

        public void OnEffectReceived(IEffectHitData data)
        {
            // Debug.Log(" EffectEnterNode OnEffectReceived", this);
            foreach (var processor in _effectReceivedProcessor)
            {
                processor.EventReceived(data);
            }
        }
        
        
    }
}