using System;
using MonoFSM.Runtime.Vote;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Variable._2_Transitions
{
    //FIXME: 用condition
    [Obsolete]
    public class VariableVoteTransition : StateTransition
    {
        [Required] [Header("When")] [PropertyOrder(-1)] [DropDownRef]
        public MonoVariableVote _vote; //FIXME: 可以用interface IBoolVariable? 可以和variable bool 合併

        [Header("Equals To")] [PropertyOrder(-1)]
        public bool TargetValue;

        protected override void Awake()
        {
            base.Awake();
            // variableNode.Field.AddListener(value =>
            // {
            //     if (value == TargetValue)
            //         TransitionCheck();
            // }, this);
            // if (_vote == null)
            // {
            //     Debug.LogError("VariableNode is null",this);
            //     return;
            // }

            _vote.Vote.OnVoteChange.AddListener(OnValueChange);
        }

        private void OnValueChange(bool arg0)
        {
            // if (arg0 == TargetValue)
            //     TransitionCheck();
            Debug.LogError("Deprecated", this);
        }

        private void OnDestroy()
        {
            _vote.Vote.OnVoteChange.RemoveListener(OnValueChange);
        }
    }
}