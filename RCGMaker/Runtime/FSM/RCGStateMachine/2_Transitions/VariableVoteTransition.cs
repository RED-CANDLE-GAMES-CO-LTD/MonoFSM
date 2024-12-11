using System;
using RCGMaker.Runtime.Vote;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.FSM.RCGStateMachine._2_Transitions
{
    public class VariableVoteTransition:AbstractStateTransition
    {
        [Required]
        [Header("When")]
        [PropertyOrder(-1)]
        [DropDownRef]
        public VariableVote _vote; //FIXME: 可以用interface IBoolVariable? 可以和variable bool 合併
        
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
            
            _vote._vote.OnVoteChange.AddListener(OnValueChange);
        }

        private void OnValueChange(bool arg0)
        {
            if (arg0 == TargetValue)
                TransitionCheck();
        }

        private void OnDestroy()
        {
            _vote._vote.OnVoteChange.RemoveListener(OnValueChange);
        }
    }
}