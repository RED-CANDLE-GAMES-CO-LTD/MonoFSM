public class VariableString : GenericVariable<GameFlagString, FlagFieldString, string>, IStringProvider
{
    public string StringValue => Value;
}