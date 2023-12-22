public enum Operator
{
    Equals, //==
    NotEqual, // !=
    GreaterThan, // >
    LessThan, // <
    IsEven
}
public class VariableFloat : VariableType<ScriptableDataFloat, FlagFieldFloat, float>
{
    // [Component(typeof(AbstractVariableModifier<float>))]
    private void AddModifier()
    {
    }
}