using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable.Attributes;
using MonoFSM.VarRefOld;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Backpack.Actions
{
    //從receiver那邊拿到變數，然後設定到自己的變數上 (有點像rebind了)
    //FIXME: 直接set?
    //FIXME: Assign Value to Variable?
    public class AssignValueAction : AbstractStateAction //, IRCGArgEventReceiver<IEffectHitData>
    {
        // public MonoValueProvider TestVariable;
//SourceValueWrapper?
//TargetValueWrapper?

        [InlineEditor] [AutoChildren] [CompRef]
        private TargetVarRef _targetVarRef;

        [InlineEditor] [AutoChildren] [CompRef]
        private SourceValueRef _sourceValueRef;

        // [AutoChildren] IConfigVar SourceValue; //FIXME; 怎麼用component...要手動assgin了嗎
        // [AutoChildren] IVariableProvider TargetVariable;
        [PreviewInInspector] private IEffectReceiver _lastReceiver;

        public override string Description => $"Assign {_sourceValueRef.Description} to {_targetVarRef.Description}";

        protected override void OnActionExecuteImplement()
        {
            if (_sourceValueRef == null)
            {
                Debug.LogError("AssignValueAction: Source value is null", _sourceValueRef);
                return;
            }
            
            var targetVar = _targetVarRef.VarRaw;
          
            if (targetVar == null)
            {
                Debug.LogError("AssignValueAction: No variable found", this);
                return;
            }

            targetVar.SetValueByRef(_sourceValueRef, this);
            Debug.Log($"AssignValueAction: Set value {_sourceValueRef} to {targetVar}", this);
            Debug.Log($"AssignValueAction: {targetVar} Set", targetVar);
        }

        // protected override void OnArgEventReceived(GeneralEffectHitData arg)
        // {
        //     if (_targetVarRef == null)
        //     {
        //         Debug.LogError("AssignValueAction: No target variable reference", this);
        //         return;
        //     }
        //
        //     var variable = _targetVarRef.VarRaw;
        //     if (variable == null)
        //     {
        //         Debug.LogError("AssignValueAction: No variable found", this);
        //         return;
        //     }
        //     
        //     variable.SetValueByRef(_sourceValueRef, this);
        // }

        // public VariableTag refVariableTag => varTag;
    }
}