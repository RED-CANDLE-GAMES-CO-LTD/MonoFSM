using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class EffectEnterNode : MonoBehaviour, IEffectReceivedHandler
    {
        public IEffectReceivedProcessor[] _effectReceivedProcessor;

        public void OnEffectReceived(IEffectHitData data)
        {
            foreach (var processor in _effectReceivedProcessor)
            {
                processor.EffectHitResult(data);
            }
        }
    }
}