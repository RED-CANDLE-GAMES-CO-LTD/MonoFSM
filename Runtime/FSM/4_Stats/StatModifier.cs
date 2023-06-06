
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
    Permanent = 0,
    Temporary = 1
}

public interface IStatModifierOwner
{
    public bool IsActivated { get; }
}
[System.Serializable]
public class StatModifier
{
    public float Value;
    public StatModType Type;

    [FormerlySerializedAs("Duration")] public StatModDurationType DurationType;
    public int Order;
    // public readonly object Source;

    [ShowInInspector] public IStatModifierOwner source;

    public StatModifier(float value, StatModType type, int order, IStatModifierOwner source = null)
    {
        Value = value;
        Type = type;
        Order = order;
        // Source = source;
        this.source = source;
    }

    public StatModifier(float value, StatModType type) : this(value, type, (int)type, null) { }

    public StatModifier(float value, StatModType type, int order) : this(value, type, order, null) { }

    // public StatModifier(float value, StatModType type, object source) : this(value, type, (int)type, source) { }
}
