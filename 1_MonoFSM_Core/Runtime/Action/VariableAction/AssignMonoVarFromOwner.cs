using MonoFSM_Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using RCGMaker.Runtime;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction
{
    public class AssignMonoVarFromOwner : AbstractStateAction
    {
        [CompRef] [Auto] private IVariableOwnerProvider _ownerProvider;
        [SerializeField] [DropDownRef] private VarMono _varMono;

        protected override void OnStateEnterImplement()
        {
            var source = _ownerProvider.GetComponentOfOwner<MonoDescriptable>();
            _varMono.SetValue(source, this);
        }

        public override string Description =>
            $"Assign {_ownerProvider.Description} to {_varMono.name}";
    }
}