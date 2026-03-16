using MonoFSM.Core.Runtime.Action;
using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    public class SetVarIntAction : AbstractStateAction
    {
        [FormerlySerializedAs("targetFlag")] [DropDownRef]
        public VarInt _targetFlag;

        [FormerlySerializedAs("TargetValue")] public int _targetValue;

        public override string Description => $"Set {_targetFlag?.name} to {_targetValue}";

        protected override void OnActionExecuteImplement()
        {
            _targetFlag.SetValue(_targetValue, this);
        }
    }
}
