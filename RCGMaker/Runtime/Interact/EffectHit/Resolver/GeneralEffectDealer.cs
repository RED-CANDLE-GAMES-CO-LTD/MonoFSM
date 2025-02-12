using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.Interact.EffectHit.Resolver;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class GeneralEffectDealer : EffectResolver, IEffectDealer
    {
        [PreviewInInspector] [Component] [AutoChildren]
        private AbstractEffectHitCondition[] _effectConditions;
        //FIXME: 要必須有嗎？如果null就表示可以當純偵測器...
        // [PropertyOrder(-1)]
        // public FloatValueSource ValueSource;

        [PropertyOrder(-1)] [SerializeReference]
        public IFloatProvider source; //FIXME: 還是要把情境也寫死？
        //通常就是 A 打 B
        //A有value
        //B有cost
        //或甚至有整套判定+運算，ApplyEffectCondition, ApplyEffects

        [PreviewInInspector] [AutoParent] IBinder _binder;

        public bool CanHitReceiver(IEffectReceiver receiver)
        {
            //FIXME: 還要有特別的condition
            ////EffectDealerCondition
            // //IsValid(Receiver)
            var r = (GeneralEffectReceiver)receiver;
            if (r.EffectType != EffectType)
            {
                return false;
            }

            //特殊的EffectCondition
            foreach (var condition in _effectConditions)
            {
                var result = condition.IsEffectHitValid((GeneralEffectReceiver)receiver);
                if (!result)
                {
                    var data = r.GenerateEffectHitData(this, receiver);
                    OnEffectHitConditionFail(data);
                    r.OnEffectHitConditionFail(data);
                    return false;
                }
            }

            return true;
        }

        public float FinalValue => source.GetFloat();

        //FIXME: runtime receivers
        [PreviewInInspector] List<IEffectReceiver> _receivers = new();

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