using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class EffectExitNode : MonoBehaviour, IEffectReceivedHandler
    {
        IEffectReceivedProcessor[] _effectReceivedProcessor;

        //有哪幾種event在外部定義
        // public RCGEventType EventType;

        public void OnEffectReceived(IEffectHitData data)
        {
            foreach (var processor in _effectReceivedProcessor)
            {
                processor.EffectHitResult(data);
            }
        }
    }
}