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

    [Required] public VariableBool variableNode;

    public bool TargetValue;
    // public float delay;
    // private Tuple<float> _delayParam;
    
    protected override void Awake()
    {
        base.Awake();

        // _delayParam = new Tuple<float>(delay);

        variableNode.Field.AddListener(value =>
        {
            if (value == TargetValue)
                TransitionCheck();
        }, this);
    }
}