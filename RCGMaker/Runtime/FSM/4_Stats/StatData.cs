using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "StatData", menuName = "ScriptableObjects/StatData", order = 1)]
public class StatData : ScriptableObject, IStringData
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
    public virtual float Value => stat.Value;

    public List<StatData> baseRatios;

    private float CalculateFinalValue()
    {
        var finalValue = Value;
        foreach (var ratio in baseRatios) finalValue *= ratio.Value;
        return finalValue;
    }

    public float ValueWithBaseRatio => CalculateFinalValue();

    [TextArea]
    public string note;

    public string GetString()
    {
        return Value.ToString();
    }
}
