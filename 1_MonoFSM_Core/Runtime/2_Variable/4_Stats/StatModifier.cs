using UnityEngine;
using UnityEngine.Serialization;

using Sirenix.OdinInspector;

using MonoFSM.Core.DataProvider;
using MonoFSM.Variable;

public enum StatModType
{
    Flat = 0,
    PercentAdd = 1,
    PercentMult = 2,
    Overwrite = 3
}

public enum StatModDurationType
{
    Permanent = 0, //
    Temporary = 1
}

public interface IStatModifierOwner //是誰改數值的
{
    public bool IsActivated { get; }
}

// [System.Serializable]
// public class StatModifierPro : IStatModifer
// {
//     public VariableFloatProvider _targetProvider;
//     public VariableFloatProvider _valueProvider;
//     public StatModType _type = StatModType.Flat;
//     public int _order;
//
//     public VariableTag targetStatTag => _targetProvider._varTag;
//     public int GetOrder => _order;
//     public StatModType GetModType => _type;
//     public float GetValue => _valueProvider.Value;
//     public Object Source => _targetProvider.Variable;
//     public bool IsValid => true;
// }

public interface IStatModifer
{
    public VariableTag targetStatTag { get; } //為什麼要這個？
    public int GetOrder { get; }
    public StatModType GetModType { get; }
    public float GetValue { get; }
    public Object Source { get; }
    bool IsValid { get; }
    public bool IsDirty { get; } //這個是用來判斷是否需要重新計算的？還是說每次都要計算？ ///可是這樣要重新resolve?
}

[System.Serializable]
public class StatModifier : IStatModifer //以前是給Characterstat用的
{
    public VariableTag statTag;
    public float Value;
    public StatModType _statModType;
    public StatModDurationType DurationType; //FIXME: 重點是啥？
    public int Order;
    // public readonly object Source;

    [ShowInInspector] public Object _source;
    public Object Source => _source;
    public bool IsValid => true;
    public bool IsDirty => false; //不可能會變

    public void SetSource(Object source)
    {
        _source = source;
    }

    public StatModifier(float value, StatModType type, int order, IStatModifierOwner source)
    {
        Value = value;
        _statModType = type;
        Order = order;
        _source = source as Object; //TODO: 一定要有source嗎？
    }

    public StatModifier(float value, StatModType type, IStatModifierOwner source) : this(value, type, (int)type, source)
    {
    }

    public VariableTag targetStatTag => statTag;
    public int GetOrder => Order;
    public StatModType GetModType => _statModType;
    public float GetValue => Value;
}