using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.Attributes;
using RCGExtension;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    public abstract class EffectResolver : MonoBehaviour, IDefaultSerializable, IHierarchyValueInfo
    {
#if UNITY_EDITOR
        private GlobalObjectId _globalId;
        public GlobalObjectId GetGlobalId()
        {
            if (_globalId.targetObjectId == 0) _globalId = GlobalObjectId.GetGlobalObjectIdSlow(this);

            return _globalId;
        }
#endif

        [Button]
        private void Rename()
        {
            name = "[" + TypeTag + "]" + _effectType.name.Replace("[EffectType]", "");
        }

        protected abstract string TypeTag { get; }


        [FormerlySerializedAs("EffectType")] [Required] [SOConfig("GeneralEffectType")]
        public GeneralEffectType _effectType;
        // public IEffectType getEffectType => EffectType;

        [Required] [Component] [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        protected EffectEnterNode _enterNode;

        [CompRef] [AutoChildren(DepthOneOnly = true)] 
        protected EffectHitFailNode _failNode;

        public void OnEffectHitConditionFail(IEffectHitData data)
        {
            _failNode?.EventHandle(data);
        }

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        protected EffectExitNode _exitNode;


        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private AbstractConditionBehaviour[] _conditions = Array.Empty<AbstractConditionBehaviour>();

        [PreviewInInspector]
        public bool IsValid => isActiveAndEnabled && _conditions.IsAllValid(); //condition 可以burst?感覺不會比較快，這個數量級

        public IActor Owner => GetComponentInParent<IActor>();
        public string ValueInfo => IsValid ? "Valid" : "Invalid";
        public bool IsDrawingValueInfo => Application.isPlaying && isActiveAndEnabled;
    }
}