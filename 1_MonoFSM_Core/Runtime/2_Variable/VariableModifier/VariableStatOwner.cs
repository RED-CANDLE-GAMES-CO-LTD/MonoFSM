using UnityEngine;
using MonoFSM.Core.Attributes;

namespace MonoFSM.Variable
{
    public class VariableStatOwner : MonoBehaviour
    {
        [PreviewInInspector] [AutoChildren] VarStat[] _variableStats;
        public VarStat[] VariableStats => _variableStats;
    }
}