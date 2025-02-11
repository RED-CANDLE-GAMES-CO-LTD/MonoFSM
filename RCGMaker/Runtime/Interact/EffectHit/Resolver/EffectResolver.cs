using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public abstract class EffectResolver : MonoBehaviour, IDefaultSerializable
    {
        [Button]
        void Rename()
        {
            name = "["+TypeTag+"]"+EffectType.name.Replace("[EffectType]","");
        }

        protected abstract string TypeTag { get; } 
        
        [Required] [SOConfig("GeneralEffectType")]
        public GeneralEffectType EffectType;
        // public IEffectType getEffectType => EffectType;

        [Required]
        [Component]
        [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        protected EffectEnterNode _enterNode;

        [Component]
        [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        protected EffectExitNode _exitNode;
    }
}