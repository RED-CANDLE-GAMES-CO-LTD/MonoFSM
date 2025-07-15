using System;
using jerryee.UnityMCP;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;

namespace MonoFSM.Core.Runtime.Action.VariableAction
{
    //FIXME: 不好，應該是value provider有個是max就好
    [Obsolete]
    public class SetVarFloatToBoundAction : AbstractStateAction
    {
        public enum BoundType
        {
            Min,
            Max
        }

        [MCPExtractable] [DropDownRef] public VarFloat _targetVar;
        public BoundType _boundType = BoundType.Max;

        protected override void OnActionExecuteImplement()
        {
            var value = _boundType == BoundType.Min ? _targetVar.Min : _targetVar.Max;
            _targetVar.SetValue(value, this);
        }
    }
}