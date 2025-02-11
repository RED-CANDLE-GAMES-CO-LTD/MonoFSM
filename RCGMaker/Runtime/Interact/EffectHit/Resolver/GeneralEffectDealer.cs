using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    
    public class GeneralEffectDealer : EffectResolver, IEffectDealer
    {
        //FIXME: 要必須有嗎？如果null就表示可以當純偵測器...
        // [PropertyOrder(-1)]
        // public FloatValueSource ValueSource;

        [PropertyOrder(-1)]
        [SerializeReference]
        public IFloatProvider source;

        [PreviewInInspector]
        [AutoParent] IBinder _binder;
        
        public bool CanHitReceiver(IEffectReceiver receiver)
        {
            return ((GeneralEffectReceiver)receiver).EffectType == EffectType;
        }

        public float FinalValue => source.GetFloat();

        [PreviewInInspector]
        List<IEffectReceiver> _receivers = new List<IEffectReceiver>();
        public void OnHitEnter(IEffectHitData data)
        {
            _enterNode?.OnEffectReceived(data);
            _receivers.Add(data.Receiver);
        }

        public void OnHitExit(IEffectHitData data)
        {
            _exitNode?.OnEffectReceived(data);
            _receivers.Remove(data.Receiver);
        }

        protected override string TypeTag => "Dealer";
    }
}