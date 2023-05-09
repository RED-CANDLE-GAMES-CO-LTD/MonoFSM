
using UnityEngine;

public enum StatModType
{
    Flat = 100,
    PercentAdd = 200,
    PercentMult = 300,
}

public enum StatModDuration
{
    Permanent = 0,
    Temporary = 1
}

[System.Serializable]
public class StatModifier
{
    public float Value;
    public StatModType Type;

    public StatModDuration Duration;
    public int Order;
    public readonly object Source;
    public ScriptableObject dataSource;

    public StatModifier(float value, StatModType type, int order, object source)
    {
        Value = value;
        Type = type;
        Order = order;
        Source = source;
        if (source is ScriptableObject)
            dataSource = source as ScriptableObject;
    }

    public StatModifier(float value, StatModType type) : this(value, type, (int)type, null) { }

    public StatModifier(float value, StatModType type, int order) : this(value, type, order, null) { }

    public StatModifier(float value, StatModType type, object source) : this(value, type, (int)type, source) { }
}
