using System;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

//FIXME: 還沒有測試過唷, 現在listen應該會錯
//FIXME: 應該用condition就好了，這個囉唆
[Obsolete]
public class VarIntTransition : StateTransition
{
    [FormerlySerializedAs("_monoVariableNode")] [FormerlySerializedAs("variableNode")] [Required]
    public VarFloat _monoVarNode;

    public float delay;

    // private Tuple<float> _delayParam;
    public float EqualValue;

    protected override void Awake()
    {
        base.Awake();

        // _delayParam = new Tuple<float>(delay);
        // variableNode.Field.AddListener(this, new Tuple<float,float>(delay, EqualValue),
        //     (t, param, value) =>
        //     {
        //         if (Mathf.Approximately(param.Item2,value))
        //             t.TransitionCheck(param.Item1);
        //     });
    }

    private void Update() //FIXME: 先暴力polling判斷
    {
        // if (Mathf.Approximately(_monoVarNode.CurrentValue, EqualValue))
        //     TransitionCheck(delay);
    }
}