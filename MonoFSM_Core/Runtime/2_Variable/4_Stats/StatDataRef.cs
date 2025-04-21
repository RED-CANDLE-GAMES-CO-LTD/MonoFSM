using UnityEngine;
using UnityEngine.Serialization;

//把別的值拿來乘上倍率，functional取值
[CreateAssetMenu(fileName = "StatDataRef", menuName = "ScriptableObjects/StatDataRef", order = 1)]
public class StatDataRef : AbstractStatData
{
    public float scale = 1;

    [FormerlySerializedAs("CurrentStatData")] [FormerlySerializedAs("refStat")]
    public StatData BindingStatData;

    public StatData FallBackStatData;
    private AbstractStatData CurrentStatData => BindingStatData ? BindingStatData : FallBackStatData;
    public override float ValueWithBaseRatio => CurrentStatData.ValueWithBaseRatio * scale;
    public override float Value => CurrentStatData.ValueWithBaseRatio * scale;

    //如何維持原本的reference但改實作？
    //一開始接的就是抽象介面？
}