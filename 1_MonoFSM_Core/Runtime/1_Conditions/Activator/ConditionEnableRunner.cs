using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.Condition
{
    public class ConditionEnableRunner : AbstractConditionActivateRunner
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
