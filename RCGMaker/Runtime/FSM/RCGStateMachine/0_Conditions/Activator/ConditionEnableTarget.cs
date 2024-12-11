using UnityEngine;

namespace _3_Script._0_RedCandleGamesUtilities.UICanvas.ActivateChecker
{
    public class ConditionEnableTarget : AbstractConditionActivateTarget
    {
        [SerializeField] private Behaviour target;

        // public Component target;
        public override void ActivateCheck()
        {
            target.enabled = result;
            Debug.Log("ConditionEnableTarget: " + target + "  enabled:" + result, target);
        }
    }
}