using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RCGFSM.Variable
{
    public class SetFlagFloatAction : AbstractStateAction
    {
        public VariableFloat targetFlag;
        public float TargetValue;

        protected override void OnStateEnterImplement()
        {
            targetFlag.SetValue(TargetValue, this);
        }
    }
}
