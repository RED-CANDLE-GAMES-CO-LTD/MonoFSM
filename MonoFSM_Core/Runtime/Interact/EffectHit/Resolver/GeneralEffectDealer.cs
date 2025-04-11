using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using MonoFSM.Variable;
using RCGMaker.Runtime.Interact.EffectHit.Resolver;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class ProxySource
    {
    }

    public class GeneralEffectDealer : EffectResolver, IEffectDealer
    {
        [PreviewInInspector] [Component] [AutoChildren]
        private AbstractEffectHitCondition[] _effectConditions;

        // public VariableMonoDescriptableProvider proxyProvider;
        // public GeneralEffectType effectType;
        [Header("自動找EffectType相同的Dealer")] //[SerializeReference]
        [Auto]
        // [PreviewInInspector]
        [Component]
        // [ShowDrawerChain]
        IVarMonoProvider _proxyProvider;

        [PreviewInInspector]
        GeneralEffectDealer proxyDealer =>
            _proxyProvider?.Value?.GetDealer(EffectType); //兩個都可以執行耶，那EffectHitData怎麼算呢？ ex: 人dealer耗體力，斧頭dealer耗耐久


        //FIXME: 要必須有嗎？如果null就表示可以當純偵測器...
        // [PropertyOrder(-1)]
        // public FloatValueSource ValueSource;

        [Auto]
        // [PreviewInInspector] 
        [Component]
        [PropertyOrder(-1)]
        IFloatProvider _valueSource; //FIXME: 還是要把情境也寫死？
        //FIXME: 可能還會涉及多個varfloat,不一定需要？ 用getFloat就好了 
        //通常就是 A 打 B
        //A有value
        //B有cost
        //或甚至有整套判定+運算，ApplyEffectCondition, ApplyEffects

        [PreviewInInspector] [AutoParent] IBinder _binder;

        public bool IsEnteredReceiver(IEffectReceiver receiver)
        {
            return _receivers.Contains(receiver);
        }
        public bool CanHitReceiver(IEffectReceiver receiver)
        {
            // if (!IsValid)
            // {
            //     return false;
            // }
            if (!receiver.IsValid) //沒開的不算
                return false;
            var r = (GeneralEffectReceiver)receiver;
            if (r.EffectType != EffectType)
            {
                return false;
            }


            if (_proxyProvider != null) //指定需要透過ProxyProvider拿 ex: 斧頭上的Dealer
            {
                if (proxyDealer == null) //並沒有找到Proxy Dealer，失敗
                {
                    var data = r.GenerateEffectHitData(this, receiver);
                    OnEffectHitConditionFail(data);
                    r.OnEffectHitConditionFail(data);
                    return false;
                }

                proxyDealer.CanHitReceiver(r); //繼續判囉？
            }


//FIXME: 應該要先判這個嗎？
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

            var id = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(r);
            
            this.Log("HitReceiver Success:" , r.GetGlobalId());
            Debug.Log("HitReceiver Success:" , r);
            return true;
        }

        public float FinalValue => _valueSource.GetFloat();

        //FIXME: runtime receivers
        [PreviewInInspector] List<IEffectReceiver> _receivers = new();
        [PreviewInInspector] private GeneralEffectReceiver _lastReceiver;

        public void OnHitEnter(IEffectHitData data)
        {
            if (_proxyProvider != null)
            {
                proxyDealer.OnHitEnter(data);
                //兩邊可能都要做事，都判
            }

            _enterNode?.OnEffectReceived(data);
            _receivers.Add(data.Receiver);
            _lastReceiver = data.Receiver as GeneralEffectReceiver;
        }

        public void OnHitExit(IEffectHitData data)
        {
            //_receivers裡面要有才可以做這件事
            if (_proxyProvider != null)
            {
                proxyDealer.OnHitEnter(data);
            }

            _exitNode?.OnEffectReceived(data);
            _receivers.Remove(data.Receiver);
        }

        protected override string TypeTag => "Dealer";
    }
}