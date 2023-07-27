using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//把別的值拿來乘上倍率，functional取值
[CreateAssetMenu(fileName = "StatDataRef", menuName = "ScriptableObjects/StatDataRef", order = 1)]
public class StatDataRef : StatData
{
    public float scale = 1;
    public StatData refStat;
    public override float Value => refStat ? refStat.Value * scale : Stat.Value;

    //如何維持原本的reference但改實作？
    //一開始接的就是抽象介面？
}