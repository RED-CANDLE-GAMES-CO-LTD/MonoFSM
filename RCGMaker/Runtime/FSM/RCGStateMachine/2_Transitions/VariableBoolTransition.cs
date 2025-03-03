using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

//監聽variable變化讓state轉換？
//監聽condition是不是比較泛用，用組合的
//lazy update condition
public class VariableBoolTransition : AbstractStateTransition, ILevelResetStart, ITransitionChecker
{
    protected override string GetNameByBehaviour()
    {
        return "[Transition] =>" + Target.name + " when " + _monoVariableNode.name + " is " + TargetValue;
    }

    [FormerlySerializedAs("variableNode")] [Required] [Header("When")] [PropertyOrder(-1)] [DropDownRef]
    public VarBool _monoVariableNode;


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
    }


    private void OnValueChange(bool value)
    {
        if (value == TargetValue)
        {
            this.Log("OnValueChange TransitionCheck", TargetValue);
            TransitionCheck();
        }
    }

    private void OnDestroy()
    {
        _monoVariableNode.Field.RemoveListener(OnValueChange, this);
    }


    public void LevelResetStart()
    {
        if (_monoVariableNode == null)
        {
            Debug.LogError("VariableNode is null", this);
            return;
        }

        this.Log("VariableBoolTransition Awake", _monoVariableNode.name);

        //不該作為transition, 而是作為event?
        //FIXME: 這個沒有管到優先順序....會自己觸發, 感覺不太好，應該是和lastValue比較決定要？
        _monoVariableNode.Field.AddListener(OnValueChange, this);
    }
}