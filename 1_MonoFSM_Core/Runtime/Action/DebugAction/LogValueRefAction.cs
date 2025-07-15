using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using MonoFSM.Core;
using UnityEngine;

namespace RCGMakerFSMCore.Runtime.Action.DebugAction
{
    public class LogValueRefAction : AbstractStateAction, IEditorOnly
    {
        //FIXME: sourceValue? targetValue? IConfigVar?
        [CompRef] [AutoParent] private IValueProvider _valueRef;

        protected override void OnActionExecuteImplement()
        {
            Debug.Log($"LogValueRefAction: {_valueRef?.Get<object>()}", this);
        }
    }
}