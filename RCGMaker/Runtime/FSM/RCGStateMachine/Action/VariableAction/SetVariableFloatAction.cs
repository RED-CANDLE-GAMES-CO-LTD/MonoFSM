using System.Collections;
using System.Collections.Generic;
using RCGMaker.Runtime.FSM._2_Variable;
using UnityEngine;


namespace RCGFSM.Variable
{
    public class SetVariableFloatAction : AbstractStateAction
    {
        public MonoVariableFloat targetFlag;
        public float TargetValue;

        protected override void OnStateEnterImplement()
        {
            targetFlag.SetValue(TargetValue, this);
        }
    }
}