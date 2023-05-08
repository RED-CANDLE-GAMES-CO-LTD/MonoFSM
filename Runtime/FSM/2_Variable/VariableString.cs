public class VariableString : VariableType<GameFlagString, FlagFieldString, string>, IStringProvider
{
    public string StringValue => Value;
}