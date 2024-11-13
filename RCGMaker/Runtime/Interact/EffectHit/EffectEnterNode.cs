using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class EffectEnterNode : MonoBehaviour, IEffectReceivedHandler, IDefaultSerializable
    {
        [Component]
        [PreviewInInspector] [AutoChildren] IRCGArgEventReceiver[] _effectReceivedProcessor;

        public void OnEffectReceived(IEffectHitData data)
        {
            foreach (var processor in _effectReceivedProcessor)
            {
                processor.EventReceived(data);
            }
        }
        
        
    }
}