using System;
using RCGMaker.Core.Attributes;
using MonoFSM.Variable;
using UnityEngine;

//耐力條，體幹..應該都可以用這個套？
//FIXME: 寫的不是很好
public class StaminaTimer : MonoBehaviour
{
    public VarBool isConsuming;

    // [DropDownRef] public VarStat maxValueStat;
    [DropDownRef] public VarStat increaseSpeedStat; //regen
    [DropDownRef] public VarStat decreaseSpeedStat; //consume //兩個可以不一樣快...但如果單純用timer就跟著時間就好了，default 1?
    [DropDownRef] public VarFloat currentTime;
    [DropDownRef] public VarStat TimeToRegen;
    [PreviewInInspector] private float pauseTimeCounter;

    private float IncreaseSpeed => increaseSpeedStat ? increaseSpeedStat.FinalValue : 1;
    private float DecreaseSpeed => decreaseSpeedStat ? decreaseSpeedStat.FinalValue : 1;

    public enum CountType
    {
        Increase,
        Decrease,
        Pause
    }

    public CountType countType;

    private void Update() //FIXME: 用update不太好？
    {
        // Debug.Log("CountDownTimer Update"+currentTime.CurrentValue+" last:"+currentTime.LastValue);
        //FIXME; 執行順序會導致這個判定沒有用，已經CommitValue了？
        //直接設定對Counter pause，然後扣value是不是比較快？
        //想要寫精力條，但這裡太複雜了，應該要再抽一層出來


        if (currentTime.CurrentValue + 1 < currentTime.LastValue) //比較上一個frame，如果是減少，就是消耗
        {
            countType = CountType.Pause;
            pauseTimeCounter = 0;
        }
        else if (isConsuming.CurrentValue && decreaseSpeedStat.FinalValue > 0)
        {
            countType = CountType.Decrease;
            pauseTimeCounter = 0;
        }
        else
        {
            pauseTimeCounter += Time.deltaTime;
            if (pauseTimeCounter >= TimeToRegen.FinalValue)
                countType = CountType.Increase;
            else
                countType = CountType.Pause;
        }

        switch (countType)
        {
            case CountType.Increase:
                currentTime.CurrentValue += IncreaseSpeed * Time.deltaTime;
                // if (currentTime.CurrentValue >= currentTime.Max)
                // {
                //     currentTime.CurrentValue = currentTime.Max;
                // }

                break;
            case CountType.Decrease:
                currentTime.CurrentValue -= DecreaseSpeed * Time.deltaTime;
                // if (currentTime.CurrentValue <= 0)
                // {
                //     currentTime.CurrentValue = 0;
                // }

                break;
            case CountType.Pause:
                break;
        }
    }
    //FIXME: 看condition?
}