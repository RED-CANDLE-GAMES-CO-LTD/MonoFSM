using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSM.Core.DataProvider;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.Runtime.Action.VariableAction
{
    public class SetVarFloatAction : AbstractStateAction
    {
        [DropDownRef] public VarFloat _targetVar;
        
        [Required] [CompRef] [Auto] private IFloatProvider _valueProvider; //如果要拿到VarFloat的Max怎麼設計比較好？

        protected override void OnActionExecuteImplement()
        {
            var value = _valueProvider.Value;
            _targetVar.SetValue(value, this);

        }

        //自動產生語意
        public override string Description => $"Set {_targetVar.name} to {_valueProvider.Description}";
    }
}