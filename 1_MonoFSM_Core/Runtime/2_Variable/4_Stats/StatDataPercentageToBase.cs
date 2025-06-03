using UnityEngine;

using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "StatDataPercentageToBase", menuName = "ScriptableObjects/StatDataPercentageToBase",
    order = 1)]

//CurrentValue和base之間的比例
public class StatDataPercentageToBase : StatData
{
    public StatData refData;
    [ShowInInspector] public override float Value 
        => refData 
            ? refData.Value / refData.Stat.BaseValue 
            : 0;
}