using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using UIValueBinder;
using UnityEngine;

namespace MonoFSM.Runtime.Variable.Action
{
    //清掉值
    public class ClearObjectVariableAction : AbstractStateAction, IArgEventReceiver<IEffectHitData>,
        IVariableTagSetter
    {
        //FIXME: 這個直接指，不對...

        //FIXME: filter 上面的MonoDescriptableTag的variable?
        public VariableTag _variableTag;
        [DropDownRef] public AbstractObjectVariable objectVariable;

        protected override void OnActionExecuteImplement()
        {
            ClearValue();
        }

        private void ClearValue()
        {
            if (objectVariable != null)
                objectVariable.ClearValue();

            //FIXME: provider的某個variable
            if (_variableTag == null)
            {
                if (objectVariable == null)
                    Debug.LogError("objectVariable & VariableTag is null", this);
                return;
            }

            var variable = GetComponentInParent<UIMonoDescriptableProvider>().MonoInstance.GetVar(_variableTag);
            (variable as AbstractObjectVariable).ClearValue();
        }

        public void ArgEventReceived(IEffectHitData arg)
        {
            ClearValue();
        }

        public VariableTag refVariableTag => _variableTag;
    }
}