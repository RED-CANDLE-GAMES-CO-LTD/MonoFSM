using System;
using Sirenix.OdinInspector;

namespace RCGMaker.Runtime.Vote
{
    //Default: Vote
    public class VoteAction : AbstractStateAction, ILevelResetPrepare
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

        protected override void OnStateEnterImplement()
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

        bool _isPrepared = false;

        public void LevelResetPrepareRuntimeData()
        {
            _isPrepared = true;
        }
    }
}