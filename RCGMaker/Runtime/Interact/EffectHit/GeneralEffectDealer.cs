using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class GeneralEffectDealer : EffectResolver, IDefaultSerializable, IEffectDealer
    {
        public bool CanHitReceiver(IEffectReceiver receiver)
        {
            return receiver.getEffectType == getEffectType;
        }


        public void OnHitEnter(IEffectHitData data)
        {
            _enterNode?.OnEffectReceived(data);
        }

        public void OnHitExit(IEffectHitData data)
        {
            _exitNode?.OnEffectReceived(data);
        }
    }
}