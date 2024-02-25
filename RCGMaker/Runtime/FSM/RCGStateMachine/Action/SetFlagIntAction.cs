using System.Collections;
using System.Collections.Generic;
using Ludiq.Reflection;
using UnityEngine;

namespace RCGFSM.Variable
{
    public class SetFlagIntAction : AbstractStateAction
    {
        public VariableInt targetFlag;
        public int TargetValue;


        protected override void OnStateEnterImplement()
        {
            targetFlag.Value = TargetValue;
        }
    }
}

