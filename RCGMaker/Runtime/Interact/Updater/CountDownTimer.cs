using System;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using UnityEngine;

//耐力條，體幹..應該都可以用這個套？
public class CountDownTimer:MonoBehaviour
{
    public VariableBool isConsuming;
    [DropDownRef]
    public VariableStat maxValueStat;
    [DropDownRef]
    public VariableStat increaseSpeedStat; //regen
    [DropDownRef]
    public VariableStat decreaseSpeedStat; //consume
    [DropDownRef]
    public VariableFloat currentTime;
    [DropDownRef]
    public VariableStat TimeToRegen;
    [PreviewInInspector]
    float pauseTimeCounter;
    public enum CountType
    {
        Increase,
        Decrease,
        Pause
    }
    CountType countType;
    private void Update() //FIXME: 用update不太好？
    {
        // Debug.Log("CountDownTimer Update"+currentTime.CurrentValue+" last:"+currentTime.LastValue);
        //FIXME; 執行順序會導致這個判定沒有用，已經CommitValue了？
        //直接設定對Counter pause，然後扣value是不是比較快？
        if(currentTime.CurrentValue + 1 < currentTime.LastValue) //比較上一個frame，如果是減少，就是消耗
        {
            countType = CountType.Pause;
            pauseTimeCounter = 0;
   
        }
        else if(isConsuming.CurrentValue && decreaseSpeedStat.FinalValue > 0)
        {
            countType = CountType.Decrease;
            pauseTimeCounter = 0;
        }
        else
        {
            pauseTimeCounter += Time.deltaTime;
            if(pauseTimeCounter >= TimeToRegen.FinalValue)
            {
                countType = CountType.Increase;
            }
            else
            {
                countType = CountType.Pause;
            }
        }
      
        switch (countType)
        {
            case CountType.Increase:
                currentTime.CurrentValue += increaseSpeedStat.FinalValue * Time.deltaTime;
                if (currentTime.CurrentValue >= maxValueStat.FinalValue)
                {
                    currentTime.CurrentValue = maxValueStat.FinalValue;
                }
                break;
            case CountType.Decrease:
                currentTime.CurrentValue -= decreaseSpeedStat.FinalValue * Time.deltaTime;
                if (currentTime.CurrentValue <= 0)
                {
                    currentTime.CurrentValue = 0;
                }
                break;
            case CountType.Pause:
                break;
        }
    }
    //FIXME: 看condition?
}
