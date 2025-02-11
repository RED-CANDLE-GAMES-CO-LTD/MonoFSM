using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VariableStatOwner : MonoBehaviour
    {
        [PreviewInInspector] [AutoChildren] MonoVariableStat[] _variableStats;
        public MonoVariableStat[] VariableStats => _variableStats;
    }
}