
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector;

public class VariableFloat : GenericVariable<ScriptableDataFloat, FlagFieldFloat, float>, IFloatValue
{
    [PropertyOrder(-1)]
    [SOConfig("VariableType")] public VariableTag VarType;
    
}