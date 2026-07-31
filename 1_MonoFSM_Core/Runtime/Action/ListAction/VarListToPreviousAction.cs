using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Variable;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.ListAction
{
    public class VarListToPreviousAction : AbstractStateAction
    {
        [SerializeField]
        [DropDownRef]
        private AbstractVarList _varList; //valueProvider?

        protected override void OnActionExecuteImplement()
        {
            if (_varList == null)
            {
                Debug.LogError("VarListToPreviousAction: _varList is null", this);
                return;
            }

            _varList.GoToPrevious();
        }
    }
}
