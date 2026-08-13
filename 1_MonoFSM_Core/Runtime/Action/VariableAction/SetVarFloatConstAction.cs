using System;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Foundation;
using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    [QuickCreate]
    public class SetVarFloatConstAction : AbstractStateAction
    {
        public override string Description =>
            $"Set {_targetVar?.Description} to {_sourceVar.Description}";

        [FormerlySerializedAs("targetVar")]
        // [MCPExtractable]
        [DropDownRef]
        public VarFloat _targetVar;

        // [Obsolete]
        // public float TargetValue;

        public VarFloatWrapper _sourceVar;

        protected override void OnActionExecuteImplement()
        {
            // if (_sourceVar._var == null && _sourceVar.Value == 0f) //舊規
            //     targetVar.SetValue(TargetValue, this);
            // else //新規
            _targetVar.SetValue(_sourceVar.Value, this);
        }

    }
}
