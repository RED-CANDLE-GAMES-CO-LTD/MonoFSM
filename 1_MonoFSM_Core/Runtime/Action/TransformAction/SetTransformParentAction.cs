using _1_MonoFSM_Core.Runtime._0_Pattern.DataProvider.ComponentWrapper;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;
using MonoFSM.Variable;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.TransformAction
{
    public class SetTransformParentAction : AbstractStateAction
    {
        public Transform _target;
        public VarCompProviderRef _targetVarRef; //拿到rigidbody的話，再拿transform

        protected override void OnActionExecuteImplement()
        {
            var comp = _targetVarRef.Value;
            _target.SetParent(comp.transform);
        }
    }
}