using System.Collections.Generic;

using UnityEngine;

using Sirenix.OdinInspector;

public abstract class AbstractStatData : ScriptableObject
{
    public abstract float ValueWithBaseRatio { get; }
    public abstract float Value { get; }
#if UNITY_EDITOR
    [TextArea] public string note;
#endif
}

[CreateAssetMenu(fileName = "StatData", menuName = "ScriptableObjects/StatData", order = 1)]
public class StatData : AbstractStatData, IStringData, INativeData
{
    //reset game的時候，要清除

    public void Clear() //重load時清除
    {
        stat.Clear();
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
    public override float Value => ValueWithBaseRatio;

    [ReadOnly]
    [ShowInInspector]
    [PropertyOrder(-1)]
    private float DesignValue => stat.Value; //設計參數

    public int ValueInt => (int)Value;

    public List<AbstractStatData> baseRatios; //為什麼要list

    private float CalculateFinalValue()
    {
        var finalValue = DesignValue;
        if (baseRatios == null) return finalValue;
        foreach (var ratio in baseRatios)
        {
            if (ratio == this)
            {
                return finalValue;
            }

            finalValue *= ratio.Value;
        }

        return finalValue;
    }

    //加上遊戲全局的修正參數...
    public override float ValueWithBaseRatio => CalculateFinalValue();

    public string GetString()
    {
        return Value.ToString();
    }

    //藥抖等級
    public int GetModifierCount => Stat.StatModifiers.Count + 1;
}