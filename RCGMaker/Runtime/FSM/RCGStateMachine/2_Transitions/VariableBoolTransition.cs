using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

//監聽variable變化讓state轉換？
//FIXME: 還沒有測試過唷, 現在listen應該會錯
public class VariableBoolTransition : AbstractStateTransition
{
    [Required] public VariableBool variableNode;
    public float delay;
    // private Tuple<float> _delayParam;

    protected override void Awake()
    {
        base.Awake();

        // _delayParam = new Tuple<float>(delay);
        
        variableNode.Field.AddListener(this, new Tuple<float>(delay),
            (t, param, value) =>
            {
                if (value)
                    t.TransitionCheck(param.Item1);
            });
    }
}