using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VariableStatOwner:MonoBehaviour
    {
        [PreviewInInspector]
        [AutoChildren] VariableStat[] _variableStats;
        public VariableStat[] VariableStats => _variableStats;
    }
}