using MonoFSM.Core.Simulate;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.Condition
{
    public class ConditionEnableTarget : AbstractConditionActivateTarget
    {
        [FormerlySerializedAs("target")] [SerializeField]
        private Behaviour _target;

        // public Component target;
        public override void ActivateCheck()
        {
            _target.enabled = result;
            // Debug.Log("ConditionEnableTarget: " + _target + "  enabled:" + result, _target);
        }
    }
}