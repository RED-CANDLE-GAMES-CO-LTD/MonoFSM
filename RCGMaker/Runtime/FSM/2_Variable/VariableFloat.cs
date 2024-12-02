
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector;

public class VariableFloat : GenericVariable<ScriptableDataFloat, FlagFieldFloat, float>, IFloatValue, IValueOfKey<VariableTag>
{
    public VariableTag Key => varTag;
}