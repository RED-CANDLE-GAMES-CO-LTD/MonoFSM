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
        private void OnValidate()
        {
            transform.localPosition = Vector3.zero;
        }

        //module不會有耶
        // [Component(AddComponentAt.Parent)]
        [PreviewInInspector] [Required] [AutoParent]
        private EffectDetectable _detectable; //不一定是，IEffectDetectable?

        [Header("Best Match Settings")]
        [Tooltip("當 EffectType 設定為只觸發最佳匹配時，此值越高優先級越高")]
        public int MatchPriority = 0;

        // [PropertyOrder(-1)]
        // public  ValueSource; //FIXME: 拿來做什麼？

        //FIXME: 從GeneralEffectHitData？
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
            var dealerEntity = _currentHitData.GeneralDealer.ParentEntity;
            _enterNode?._hittingEntity?.SetValue(dealerEntity, this);
            _enterNode?.EventHandle(_currentHitData);

            _dealers.Add(data.Dealer as GeneralEffectDealer);
#if UNITY_EDITOR
            _lastHitData = data;
#endif
        }

        public bool HasDealerOverlap => _dealers.Count > 0;

        [GUIColor(0.3f, 0.9f, 0.3f)]
        [PreviewInInspector]
        private HashSet<GeneralEffectDealer> _dealers = new();

        public void OnEffectHitBestMatchEnter(GeneralEffectHitData data)
        {
            //bestEnterNode
            _bestEnterNode?.EventHandle(data);
        }

        public void OnEffectHitBestMatchExit(GeneralEffectHitData data)
        {
            this.Log("OnHitBestMatchExit");
            _bestExitNode?.EventHandle(data);
            // _currentHitData = null;
        }

        public void OnEffectHitExit(GeneralEffectHitData data)
        {
            this.Log("OnHitExit");
            _dealers.Remove(data.Dealer as GeneralEffectDealer);
            _exitNode?.EventHandle(data as GeneralEffectHitData);
            _currentHitData = null;
            //FIXME: 要清掉 _hittingEntity 嗎？那好像不要放在enterNODe耶...而且
            //每個Dealer都要call好煩喔
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
