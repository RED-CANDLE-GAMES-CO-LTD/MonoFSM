public class VarString : GenericMonoVariable<GameFlagString, FlagFieldString, string>
{
    public string StringValue => CurrentValue;
    // public override GameFlagBase FinalData => BindData;
}