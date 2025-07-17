using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._1_Conditions
{
    public class ConditionReference : AbstractConditionBehaviour
    {
        //FIXME: 應該要只抓到VariableFolder下的(Blackboard)的
        [DropDownRef] [SerializeField] private AbstractConditionBehaviour _proxyCondition;
        protected override bool IsValid => _proxyCondition != null && _proxyCondition.FinalResult;

        public override string Description
        {
            get
            {
                if (_proxyCondition == null)
                    return "No Condition";
                return " Ref: " + _proxyCondition.Description;
            }
        }
    }
}