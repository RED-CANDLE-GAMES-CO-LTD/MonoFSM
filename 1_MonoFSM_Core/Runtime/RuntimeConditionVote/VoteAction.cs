using MonoFSM.Core.Runtime.Action;
using Sirenix.OdinInspector;

namespace MonoFSM.Runtime.Vote
{
    //Default: Vote
    public class VoteAction : AbstractStateAction
    {
        public enum VoteType
        {
            Vote,
            Revoke,
            EnableDisable,
        }

        public VoteType voteType = VoteType.Vote;

        [ShowIf(nameof(voteType), VoteType.Vote)]
        public bool voteValue = true;

        [DropDownRef]
        public VarVote _voteVar;

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
            //FIXME: 不是很喜歡，需要更能信任的 OnEnable/OnDisable
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

        public override void ResetStateRestore(bool isHardReset)
        {
            _isPrepared = true;
            base.ResetStateRestore(false);
        }
    }
}
