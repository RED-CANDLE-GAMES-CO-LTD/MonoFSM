
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

public interface IStatModifierOwner //為什麼要做這個？
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
