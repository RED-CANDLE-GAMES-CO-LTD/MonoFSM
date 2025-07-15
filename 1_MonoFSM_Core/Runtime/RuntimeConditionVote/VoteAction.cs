using System;
using MonoFSM.Core.Runtime.Action;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;

namespace MonoFSM.Runtime.Vote
{
    //Default: Vote
    public class VoteAction : AbstractStateAction, IResetStateRestore
    {
        public enum VoteType
        {
            Vote,
            Revoke,
            EnableDisable
        }

        public VoteType voteType = VoteType.EnableDisable;

        [ShowIf(nameof(voteType), VoteType.Vote)]
        public bool voteValue = true;

        [DropDownRef] public MonoVariableVote _voteVar;

        protected override string renamePostfix => $"{voteType} {_voteVar.name} {voteValue}";

        protected override void OnActionExecuteImplement()
        {
            if (voteType == VoteType.Vote)
                _voteVar.Vote.Vote(this, voteValue);
            else if (voteType == VoteType.Revoke)
                _voteVar.Vote.Revoke(this);
        }

        private void OnEnable()
        {
            if (_isPrepared == false)
                return;
            if (voteType == VoteType.EnableDisable)
                _voteVar.Vote.Vote(this, voteValue);
        }

        private void OnDisable()
        {
            if (_isPrepared == false)
                return;
            if (voteType == VoteType.EnableDisable)
                _voteVar.Vote.Revoke(this);
        }

        private bool _isPrepared = false;

        public void ResetStateRestore()
        {
            _isPrepared = true;
        }
    }
}