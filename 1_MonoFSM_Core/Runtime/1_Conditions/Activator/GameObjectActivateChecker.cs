using MonoFSM.Core.Condition;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._1_Conditions.Activator
{
    public class GameObjectActivateChecker : AbstractConditionActivateTarget
    {
        [PropertyOrder(-1)] [Required] public GameObject _target;

        public override void ActivateCheck()
        {
            _target.SetActive(result);
        }
    }
}