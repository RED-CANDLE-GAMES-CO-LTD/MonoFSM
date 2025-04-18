using MonoFSM.Variable.Attributes;
using RCGMaker.Core.Attributes;
using RCGMakerFSM.VarRef;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Backpack.Actions
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
        private SourceValueRef _sourceValueRef;

        [InlineEditor] [AutoChildren] [CompRef]
        private TargetVarRef _targetVarRef;

        // [AutoChildren] IConfigVar SourceValue; //FIXME; 怎麼用component...要手動assgin了嗎
        // [AutoChildren] IVariableProvider TargetVariable;
        [PreviewInInspector] private IEffectReceiver _lastReceiver;

        protected override string Description => $"Assign value from {_sourceValueRef} to {_targetVarRef}";

        protected override void OnStateEnterImplement()
        {
            // throw new NotImplementedException();
            var sourceValue = _sourceValueRef.GetValue();
            var targetVar = _targetVarRef.VarRaw;
            if (targetVar == null)
            {
                Debug.LogError("AssignValueAction: No variable found", this);
                return;
            }

            targetVar.SetValue(sourceValue, this);
            Debug.Log($"AssignValueAction: Set value {sourceValue} to {targetVar}", this);
        }

        public override void EventReceived(IEffectHitData arg)
        {
            // var receiver = arg.Receiver as MonoBehaviour;
            // _lastReceiver = arg.Receiver;
            // Debug.Log("SetObjectVariableFromReceiver EventReceived", receiver);
            // if (receiver == null)
            // {
            //     Debug.LogError("SetObjectVariableFromReceiver: Receiver is not a MonoBehaviour",this);
            //     return;
            // }
            if (_targetVarRef == null)
            {
                Debug.LogError("AssignValueAction: No target variable reference", this);
                return;
            }

            var variable = _targetVarRef.VarRaw;
            if (variable == null)
            {
                Debug.LogError("AssignValueAction: No variable found", this);
                return;
            }

            Debug.Log("AssignValueAction: Set value to " + variable, variable);
            var value = _sourceValueRef.GetValue();
            Debug.Log("AssignValueAction: Set value: " + value);
            variable.SetValue(value, this);
            // TargetVariable.GetVariable().SetValue(SourceValue.GetValue(),this);
            // if (sourceType == SourceType.DescriptableData)
            // {
            //     //FIXME: 效能好像不好？
            //     var descriptable = receiver.GetMonoDescriptableInstance(_monoDescriptableTag); //FIXME: 換成effect resolver?
            //     ObjectVariableToSet.RawValue = descriptable.data;
            // }
            // else if (sourceType == SourceType.MonoDescriptable)
            // {
            //     var descriptable = receiver.GetMonoDescriptableInstance(_monoDescriptableTag);
            //     ObjectVariableToSet.RawValue = descriptable;
            // }
            // else
            // {
            //     var variableFound = receiver.FindVariableOfBinder<AbstractReferenceVariable>(varTag);
            //     if (variableFound == null)
            //     {
            //         Debug.LogError("SetObjectVariableFromReceiver: No variable found of Tag"+varTag,receiver);
            //         return;
            //     }
            //
            //     Debug.Log("SetObjectVariableFromReceiver variableFound"+ variableFound.RawValue, variableFound);
            //     ObjectVariableToSet.RawValue = variableFound.RawValue;
            //     Debug.Log("SetObjectVariableFromReceiver ObjectVariableToSet"+ ObjectVariableToSet, ObjectVariableToSet);
            // }
        }

        // public VariableTag refVariableTag => varTag;
    }
}