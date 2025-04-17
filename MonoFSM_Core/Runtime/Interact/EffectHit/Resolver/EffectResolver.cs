using System;
using jerryee.UnityMCP;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public abstract class EffectResolver : MonoBehaviour, IDefaultSerializable
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
            name = "[" + TypeTag + "]" + EffectType.name.Replace("[EffectType]", "");
        }

        protected abstract string TypeTag { get; }

        [MCPExtractable] [Required] [SOConfig("GeneralEffectType")]
        public GeneralEffectType EffectType;
        // public IEffectType getEffectType => EffectType;

        [Required] [Component] [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        protected EffectEnterNode _enterNode;

        [Component] [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        protected EffectHitFailNode _failNode;

        public void OnEffectHitConditionFail(IEffectHitData data)
        {
            _failNode?.EventHandle(data);
        }

        [Component] [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        protected EffectExitNode _exitNode;


        [Component] [PreviewInInspector] [AutoChildren]
        private AbstractConditionComp[] _conditions = Array.Empty<AbstractConditionComp>();

        [PreviewInInspector]
        public bool IsValid => isActiveAndEnabled && _conditions.IsAllValid(); //condition 可以burst?感覺不會比較快，這個數量級

        public IActor Owner => GetComponentInParent<IActor>();
    }
}