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

        // public MonoEntity ParentEntity
        // {
        //     get
        //     {
        //         AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_parentEntity));
        //         // this.EnsureComponentInParent(ref _parentEntity);
        //         return _parentEntity;
        //     }
        // }

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
        // [PreviewInInspector]
        // public bool IsValid => gameObject.activeSelf && _conditions.IsAllValid();

        public IActor Owner => GetComponentInParent<IActor>();
        public override string ValueInfo => IsValid ? "Valid" : "Off";
        public override bool IsDrawingValueInfo => Application.isPlaying && isActiveAndEnabled;

        // 上次 effect exit 的 tick，-1 = 從沒 exit 過
        [ShowInDebugMode]
        protected int _lastExitTick = -1;

        /// <summary>
        /// effect exit 時呼叫，記錄當下 sim tick（本地用，不同步）
        /// </summary>
        protected void RecordEffectExit()
        {
            _lastExitTick = WorldUpdateSimulator.CurrentTick;
        }

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
            _lastExitTick = -1;
        }
    }
}
