using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using UIValueBinder;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.Variable.Action
{
    //清掉值
    public class ClearObjectVariableAction
        : AbstractStateAction,
            IArgEventReceiver<IEffectHitData>
    {
        public override string Description => "Clear" + _objectVariable?.name;
        //FIXME: 這個直接指，不對...

        //FIXME: filter 上面的MonoDescriptableTag的variable?
        // public VariableTag _variableTag;

        [FormerlySerializedAs("objectVariable")] [DropDownRef]
        public AbstractMonoVariable _objectVariable;

        protected override void OnActionExecuteImplement()
        {
            ClearValue();
        }

        private void ClearValue()
        {
            if (_objectVariable != null)
                _objectVariable.ClearValue();

            //FIXME: provider的某個variable
            // if (_variableTag == null)
            // {
            //     if (_objectVariable == null)
            //         Debug.LogError("objectVariable & VariableTag is null", this);
            //     return;
            // }

            // var variable = GetComponentInParent<UIMonoDescriptableProvider>()
            //     .MonoInstance.GetVar(_variableTag);
            // variable.ClearValue();
        }

        public void ArgEventReceived(IEffectHitData arg)
        {
            ClearValue();
        }

        // public VariableTag refVariableTag => _variableTag;
    }
}
