
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;

public class VariableFloat : GenericVariable<ScriptableDataFloat, FlagFieldFloat, float>, IFloatValue
{
    [SOConfig("VariableType")] public VariableTag VarType;


    private List<ModifierInjector> _modifierInjectors; //從外部修改值

    //TODO: readonly?
    
}