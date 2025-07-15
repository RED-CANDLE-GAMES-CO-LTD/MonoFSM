using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.VariableAction
{
    public class SetVarTransformAction: AbstractStateAction, IArgEventReceiver<Transform>
    {
        // public Vector3 teleportPosition;
        // public Transform playerTransform;
        [DropDownRef]
        public VarTransform targetVar;

        protected override void OnActionExecuteImplement()
        {
            
        }

        public void ArgEventReceived(Transform arg)
        {
            if (arg == null)
            {
                Debug.LogError("Arg is null", this);
                return;
            }

            targetVar.SetValue(arg, this);
            //network? singleton...
        }
    }
}