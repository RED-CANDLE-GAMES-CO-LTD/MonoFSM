using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._1_Conditions
{
    /// <summary>
    /// 把另一處已經組好的條件當成自己的結果（proxy）。當同一個判斷要被多個 state / action /
    /// value source 共用時，掛這顆指過去，不要複製一份條件節點。
    /// 通常指向 VariableFolder（blackboard）底下那顆權威的條件。
    /// _proxyCondition 為 null 時條件視為不成立。
    /// </summary>
    public class ConditionRef : AbstractConditionBehaviour
    {
        //FIXME: 應該要只抓到VariableFolder下的(Blackboard)的
        [DropDownRef]
        [SerializeField]
        private AbstractConditionBehaviour _proxyCondition;
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
