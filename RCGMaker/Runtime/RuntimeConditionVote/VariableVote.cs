using System;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime.Vote
{
    public class VariableVote:AbstractVariable
    {
        // [SerializeField]
        public RuntimeConditionVote _vote = new RuntimeConditionVote();
        public override GameFlagBase FinalData { get; }
        public override Type FinalDataType { get; }
        public override object objectValue => _vote.Result;
        [PreviewInInspector]
        public bool Result => _vote.Result;
        // public void Vote(bool vote, MonoBehaviour m)
        // {
        //     _vote.Vote(m, vote);
        // }
        
    }
}