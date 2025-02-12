using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;


//get operation?
public interface IVariableFloatOperation // //乘區  好像不是variable, 應該是 Effect的FinalValue Calculation
{
    float ApplyOperation(float value);
}

public interface IVariableFloatSetOperation //很確定是set variable時的operation
{
    float SetOperation(float value);
}

public interface AbstractVariableModifier<T>
{
    T BeforeSetValueModifyCheck(T value);
    T AfterGetValueModifyCheck(T value);
}


//限制VariableFloat的最小最大值，可以用RCGEventSender倒接事件
public class VariableFloatBoundModifier : MonoBehaviour, AbstractVariableModifier<float>
{
    [PreviewInInspector] [AutoParent] VariableFloat _monoVariable;

    // [Auto] VariableFloat variable;
    [HideIf(nameof(MinVar))] public float min = 0;

    [HideIf(nameof(MaxVar))] public float max = 1;

    //ex: 血量
    //這會不會很麻煩每次都要設定？

    [DropDownRef] [SerializeField] VariableFloat MinVar;
    [DropDownRef] [SerializeField] VariableFloat MaxVar; //好像應該用繼承的
    [ShowInInspector] public float MaxValue => MaxVar != null ? MaxVar.CurrentValue : max;
    [ShowInInspector] public float MinValue => MinVar != null ? MinVar.CurrentValue : min;
    public float Percentage => (_monoVariable.CurrentValue - MinValue) / (MaxValue - MinValue);

    public UnityEvent OnMin;
    public UnityEvent OnMax;

    public float SetOperation(float value)
    {
        if (value < min)
        {
            value = min;
            OnMin.Invoke();
        }

        if (value > MaxValue)
        {
            value = MaxValue;
            OnMax.Invoke();
        }

        return value;
    }

    public float BeforeSetValueModifyCheck(float value)
    {
        return SetOperation(value);
    }

    public float AfterGetValueModifyCheck(float value)
    {
        return value; //要再bound一次嗎？
    }
}