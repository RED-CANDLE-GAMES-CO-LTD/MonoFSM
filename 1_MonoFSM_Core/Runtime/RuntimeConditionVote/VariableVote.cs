using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace MonoFSM.Runtime.Vote
{
    public class MonoVariableVote : AbstractMonoVariable
    {
        // [SerializeField]
        private readonly RuntimeConditionVote _vote = new();
        public RuntimeConditionVote Vote => _vote;
        // public override GameFlagBase FinalData { get; }
        // public override Type FinalDataType { get; }
        // public override void AddListener<T>(UnityAction<T> action)
        // {
        //     if (action == null) return;
        //     throw new NotImplementedException("MonoVariableVote does not support AddListener with UnityAction<T>.");
        // }

        public override Type ValueType => typeof(bool);
        public override object objectValue => _vote.Result;

        protected override void SetValueInternal<T>(T value, Object byWho = null)
        {
            _vote.Vote(byWho, (bool)(object)value);
        }

        public override bool IsValueExist => true;

        [PreviewInInspector] public bool Result => _vote.Result;
        // public void Vote(bool vote, MonoBehaviour m)
        // {
        //     _vote.Vote(m, vote);
        // }
    }
}