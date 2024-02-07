using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class AbstractStatData : ScriptableObject
{
    public abstract float Value { get; }
#if UNITY_EDITOR
    [TextArea] public string note;
#endif
}

[CreateAssetMenu(fileName = "StatData", menuName = "ScriptableObjects/StatData", order = 1)]
public class StatData : AbstractStatData, IStringData
{
//reset game的時候，要清除

    public void Clear() //重load時清除
    {
        stat?.Clear();
    }
    [Header("能力值")]
    // public FlagFieldStat flagStat;
    //TODO:
    [SerializeField]
    private CharacterStat stat;

    public CharacterStat Stat => stat;
    [ReadOnly]
    [ShowInInspector]
    [PropertyOrder(-1)]
    public override float Value => stat.Value;

    public List<AbstractStatData> baseRatios; //為什麼要list

    private float CalculateFinalValue()
    {
        var finalValue = Value;
        foreach (var ratio in baseRatios) finalValue *= ratio.Value;
        return finalValue;
    }

    public float ValueWithBaseRatio => CalculateFinalValue();

    public string GetString()
    {
        return Value.ToString();
    }
}
