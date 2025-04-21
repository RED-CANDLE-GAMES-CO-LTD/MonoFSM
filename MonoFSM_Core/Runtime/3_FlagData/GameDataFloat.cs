using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class DataFloatModifyEntry : DataModifyEntry<GameDataBool, FlagFieldBool, bool>
{
}

[CreateAssetMenu(fileName = "NewFloatFlag", menuName = "GameFlag/Float", order = 1)]
public class GameDataFloat : AbstractScriptableData<FlagFieldFloat, float>
{
    [InlineEditor] [SerializeField] private StatData MaxStat;

    [ShowInInspector] public float MaxValue => MaxStat ? MaxStat.Value : 99999;

    [Header("過多會傳到這裡存起來")] public GameDataFloat ExternalRepository;

    public void RestoreFromRepository()
    {
        if (ExternalRepository)
        {
            //從AmmoRepository拿到子彈
            var toFull = MaxValue - CurrentValue;

            if (ExternalRepository.CurrentValue < toFull) toFull = ExternalRepository.CurrentValue;
            ExternalRepository.CurrentValue -= toFull;
            CurrentValue += toFull;
        }
    }

    private const float minValue = 0;
    // [InlineEditor()]
    // [SerializeField] private StatData AddValuePerMinute;
    // [InlineEditor()]
    // [SerializeField] private StatData ReduceValuePerMinute;

    // [Header("開始扣的話要乘上消耗倍率")] public const float ReducePunishReduceRatio = 2;

    public int ValueInt => (int)CurrentValue;
    public float Value => CurrentValue;

    public override float CurrentValue
    {
        get => base.CurrentValue;
        set
        {
            //太多的時候存到repository
            if (ExternalRepository)
                if (value > MaxValue)
                {
                    ExternalRepository.CurrentValue += value - MaxValue;
                    value = MaxValue;
                }

            //bound by max
            if (value > MaxValue)
                value = MaxValue;
            base.CurrentValue = value;
        }
    }


    // [ReadOnly]
    // [ShowInInspector]
    // public float CurrentRate
    // {
    //     get
    //     {
    //         if (AddValuePerMinute == null || ReduceValuePerMinute == null)
    //             return 0;
    //         var addValue =  (AddValuePerMinute.Value - ReduceValuePerMinute.Value) /60;
    //         if (addValue < 0)
    //         {
    //             //消耗時，懲罰倍率
    //             addValue *= ReducePunishReduceRatio; //TODO: 特規，要
    //         }
    //
    //         return addValue;
    //     }
    // }
    // [ReadOnly]
    // [ShowInInspector]
    // public float EstimateTimeCountDown
    // {
    //     get
    //     {
    //         return CurrentValue / CurrentRate;
    //     }
    // }
    // public void UpdateValue() //要讓誰update，
    // {
    //     var tempValue = CurrentValue;
    //     tempValue += Time.deltaTime * CurrentRate;
    //     if (tempValue > MaxStat.Value)
    //         tempValue = MaxStat.Value;
    //     if (tempValue < minValue)
    //         tempValue = minValue;
    //     CurrentValue = tempValue;
    //     //TODO: CurrentValue會太早變成0...要float才對最後再轉型，GameFlagInt不好用
    // }
}

//ScriptableDataFloat