using MonoFSM.Core.Attributes;
using MonoFSM.EditorExtension;
using MonoFSM.Variable;
using UnityEngine;

public interface IIntProvider
{
    public int IntValue { get; }
}

/// <summary>可綁 VariableTag／GameFlagInt 的整數變數，支援本地值、網路覆寫與整數 provider 讀取。</summary>
public class VarInt : AbstractFieldVariable<GameFlagInt, FlagFieldInt, int>, IIntProvider,
    IStringTokenVar
{
    public int IntValue => CurrentValue;

    // public override GameFlagBase FinalData => BindData;
    protected override bool IsLocalValueExist => CurrentValue != 0;
    public override string ValueInfo => IntValue.ToString();
    public override bool IsDrawingValueInfo => true;

    [Component(AddComponentAt.Same)]
    [AutoChildren(false)]
    [SerializeField]
    private VariableIntBoundModifier _boundModifier;

    [ShowInPlayMode]
    public int Min => _boundModifier ? _boundModifier.MinValue : 0;

    [ShowInPlayMode]
    public int Max => _boundModifier ? _boundModifier.MaxValue : int.MaxValue;

    public bool IsMax => CurrentValue >= Max;
    public bool IsMin => CurrentValue <= Min;
}
