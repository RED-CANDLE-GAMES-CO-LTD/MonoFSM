using System.Collections;
using System.Collections.Generic;
using Ludiq.Reflection;
using UnityEngine;

namespace RCGFSM.Variable
{
    public class SetVariableIntAction : AbstractStateAction
    {
        [DropDownRef] public VarInt targetFlag;
        public int TargetValue;


        protected override void OnStateEnterImplement()
        {
            targetFlag.SetValue(TargetValue, this);
        }
    }
}