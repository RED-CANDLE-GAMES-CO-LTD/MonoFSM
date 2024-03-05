using System.Linq;
using RCGMaker.Core.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class EffectExitNode : MonoBehaviour, IEffectReceivedHandler
    {
        [PreviewInInspector] [AutoChildren] IRCGArgEventReceiver[] _effectReceivedProcessor;

        [PreviewInInspector]
        private Component[] processorComps => _effectReceivedProcessor.Select(x => x as Component).ToArray();
        //有哪幾種event在外部定義
        // public RCGEventType EventType;

        public void OnEffectReceived(IEffectHitData data)
        {
            foreach (var processor in _effectReceivedProcessor)
            {
                processor.EventReceived(data);
            }
        }
    }
}