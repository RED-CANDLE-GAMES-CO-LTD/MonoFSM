using UnityEngine;

public interface IIntProvider
{
    public int IntValue { get; }
}

public class VarInt : GenericMonoVariable<GameFlagInt, FlagFieldInt, int>, IIntProvider
{
    public int IntValue => CurrentValue;
    // public override GameFlagBase FinalData => BindData;
    public override bool IsValueExist => CurrentValue != 0;
}