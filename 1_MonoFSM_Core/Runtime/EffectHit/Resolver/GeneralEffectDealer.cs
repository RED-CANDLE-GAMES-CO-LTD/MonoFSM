using System.Collections.Generic;
using System.Diagnostics;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Detection;
using MonoFSM.Runtime.Interact.EffectHit.Resolver;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    public class ProxySource { }

    //FIXME: 篩選掉同個owner下的判斷？
//FIXME: 還是要可以帶一個變數會比較好 (或是一組變數？可以 remapping的？) 畢竟就算要 add force 之類的還是有可能會有多種力道之類的
    public class GeneralEffectDealer : EffectResolver, IEffectDealer
    {
        public override string ValueInfo =>
            _receivers.Count > 0 ? _receivers.Count.ToString() : base.ValueInfo;

        public override void ResetStateRestore(bool IsHardReset)
        {
            base.ResetStateRestore(false);
            _lockedEntity = null;
            _hittingEntities.Clear();
        }

        private void OnDisable()
        {
            _lockedEntity = null;
        }

        // public VariableMonoDescriptableProvider proxyProvider;
        // public GeneralEffectType effectType;
        [Header("自動找EffectType相同的Dealer")] //[SerializeReference]
        [Auto]
        // [PreviewInInspector]
        [Component]
        // [ShowDrawerChain]
        private IVarMonoProvider _proxyProvider;

        [PreviewInInspector]
        private GeneralEffectDealer proxyDealer => _proxyProvider?.Value?.GetDealer(_effectType);

        [Header("一次 Enable 只能打一個 Entity")] [SerializeField]
        private bool _singleEntityPerEnable;

        [ShowInDebugMode] private MonoEntity _lockedEntity;

        //互動時，兩個都可以執行耶，那EffectHitData怎麼算呢？ ex: 人dealer耗體力，斧頭dealer耗耐久


        //FIXME: 要必須有嗎？如果null就表示可以當純偵測器...
        // [PropertyOrder(-1)]
        // public FloatValueSource ValueSource;
        //FIXME: 還要可以把這個值取出, 從receiver那邊做？和tag拿var整合
        public VarFloatWrapper _defaultValue;

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

        [PreviewInInspector]
        [AutoParent]
        private IBinder _binder;

        public bool IsEnteredReceiver(GeneralEffectReceiver receiver)
        {
            return _receivers.Contains(receiver);
        }

        public bool HasReceiverOverlap => isActiveAndEnabled && _receivers.Count > 0;



        [ShowInDebugMode]
        private string _failReason = "No Fail Reason";

        [Conditional("UNITY_EDITOR")]
        public void SetFailReason(string reason)
        {
            _failReason = reason;
        }

        [PreviewInInspector] [Component] [AutoChildren(DepthOneOnly = true)]
        protected AbstractEffectHitCondition[] _effectConditions;

        public bool IsEffectConditionsAllValid(EffectResolver pairResolver)
        {
            if (_effectConditions != null)
                foreach (var condition in _effectConditions)
                {
                    var result = condition.IsEffectShouldHit(pairResolver);
                    if (!result)
                    {
                        // SetFailReason($"EffectCondition {condition.GetType().Name} failed");
                        // var data = r.GenerateEffectHitData(this);
                        // OnEffectHitConditionFail(data);
                        // r.OnEffectHitConditionFail(data);
                        return false;
                    }
                }

            return true;
        }


        public bool CanHitReceiver(IEffectReceiver receiver)
        {
            SetFailReason("Check");
            if (receiver == null)
            {
                SetFailReason("Receiver is null");
                return false;
            }
            var r = (GeneralEffectReceiver)receiver;

            if (_singleEntityPerEnable && _lockedEntity != null && r.BindEntity != _lockedEntity)
            {
                SetFailReason("SingleEntityPerEnable: already locked to another entity");
                return false;
            }
            if (r._effectType != _effectType)
            {
                _candidateReceivers.Add(receiver); //什麼時候清掉？
                SetFailReason("EffectType mismatch");
                return false;
            }

            if (!receiver.IsValid) //沒開的不算
            {
                _candidateReceivers.Add(receiver); //什麼時候清掉？
                SetFailReason("Receiver is not valid");
                return false;
            }

            if (_proxyProvider != null) //指定需要透過ProxyProvider拿 ex: 斧頭上的Dealer
            {
                if (proxyDealer == null) //並沒有找到Proxy Dealer，失敗
                {
                    SetFailReason("ProxyDealer is null");
                    var data = r.GenerateEffectHitData(this, null);
                    OnEffectHitConditionFail(data);
                    r.OnEffectHitConditionFail(data);
                    return false;
                }

                proxyDealer.CanHitReceiver(r); //繼續判囉？
            }

            if (_effectConditions != null)
                foreach (var condition in _effectConditions)
                {
                    var result = condition.IsEffectShouldHit(r);
                    if (!result)
                    {
                        SetFailReason($"EffectCondition {condition.GetType().Name} failed");
                        var data = r.GenerateEffectHitData(this, null); //FIXME: fail的話就先傳null了？
                        OnEffectHitConditionFail(data);
                        r.OnEffectHitConditionFail(data);
                        return false;
                    }
                }

            // if (!r.IsEffectConditionsAllValid(this))
            // {
            //     SetFailReason($"Receiver's EffectCondition fail");
            //     var data = r.GenerateEffectHitData(this, null);
            //     OnEffectHitConditionFail(data);
            //     r.OnEffectHitConditionFail(data);
            //     return false;
            // }

#if UNITY_EDITOR
            this.Log("HitReceiver Success:"); //, r.GetGlobalId());
#endif
            SetFailReason("HitReceiver Success");
            return true;
        }

        // public float FinalValue => _valueSource.Value;

        //FIXME: runtime receivers
        [PreviewInInspector]
        private HashSet<GeneralEffectReceiver> _receivers = new();

        //FIXME: 沒有清掉？
        [Header("Condition不符合的")]
        [PreviewInDebugMode]
        private HashSet<IEffectReceiver> _candidateReceivers = new();

        public void ClearCandidateReceivers()
        {
            _candidateReceivers.Clear();
        }

        [PreviewInInspector]
        private GeneralEffectReceiver _lastReceiver;

        [ShowInInspector]
        GeneralEffectReceiver _lastBestMatchReceiver;
        public GeneralEffectReceiver BestMatchReceiver => _lastBestMatchReceiver;

        public void OnBestMatchCheck()
        {
            if (_receivers.Count == 0)
            {
                if (_lastBestMatchReceiver != null)
                    _lastBestMatchReceiver.OnEffectHitBestMatchExit(_currentHitData);
                _lastBestMatchReceiver = null;
                return;
            }

            if (_receivers.Count == 1)
            {
                GeneralEffectReceiver only = null;
                foreach (var r in _receivers) { only = r; break; }
                if (only == null) return;
                if (_lastBestMatchReceiver != only)
                {
                    if (_lastBestMatchReceiver != null)
                        _lastBestMatchReceiver.OnEffectHitBestMatchExit(_currentHitData);
                    only.OnEffectHitBestMatchEnter(_currentHitData);
                    _lastBestMatchReceiver = only;
                }
                return;
            }

            // receivers >= 2: 用計分機制找 best match
            GeneralEffectReceiver bestMatch = null;
            float bestScore = float.MinValue;
            foreach (var receiver in _receivers)
            {
                var score = _onlyTriggerBestMatch != null
                    ? _onlyTriggerBestMatch.CalculateScore(this, receiver)
                    : -Vector3.Distance(transform.position, receiver.transform.position); // 距離越近分數越高
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = receiver;
                }
            }

            if (_lastBestMatchReceiver != bestMatch)
            {
                if (_lastBestMatchReceiver != null)
                    _lastBestMatchReceiver.OnEffectHitBestMatchExit(_currentHitData);
                if (bestMatch != null)
                    bestMatch.OnEffectHitBestMatchEnter(_currentHitData);
                _lastBestMatchReceiver = bestMatch;
            }
        }

        public void OnHitEnter(IEffectHitData data, DetectData? detectData = null)
        {
            _currentHitData = data as GeneralEffectHitData;
            if (_currentHitData == null)
            {
                Debug.LogError("EffectHitData is not GeneralEffectHitData");
                return;
            }
            if (_proxyProvider != null)
                proxyDealer.OnHitEnter(_currentHitData, detectData);

            var receiverEntity = _currentHitData.GeneralReceiver.BindEntity;
            if (_singleEntityPerEnable && _lockedEntity == null)
                _lockedEntity = receiverEntity;
            _enterNode?._hittingEntity?.SetValue(receiverEntity, this); //要先做
            _enterNode?.EventHandle(_currentHitData);

            _receivers.Add(_currentHitData.GeneralReceiver);
            if (!_hittingEntities.Contains(receiverEntity))
                _hittingEntities.Add(receiverEntity);

            _lastReceiver = data.Receiver as GeneralEffectReceiver;
        }

        [ShowInInspector]
        private readonly List<MonoEntity> _hittingEntities = new();

        public List<MonoEntity> GetHittingEntities()
        {
            return _hittingEntities;
        }

        public void OnHitExit(IEffectHitData data)
        {
            //_receivers裡面要有才可以做這件事
            if (_proxyProvider != null)
                proxyDealer.OnHitEnter(data);

            var hitData = data as GeneralEffectHitData;
            var exitReceiver = (GeneralEffectReceiver)data.Receiver;
            _receivers.Remove(exitReceiver);
            _exitNode?.EventHandle(hitData);

            // 只有在沒有其他 receiver 指向同一 entity 時才從清單移除
            var entity = exitReceiver.BindEntity;
            bool entityStillActive = false;
            foreach (var r in _receivers)
            {
                if (r.BindEntity == entity)
                {
                    entityStillActive = true;
                    break;
                }
            }

            if (!entityStillActive)
                _hittingEntities.Remove(entity);
        }

        protected override string TypeTag => "Dealer";
        protected override string DescriptionTag => "Dealer";

        [CompRef]
        [Auto]
        private AbstractOnlyTriggerBestMatch _onlyTriggerBestMatch;

        // public AbstractOnlyTriggerBestMatch OnlyTriggerBestMatch => _onlyTriggerBestMatch;
        public bool IsOnlyTriggerBestMatch => _onlyTriggerBestMatch != null;
    }
}
