using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public enum StatModType
{
    Flat = 100,
    PercentAdd = 200,
    PercentMult = 300,
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

[System.Serializable]
public class StatModifierPro : IStatModifer
{
    public VariableFloatProvider _targetProvider;
    public VariableFloatProvider _valueProvider;
    public StatModType _type = StatModType.Flat;
    public int _order;

    public VariableTag targetStatTag => _targetProvider._varTag;
    public int GetOrder => _order;
    public StatModType GetModType => _type;
    public float GetValue => _valueProvider.Value;
}

public interface IStatModifer
{
    public VariableTag targetStatTag { get; }
    public int GetOrder { get; }
    public StatModType GetModType { get; }
    public float GetValue { get; }
}

[System.Serializable]
public class StatModifier //以前是給Characterstat用的
{
    public VariableTag statTag;
    public float Value;
    public StatModType Type;

    [FormerlySerializedAs("Duration")] public StatModDurationType DurationType;
    public int Order;
    // public readonly object Source;

    [ShowInInspector] public ScriptableObject Source;

    public StatModifier(float value, StatModType type, int order, IStatModifierOwner source)
    {
        Value = value;
        Type = type;
        Order = order;
        // Source = source;
        // Debug.Log("[StatModifier Source]" + Source, Source);
        Source = source as ScriptableObject; //TODO: 一定要有source嗎？
    }

    public StatModifier(float value, StatModType type, IStatModifierOwner source) : this(value, type, (int)type, source)
    {
    }

    // public StatModifier(float value, StatModType type, int order) : this(value, type, order, null) { }

    // public StatModifier(float value, StatModType type, object source) : this(value, type, (int)type, source) { }
}