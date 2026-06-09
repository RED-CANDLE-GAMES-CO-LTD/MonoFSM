using System;
using MonoFSM.Core.Condition;
using MonoFSM.Core.Simulate;

namespace _1_MonoFSM_Core.Runtime._1_Conditions.Activator
{
    public class SimpleActivateUpdateChecker : AbstractConditionUpdateChecker, IUpdateSimulate
    {
        private void OnValidate()
        {
            _forceCheckWhenDisabled = true;
        }
        //FIXME: 違反？
        // bool IUpdateSimulate.IsUpdating => true; //不管怎樣都要檢查，因為他是自己要關掉自己

        protected override void ActivateCheckImplement(bool isValid)
        {
            if (gameObject.activeSelf != isValid)
                gameObject.SetActive(isValid);
        }
    }
}
