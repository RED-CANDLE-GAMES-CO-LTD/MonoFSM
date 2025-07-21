using System.Collections.Generic;
using System.Diagnostics;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Detection;
using MonoFSM.Runtime.Interact.EffectHit.Resolver;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    public class ProxySource
    {
    }

    //FIXME: 篩選掉同個owner下的判斷？

    public class GeneralEffectDealer : EffectResolver, IEffectDealer,IHitDataProvider
    {
        [PreviewInInspector] [Component] [AutoChildren(DepthOneOnly = true)]
        private AbstractEffectHitCondition[] _effectConditions;

        // public VariableMonoDescriptableProvider proxyProvider;
        // public GeneralEffectType effectType;
        [Header("自動找EffectType相同的Dealer")] //[SerializeReference]
        [Auto]
        // [PreviewInInspector]
        [Component]
        // [ShowDrawerChain]
        private IVarMonoProvider _proxyProvider;

        
        [PreviewInInspector]
        private GeneralEffectDealer proxyDealer =>
            _proxyProvider?.Value?.GetDealer(_effectType);
        //互動時，兩個都可以執行耶，那EffectHitData怎麼算呢？ ex: 人dealer耗體力，斧頭dealer耗耐久


        //FIXME: 要必須有嗎？如果null就表示可以當純偵測器...
        // [PropertyOrder(-1)]
        // public FloatValueSource ValueSource;

        // [Auto]
        // // [PreviewInInspector] 
        // [Component]
        // [PropertyOrder(-1)]
        // private IFloatProvider _valueSource; //FIXME: 還是要把情境也寫死？
        //FIXME: 可能還會涉及多個varfloat,不一定需要？ 用getFloat就好了 
        //通常就是 A 打 B
        //A有value
        //B有cost
        //或甚至有整套判定+運算，ApplyEffectCondition, ApplyEffects

        [PreviewInInspector] [AutoParent] private IBinder _binder;

        public bool IsEnteredReceiver(IEffectReceiver receiver)
        {
            return _receivers.Contains(receiver);
        }

        [ShowInDebugMode] private string _failReason = "No Fail Reason";

        [Conditional("UNITY_EDITOR")]
        public void SetFailReason(string reason)
        {
            _failReason = reason;
        }

        public bool CanHitReceiver(IEffectReceiver receiver)
        {
            // if (!IsValid)
            // {
            //     return false;
            // }
            SetFailReason("Check");
            if (!receiver.IsValid) //沒開的不算
            {
                SetFailReason("Receiver is not valid");
                return false;
            }
            var r = (GeneralEffectReceiver)receiver;
            if (r._effectType != _effectType)
            {
                SetFailReason("EffectType mismatch");
                return false;
            }


            if (_proxyProvider != null) //指定需要透過ProxyProvider拿 ex: 斧頭上的Dealer
            {
                if (proxyDealer == null) //並沒有找到Proxy Dealer，失敗
                {
                    SetFailReason("ProxyDealer is null");
                    var data = r.GenerateEffectHitData(this);
                    OnEffectHitConditionFail(data);
                    r.OnEffectHitConditionFail(data);
                    return false;
                }

                proxyDealer.CanHitReceiver(r); //繼續判囉？
            }
            
            if(_effectConditions != null)
                foreach (var condition in _effectConditions)
                {
                    var result = condition.IsEffectHitValid((GeneralEffectReceiver)receiver);
                    if (!result)
                    {
                        SetFailReason($"EffectCondition {condition.GetType().Name} failed");
                        var data = r.GenerateEffectHitData(this);
                        OnEffectHitConditionFail(data);
                        r.OnEffectHitConditionFail(data);
                        return false;
                    }
                }

#if UNITY_EDITOR
            this.Log("HitReceiver Success:"); //, r.GetGlobalId()); 
#endif
            SetFailReason("HitReceiver Success");
            return true;
        }

        // public float FinalValue => _valueSource.Value;

        //FIXME: runtime receivers
        [PreviewInInspector] private List<IEffectReceiver> _receivers = new();
        [PreviewInInspector] private GeneralEffectReceiver _lastReceiver;

        public void OnHitEnter(IEffectHitData data, DetectData? detectData = null)
        {
            _currentHitData = data;
            if (_proxyProvider != null) proxyDealer.OnHitEnter(data as GeneralEffectHitData, detectData);
            //兩邊可能都要做事，都判
            _enterNode?.EventHandle(data as GeneralEffectHitData);
            _receivers.Add(data.Receiver);
            _lastReceiver = data.Receiver as GeneralEffectReceiver;
        }

        private IEffectHitData _currentHitData;

        public void OnHitExit(IEffectHitData data)
        {
            //_receivers裡面要有才可以做這件事
            if (_proxyProvider != null) proxyDealer.OnHitEnter(data);

            _exitNode?.EventHandle(data as GeneralEffectHitData);
            _receivers.Remove(data.Receiver);
        }

        protected override string TypeTag => "Dealer";
        public IEffectHitData GetHitData()
        {
            return _currentHitData;
        }
    }
}