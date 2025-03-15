public class VariableString : GenericMonoVariable<GameFlagString, FlagFieldString, string>
{
    public string StringValue => CurrentValue;
    public override GameFlagBase FinalData => ScriptableData;
}