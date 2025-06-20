using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewIntFlag", menuName = "GameFlag/Int", order = 1)]

public class GameFlagInt : AbstractScriptableData<FlagFieldInt, int>
{
    [SerializeField]
    StatData MaxStat;

    public int MaxValue => (int)MaxStat.Value;

    const float minValue = 0;
    [InlineEditor()]
    [SerializeField] private StatData AddValuePerMinute;
    [InlineEditor()]
    [SerializeField] private StatData ReduceValuePerMinute;
    [Header("開始扣的話要乘上消耗倍率")]
    public float ReducePunishReduceRatio = 1;  
    public float tempValue;

    [ReadOnly]
    [ShowInInspector]
    public float CurrentRate
    {
        get
        {
            if (AddValuePerMinute == null || ReduceValuePerMinute == null)
                return 0 ;
            var addValue =  (AddValuePerMinute.Value - ReduceValuePerMinute.Value) /60;
            if (addValue < 0)
            {
                //消耗時，懲罰倍率
                addValue *= ReducePunishReduceRatio; //TODO: 特規，要
            }

            return addValue;
        }
    }
    // public void UpdateValue() //要讓誰update，
    // {
    //
    //     tempValue += RCGTime.deltaTime *CurrentRate;
    //     if (tempValue > MaxStat.Value)
    //         tempValue = MaxStat.Value;
    //     if (tempValue < minValue)
    //         tempValue = minValue;
    //     CurrentValue = Mathf.FloorToInt(tempValue);
    //     //TODO: CurrentValue會太早變成0...要float才對最後再轉型，GameFlagInt不好用
    // }

    public override void FlagInitStart()
    {
        base.FlagInitStart();
        tempValue = CurrentValue;
    }
    // public StatData RecoverRate;

    // public FlagFieldInt field;

    // public int CurrentValue
    // {
    //     get
    //     {
    //         return field.CurrentValue;
    //         // if (GameFlagManager.Instance.TestModeFlag.TestMode == TestModeGameFlag.TestType.DeveloperStaticTest)
    //         //     return TestValue;
    //         // return _currentValue;
    //     }
    //     set
    //     {
    //         field.CurrentValue = value;
    //         // _currentValue = value;
    //     }
    // }

}
