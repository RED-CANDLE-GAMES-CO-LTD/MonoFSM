using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM_Core.Runtime.Action.VariableAction
{
    public class SetVarTransformAction: AbstractStateAction, IArgEventReceiver<Transform>
    {
        // public Vector3 teleportPosition;
        // public Transform playerTransform;
        [DropDownRef]
        public VarTransform targetVar;

        protected override void OnStateEnterImplement()
        {
            
        }

        public void ArgEventReceived(Transform arg)
        {
            targetVar.SetValue(arg);
            //network? singleton...
        }
    }
}