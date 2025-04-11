using System;
using RCGMaker.Core.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Runtime.Vote
{
    public class MonoVariableVote : AbstractMonoVariable
    {
        // [SerializeField]
        private readonly RuntimeConditionVote _vote = new();
        public RuntimeConditionVote Vote => _vote;
        public override GameFlagBase FinalData { get; }
        public override Type FinalDataType { get; }
        public override Type ValueType => typeof(bool);
        public override object objectValue => _vote.Result;

        protected override void SetValueInternal<T>(T value, Object byWho = null)
        {
            _vote.Vote(byWho, (bool)(object)value);
        }

        [PreviewInInspector] public bool Result => _vote.Result;
        // public void Vote(bool vote, MonoBehaviour m)
        // {
        //     _vote.Vote(m, vote);
        // }
    }
}