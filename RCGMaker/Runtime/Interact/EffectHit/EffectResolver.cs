using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class EffectResolver : MonoBehaviour
    {
        [Required] [SOConfig("GeneralEffectType")]
        public GeneralEffectType EffectType;

        public IEffectType getEffectType => EffectType;

        [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        protected EffectEnterNode _enterNode;

        [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        protected EffectExitNode _exitNode;
    }
}