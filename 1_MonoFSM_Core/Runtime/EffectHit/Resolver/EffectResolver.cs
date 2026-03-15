using System;
using MonoFSM.Core.Runtime;
using _1_MonoFSM_Core.Runtime.EffectHit;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Detection;
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
        public override string Description => FormatName(_effectType?.name) + _note; //要包含Detector的名字嗎？ 遠距離 的 player
#else
        public override string Description => FormatName(_effectType?.name);
#endif

        protected abstract string TypeTag { get; }

        [FormerlySerializedAs("EffectType")]
        [Required]
        [SOConfig("GeneralEffectType")]
        public GeneralEffectType _effectType; //改成private?

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
        protected EffectExitNode _exitNode;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        protected EffectEnterBestMatchNode _bestEnterNode;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        protected EffectExitBestMatchNode _bestExitNode;

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
        public string ValueInfo => IsValid ? "Valid" : "Invalid";
        public bool IsDrawingValueInfo => Application.isPlaying && isActiveAndEnabled;

        public virtual void ResetStateRestore()
        {
            _currentHitData = null;
        }
    }
}
