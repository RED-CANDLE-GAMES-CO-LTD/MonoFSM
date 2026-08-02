using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Condition;
using MonoFSM.Core.Attributes;
using MonoFSM.FSM;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    ///     跨 prefab 版的 <see cref="HasStateTagCondition" />：目標不是直接引用的 MonoFSMOwner，
    ///     而是一顆 VarEntity（通常是 Global getter，例如 d_TeamStatus），
    ///     掃它子樹裡所有 MonoFSMOwner，任一個當前 state 帶 _tag 就成立。
    ///     一個 entity 底下可能有多組 StateFolder，所以是「任一」而不是「唯一」。
    /// </summary>
    public class EntityHasStateTagCondition : AbstractConditionBehaviour
    {
        [ConditionPreset("Entity Has State Tag", Category = "State", Priority = 99, ColorHex = "#FFB347")]
        private static void Preset_EntityStateTag(EntityHasStateTagCondition c)
        {
        }

        [Required]
        [DropDownRef]
        [SerializeField]
        private VarEntity _targetEntity;

        [Required]
        [SerializeField]
        private StateTag _tag;

        private readonly List<MonoFSMOwner> _fsmOwners = new();

        protected override bool IsValid
        {
            get
            {
                if (_targetEntity == null || _tag == null) return false;
                var entity = _targetEntity.Value;
                if (entity == null) return false;

                _fsmOwners.Clear();
                entity.GetComponentsInChildren(_fsmOwners);
                foreach (var owner in _fsmOwners)
                    if (owner.CurrentState is GeneralState state && state.HasTag(_tag))
                        return true;

                return false;
            }
        }

        public override string Description =>
            $"{(_targetEntity != null ? _targetEntity.name : "?")} Has Tag [{(_tag != null ? _tag.name : "?")}]";
    }
}
