using MonoFSM.EditorExtension;

public interface IIntProvider
{
    public int IntValue { get; }
}

public class VarInt : AbstractFieldVariable<GameFlagInt, FlagFieldInt, int>, IIntProvider,
    IStringTokenVar
{
    public int IntValue => CurrentValue;

    // public override GameFlagBase FinalData => BindData;
    public override bool IsValueExist => CurrentValue != 0;
    public override string ValueInfo => IntValue.ToString();
    public override bool IsDrawingValueInfo => true;
}
