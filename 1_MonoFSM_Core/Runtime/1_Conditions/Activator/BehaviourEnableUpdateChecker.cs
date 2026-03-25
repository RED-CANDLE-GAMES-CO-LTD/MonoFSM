using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.Condition
{
    //有必要獨立 class 嗎？
    public class BehaviourEnableUpdateChecker : AbstractConditionUpdateChecker
    {
        //FIXME: Dropdown filter component in parent node?
        [FormerlySerializedAs("target")]
        [SerializeField]
        private Behaviour _target;

        // public Component target;
        protected override void ActivateCheckImplement(bool isValid) //這裡傳result?
        {
            _target.enabled = isValid;
            // Debug.Log("ConditionEnableTarget: " + _target + "  enabled:" + result, _target);
        }
    }
}
