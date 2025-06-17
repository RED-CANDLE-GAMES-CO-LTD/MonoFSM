using System;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using UnityEngine;

namespace RCGMakerFSMCore.Runtime.Action.DebugAction
{
    public class LogValueRefAction : AbstractStateAction
    {
        //FIXME: sourceValue? targetValue? IConfigVar?
        [CompRef] [AutoParent] private IValueProvider _valueRef;

        protected override void OnStateEnterImplement()
        {
            Debug.Log($"LogValueRefAction: {_valueRef?.GetValue()}", this);
        }
    }
}