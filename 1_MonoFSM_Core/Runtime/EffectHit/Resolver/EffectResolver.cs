using System;
using MonoFSM.Core.Runtime;
using _1_MonoFSM_Core.Runtime.EffectHit;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Detection;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Interact.EffectHit.Resolver;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Runtime.Interact.EffectHit
{
    public abstract class EffectResolver
        : AbstractDescriptionBehaviour,
            IDefaultSerializable,
            IHitDataProvider,
            IResetStateRestore //, IHierarchyValueInfo,
    {

        [RequiredIn(PrefabKind.PrefabInstance)]
        [PreviewInInspector]
        [AutoParent]
        private MonoEntity _parentEntity;

        public T GetSchema<T>()
            where T : AbstractEntitySchema
        {
            return _parentEntity.GetSchema<T>();
        }

        [ShowInDebugMode]
        protected GeneralEffectHitData _currentHitData; //FIXME: 和last差在哪？

        [ShowInDebugMode]
        protected DetectData? _detectData;

#if UNITY_EDITOR
        [GUIColor(0.3f, 0.9f, 0.3f)]
        [Header("Debug Info")]
        [ShowInDebugMode]
        protected IEffectHitData _lastHitData;
#endif

        public GeneralEffectHitData GetGeneralHitData()
        {
            return _currentHitData as GeneralEffectHitData;
        }

        public IEffectHitData GetHitData()
        {
            return _currentHitData;
        }

#if UNITY_EDITOR
        private GlobalObjectId _globalId;

        public GlobalObjectId GetGlobalId()
        {
            if (_globalId.targetObjectId == 0)
                _globalId = GlobalObjectId.GetGlobalObjectIdSlow(this);

            return _globalId;
        }
#endif

        // [Button]
        // private void Rename()
        // {
        //     name = "[" + TypeTag + "]" + _effectType.name.Replace("[EffectType]", "");
        // }

#if UNITY_EDITOR
        public override string Description =>
            FormatName(_effectType?.name); //要包含Detector的名字嗎？ 遠距離 的 player
#else
        public override string Description => FormatName(_effectType?.name);
#endif

        protected abstract string TypeTag { get; }

        [FormerlySerializedAs("EffectType")]
        [Required]
        [SOConfig("GeneralEffectType")]
        public GeneralEffectType _effectType; //fixme: 改成private?

        public GeneralEffectType EffectType => _effectType;

        // public IEffectType getEffectType => EffectType;

        // [Required]
        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        protected EffectEnterNode _enterNode;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        protected EffectHitFailNode _failNode;

        public void OnEffectHitConditionFail(IEffectHitData data)
        {
            _failNode?.EventHandle(data);
        }

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        protected EffectStayNode _stayNode;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        protected EffectExitNode _exitNode;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        protected EffectEnterBestMatchNode _bestEnterNode;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        protected EffectExitBestMatchNode _bestExitNode;

        //best match 的 enter/exit 都走這裡，Dealer/Receiver 兩邊行為一致：
        //enter node 上的 local _hittingEntity 一律寫入「對方」的 entity（dealer 寫 receiver 的、receiver 寫 dealer 的）
        protected void BestMatchEnterHandle(GeneralEffectHitData data, MonoEntity pairEntity)
        {
            _bestEnterNode?._hittingEntity?.SetValue(pairEntity, this); //要先做
            _bestEnterNode?.EventHandle(data);
        }

        protected void BestMatchExitHandle(GeneralEffectHitData data)
        {
            _bestExitNode?.EventHandle(data);
        }

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private AbstractConditionBehaviour[] _conditions =
            Array.Empty<AbstractConditionBehaviour>();

        [GUIColor(0.3f, 0.9f, 0.3f)]
        [ShowInDebugMode]
        bool IsConditionPasses => _conditions.IsAllValid();

        //FIXME: 關掉的就不算嗎 hmmm
        [PreviewInInspector] public bool IsValid => isActiveAndEnabled && _conditions.IsAllValid();

        //被 culling handle 暫停（不含 despawn／手動關掉）。寫法同 EffectDetectable.IsSuspendedByCulling，
        //也同 EffectDetector.OnDisable 的判斷 —— 「暫停模擬」和「東西不見了」必須是同一個來源。
        public bool IsSuspendedByCulling => _parentObj != null && _parentObj.IsCulledByHandle;

        /// <summary>
        ///     查詢命中狀態（overlap 帳本可不可以信）用的 IsValid：被 culling handle 關掉時不算無效。
        ///     culling handler 是直接 SetActive(false) 整棵 LogicRoot，resolver 的 GO 跟著 inactive，
        ///     detector 端明明凍結了 overlap（不清、不發 exit），拿 IsValid 過濾就會讀到「沒打到」，
        ///     進出 culling 一次值就翻面兩次。cull 期間仍然評估 _conditions 是安全的：
        ///     ConditionHelper.IsAllValid 對 activeSelf == false 的 condition 是 continue（跳過不計），
        ///     不是 return false。
        ///     刻意不把這個語意併進 IsValid：IsValid 回答的是「這顆 resolver 現在有效嗎（能不能被打到）」，
        ///     cull 中的東西不該被打到；這顆回答的是「帳本可不可以信」。兩者混在一起會讓被 cull 的物件
        ///     還能收到 effect。所以 CanHitReceiver 之類的判定路徑一律留用 IsValid。
        /// </summary>
        public bool IsValidOrFrozenByCulling =>
            IsSuspendedByCulling ? _conditions.IsAllValid() : IsValid;

        /// <summary>
        ///     overlap 查詢入口（dealer 的 HasReceiverOverlap、receiver 的 HasDealerOverlap /
        ///     IsBestMatched）的結果與擋掉理由。每條 return 前都要寫，不要有靜默分支。
        ///     這些 getter 每幀被 condition 讀，所以只在 editor 賦值、不用 log 也不串字串。
        /// </summary>
        public enum OverlapQueryState
        {
            NotQueried,

            //自己被關掉且不是 culling 造成的（despawn／手動 disable）→ 判定無效
            InactiveNotCulling,

            //帳本是空的，真的沒打到
            NoOverlapRecord,

            //帳本有東西但全部失效（對側被真的關掉／condition 不成立）
            AllOverlapsInvalid,

            //正常命中
            Overlapping,

            //自己或對側被 culling handle 關掉，沿用凍結期間的 overlap 值
            FrozenByCulling,
        }

        public IActor Owner => GetComponentInParent<IActor>();
        public override string ValueInfo => IsValid ? "Valid" : "Off";
        public override bool IsDrawingValueInfo => Application.isPlaying && isActiveAndEnabled;

        // 上次 effect enter 的 tick，-1 = 從沒 enter 過
        [ShowInDebugMode]
        protected int _lastEnterTick = -1;

        // 上次 effect exit 的 tick，-1 = 從沒 exit 過
        [ShowInDebugMode]
        protected int _lastExitTick = -1;

        /// <summary>
        /// effect enter 時呼叫，記錄當下 sim tick（本地用，不同步）
        /// </summary>
        protected void RecordEffectEnter()
        {
            _lastEnterTick = WorldUpdateSimulator.CurrentTick;
        }

        /// <summary>
        /// effect exit 時呼叫，記錄當下 sim tick（本地用，不同步）
        /// </summary>
        protected void RecordEffectExit()
        {
            _lastExitTick = WorldUpdateSimulator.CurrentTick;
        }

        /// <summary>
        /// 距離上次 effect enter 經過的秒數；從沒 enter 過回傳 +∞
        /// </summary>
        [ShowInDebugMode]
        public float SecondsSinceLastEnter =>
            _lastEnterTick < 0
                ? float.PositiveInfinity
                : (WorldUpdateSimulator.CurrentTick - _lastEnterTick) * WorldUpdateSimulator.DeltaTime;

        /// <summary>
        /// 距離上次 effect exit 經過的秒數；從沒 exit 過回傳 +∞
        /// </summary>
        [ShowInDebugMode]
        public float SecondsSinceLastExit =>
            _lastExitTick < 0
                ? float.PositiveInfinity
                : (WorldUpdateSimulator.CurrentTick - _lastExitTick) * WorldUpdateSimulator.DeltaTime;

        public virtual void ResetStateRestore(bool IsHardReset)
        {
            _currentHitData = null;
            //殘留的話 GetDetectData() 會回 reset 前的命中點/法線
            _detectData = null;
            _lastEnterTick = -1;
            _lastExitTick = -1;
        }
    }
}
