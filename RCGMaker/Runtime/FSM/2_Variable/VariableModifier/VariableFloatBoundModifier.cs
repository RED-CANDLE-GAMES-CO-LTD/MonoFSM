using System.Collections;
using System.Collections.Generic;
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

public abstract class AbstractVariableModifier<T> : MonoBehaviour
{
    public abstract T BeforeSetValueModifyCheck(T value);
    public abstract T AfterGetValueModifyCheck(T value);
}


//限制VariableFloat的最小最大值，可以用RCGEventSender倒接事件
public class VariableFloatBoundModifier : MonoBehaviour, IVariableFloatSetOperation
{
    // [Auto] VariableFloat variable;
    public float min = 0;

    [HideIf(nameof(MaxVar))]
    public float max = 1;

    //ex: 血量
    //這會不會很麻煩每次都要設定？

    [SerializeField] VariableFloat MaxVar; //好像應該用繼承的
    [ShowInInspector] private float MaxValue => MaxVar != null ? MaxVar.CurrentValue : max;


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
}
