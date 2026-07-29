using System.Collections.Generic;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Detection;
using MonoFSM.Core.EffectHit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    //FIXME: 應該要怎麼轉接比較好，我會有好幾種事件類型，幫每種事件類型定義類別，再讓下面的action去做事
    public class GeneralEffectReceiver
        : EffectResolver,
            IEffectReceiver,
            IDetectDataProvider,
            IValueOfKey<GeneralEffectType>
    {
        protected override bool HasError()
        {
            _detectable = GetComponentInParent<EffectDetectable>();
            return base.HasError() || Detectable == null;
        }

        public override string ValueInfo =>
            HasDealerOverlap ? base.ValueInfo + " Has Dealer!" : base.ValueInfo;

        private void OnValidate()
        {
            transform.localPosition = Vector3.zero;
        }

        public override void ResetStateRestore(bool IsHardReset)
        {
            base.ResetStateRestore(false);
            _dealers.Clear();
#if UNITY_EDITOR
            _lastHitData = null;
#endif
        }

        //module不會有耶
        // [Component(AddComponentAt.Parent)]

        [ShowInDebugMode]
        [AutoParent]
        private EffectDetectable _detectable; //不一定是，IEffectDetectable?

        [Required]
        [GUIColor(0.8f, 0.9f, 0.3f)]
        [PreviewInInspector]
        public EffectDetectable Detectable => _detectable?._bindingRoot != null
            ? _detectable._bindingRoot as EffectDetectable
            : _detectable;

        [Header("Best Match Settings")]
        [Tooltip("當 EffectType 設定為只觸發最佳匹配時，此值越高優先級越高")]
        public int MatchPriority = 0;

        // [PropertyOrder(-1)]
        // public  ValueSource; //FIXME: 拿來做什麼？

        //FIXME: 從GeneralEffectHitData？
        //FIXME: 太多進入點了吧
        public GeneralEffectHitData GenerateEffectHitData(
            IEffectDealer dealer,
            BaseEffectDetectTarget receiverSourceObj
        )
        {
            //FIXME: 要用pool, 泛用的pool
            var data = new GeneralEffectHitData();
            data.Override(dealer, this, receiverSourceObj);
            return data;
        }

        public void ForceDirectEffectHit(
            GeneralEffectDealer dealer,
            BaseEffectDetectTarget receiverSourceObj
        )
        {
            if (!dealer.CanHitReceiver(this))
                return;

            // Debug.Log("ForceDirectEffectHit", this);
            var hitData = GenerateEffectHitData(dealer, receiverSourceObj);
            dealer.OnHitEnter(hitData);
            OnEffectHitEnter(hitData);
            //然後要馬上離開？
            dealer.OnHitExit(hitData);
            OnEffectHitExit(hitData);
        }

        //收到事件後，叫下面的action做事
        public IEffectType getEffectType => _effectType;

        //FIXME: rename to OnHitEnter
        public void OnEffectHitEnter(GeneralEffectHitData data, DetectData detectData) //這裡是code定義
        {
            _detectData = detectData;
            OnEffectHitEnter(data);
            // Debug.Log("OnEffectHitEnter with DetectData", this);
        }

        public void OnEffectHitEnter(GeneralEffectHitData data)
        {
            // Debug.Log("OnEffectHitEnter", this);
            this.Log("OnHitEnter");
            _currentHitData = data as GeneralEffectHitData;
            var dealerEntity = _currentHitData.GeneralDealer.BindEntity;
            _enterNode?._hittingEntity?.SetValue(dealerEntity, this);
            _enterNode?.EventHandle(_currentHitData);

            _dealers[data.Dealer as GeneralEffectDealer] = _currentHitData;
#if UNITY_EDITOR
            _lastHitData = data;
#endif
        }

        //重疊期間每幀觸發（enter 那幀不觸發），data 重用 enter 時的同一顆 instance
        public void OnEffectHitStay(GeneralEffectHitData data, DetectData detectData)
        {
            _detectData = detectData;
            _currentHitData = data;
            _stayNode?.EventHandle(data);
        }

        //取得 enter 時為這個 dealer 建立的 hitData（stay/exit 重用，不再 new）
        public bool TryGetHitDataFor(GeneralEffectDealer dealer, out GeneralEffectHitData hitData)
        {
            return _dealers.TryGetValue(dealer, out hitData) && hitData != null;
        }

        public bool HasDealerOverlap => isActiveAndEnabled && _dealers.Count > 0;

        //FIXME: 會殘留...
        [GUIColor(0.3f, 0.9f, 0.3f)]
        [PreviewInInspector]
        private readonly Dictionary<GeneralEffectDealer, GeneralEffectHitData> _dealers = new();

        public void OnEffectHitBestMatchEnter(GeneralEffectHitData data)
        {
            BestMatchEnterHandle(data, data?.GeneralDealer?.BindEntity);
        }

        public void OnEffectHitBestMatchExit(GeneralEffectHitData data)
        {
            this.Log("OnHitBestMatchExit");
            BestMatchExitHandle(data);
            _bestEnterNode?.ClearHittingEntityIfNeeded();
            // _currentHitData = null;
        }

        public void OnEffectHitExit(GeneralEffectHitData data)
        {
            this.Log("OnHitExit");
            _dealers.Remove(data.Dealer as GeneralEffectDealer);
            RecordEffectExit();
            _exitNode?.EventHandle(data);
            _currentHitData = null;

            //還有其他 dealer 在打就不能清（每個 dealer 都會 call 到這裡）
            if (_dealers.Count == 0)
                _enterNode?.ClearHittingEntityIfNeeded();
        }

        // public float ReactValue => ValueSource?.FinalValue ?? 0;

        //EffectExit也要呢
        protected override string TypeTag => "Receiver";

        public DetectData? GetDetectData()
        {
            if (_detectData.HasValue)
                return _detectData.Value;
            else
                return null; //或許可以拋出異常？
        }

        protected override string DescriptionTag => "Receiver";
        public GeneralEffectType Key => _effectType;
    }
}
