using System;
using MonoFSM.Core.Attributes;
using MonoFSM.EditorExtension;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Runtime.Vote
{
    //FIXME: Vote可能獨立根本不是Var?
    public class VarVote : AbstractMonoVariable, IHierarchyValueInfo
    {
        // [SerializeField]
        private readonly RuntimeConditionVote _vote = new();

        [ShowInInspector]
        public RuntimeConditionVote Vote => _vote;

        // public override GameFlagBase FinalData { get; }
        // public override Type FinalDataType { get; }
        // public override void AddListener<T>(UnityAction<T> action)
        // {
        //     if (action == null) return;
        //     throw new NotImplementedException("MonoVariableVote does not support AddListener with UnityAction<T>.");
        // }


        public override void ClearValue()
        {
            _vote.ClearValue();
        }

        public override void SetRaw<T1>(T1 value, Object byWho)
        {
            if (value is bool bValue)
                _vote.Vote(byWho, bValue);
        }

        public override Type ValueType => typeof(bool);

        // public override object objectValue => _vote.Result;

        public override T GetValue<T>()
        {
            var value = _vote.Result;
            if (value is T tValue)
                return tValue;
            return default;
        }

        public override void SetValueFromVar(AbstractMonoVariable source, Object byWho)
        {
            //用的到嗎？根本用不到？
        }

        protected void SetValueInternal(bool value, Object byWho = null)
        {
            _vote.Vote(byWho, value);
        }

        public override string StringValue => _vote.Result.ToString();
        public override bool IsValueExist => true;
        protected override bool HasValueSource => false;

        public override void ResetStateRestore()
        {
            _vote.ClearValue();
        }

        [PreviewInInspector]
        public bool Result => _vote.Result;

        // public void Vote(bool vote, MonoBehaviour m)
        // {
        //     _vote.Vote(m, vote);
        // }
        public string ValueInfo => Result.ToString();
        public bool IsDrawingValueInfo => true;
    }
}
