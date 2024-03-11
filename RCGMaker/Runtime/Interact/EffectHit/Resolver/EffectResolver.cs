using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class EffectResolver : MonoBehaviour, IDefaultSerializable
    {
        [Required] [SOConfig("GeneralEffectType")]
        public GeneralEffectType EffectType;
        // public IEffectType getEffectType => EffectType;

        [Component]
        [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        protected EffectEnterNode _enterNode;

        [Component]
        [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        protected EffectExitNode _exitNode;
    }
}