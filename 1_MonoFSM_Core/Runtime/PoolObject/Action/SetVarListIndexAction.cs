using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Variable;
using UnityEngine;

namespace MonoFSM.Runtime.ObjectPool
{
    public class SetVarListIndexAction : AbstractStateAction
    {
        public int _index;
        [DropDownRef] public AbstractVarList _varList;

        protected override void OnActionExecuteImplement()
        {
            // Debug.Log($"SetVarListIndexAction: Setting index {_index} on VarList {_varList.name}", this);
            _varList.SetIndex(_index);
        }

        public override string Description => $"Set VarList Index: {_varList.name} to {_index}";
    }
}