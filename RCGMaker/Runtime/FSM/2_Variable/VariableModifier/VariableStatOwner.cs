using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VariableStatOwner : MonoBehaviour
    {
        [PreviewInInspector] [AutoChildren] VarStat[] _variableStats;
        public VarStat[] VariableStats => _variableStats;
    }
}