using RCGMaker.Core.Attributes;
using UnityEngine;

namespace MonoFSM.Variable
{
    public class VariableStatOwner : MonoBehaviour
    {
        [PreviewInInspector] [AutoChildren] VarStat[] _variableStats;
        public VarStat[] VariableStats => _variableStats;
    }
}