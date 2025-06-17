using UnityEngine;
using UnityEngine.Events;

namespace MonoFSM.Runtime.Interact.EffectHit.Resolver
{
    public class EffectReceivedEventInvoker : MonoBehaviour, IEffectReceivedProcessor
    {
        public UnityEvent OnEffectReceivedEvent;

        public void EffectHitResult(IEffectHitData hitData)
        {
            OnEffectReceivedEvent.Invoke();
        }
    }
}