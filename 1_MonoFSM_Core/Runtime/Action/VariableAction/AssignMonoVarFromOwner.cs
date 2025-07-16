using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;
using MonoFSM.Runtime.Variable;
using UnityEngine;
using UnityEngine.Serialization;

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction
{
    public class AssignMonoVarFromOwner : AbstractStateAction
    {
        [CompRef] [Auto] private IBlackboardProvider _ownerProvider;

        [FormerlySerializedAs("_varMono")] [SerializeField] [DropDownRef]
        private VarBlackboard _varBlackboard;

        protected override void OnActionExecuteImplement()
        {
            Debug.Log($"AssignMonoVarFromOwner: Assigning {_ownerProvider.Description} to {_varBlackboard.name}");
            var source = _ownerProvider.GetComponentOfOwner<MonoEntity>();
            _varBlackboard.SetValue(source, this);
        }

        public override string Description =>
            $"Assign {_ownerProvider?.Description} to {_varBlackboard?.name}";
    }
}