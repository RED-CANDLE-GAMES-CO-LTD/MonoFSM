using _1_MonoFSM_Core.Runtime.Attributes;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    //觀戰對象優先，沒有的話 fallback 回自己；邏輯搬自 PlayerPositionFollower.FollowTargetEntity
    public class SpectateAwareEntityProvider : AbstractEntitySource
    {
        public override string SuggestDeclarationName => _selfEntity?._varTag?.name;

        [PropertyOrder(-1)] [DropDownRef] public VarEntity _selfEntity;

        [Tooltip("觀戰對象身上掛的 VarEntity tag（例如 d_SpectateTarget），有值時優先跟這個")]
        [SOTypeDropdown(typeof(VarEntity))]
        public VariableTag _spectateTargetTag;

        [ShowInPlayMode]
        public override MonoEntity monoEntity
        {
            get
            {
                var self = _selfEntity?.Value;
                if (self == null || _spectateTargetTag == null)
                    return self;

                var spectateTarget = self.GetVar<VarEntity>(_spectateTargetTag)?.Value;
                return spectateTarget != null ? spectateTarget : self;
            }
        }
    }
}
