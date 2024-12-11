using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

//監聽variable變化讓state轉換？
//FIXME: 還沒有測試過唷, 現在listen應該會錯
//監聽condition是不是比較泛用，用組合的
//lazy update condition
public class VariableBoolTransition : AbstractStateTransition
{
    protected override string GetNameByBehaviour()
    {
        return "[Transition] =>" + Target.name + " when " + variableNode.name + " is " + TargetValue;
    }

    [Required]
    [Header("When")] [PropertyOrder(-1)]
    [DropDownRef]
    public VariableBool variableNode;


    [Header("Equals To")] [PropertyOrder(-1)] 
    public bool TargetValue;
    // public float delay;
    // private Tuple<float> _delayParam;
    
    protected override void Awake()
    {
        base.Awake();
        // variableNode.Field.AddListener(value =>
        // {
        //     if (value == TargetValue)
        //         TransitionCheck();
        // }, this);
        if (variableNode == null)
        {
            Debug.LogError("VariableNode is null",this);
            return;
        }
            
        variableNode.Field.AddListener(OnValueChange, this);
    }

    private void OnValueChange(bool value)
    {
        if (value == TargetValue)
            TransitionCheck();
    }

    private void OnDestroy()
    {
        variableNode.Field.RemoveListener(OnValueChange, this);
    }
}