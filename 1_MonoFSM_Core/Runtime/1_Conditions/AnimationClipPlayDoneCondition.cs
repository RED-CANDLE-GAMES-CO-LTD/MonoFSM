using MonoFSM.Animation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// 對應 IClipPlayProgress（AnimationClipPlayAction / RootMotionClipMoveAction）的播放完成條件
    /// （同 AnimationDoneCondition 之於 AnimatorPlayAction）。
    /// _exitRatio &gt; 0 時改為「播放進度超過比例」即成立。
    /// </summary>
    public class AnimationClipPlayDoneCondition : AbstractConditionBehaviour
    {
        [Range(0, 1)] public float _exitRatio = 0;

        public override string Description =>
            _exitRatio <= 0 ? "Clip Done" : "Clip Exit at: " + _exitRatio;

        [Button]
        void SetExitRatio()
        {
            _exitRatio = 0.75f;
        }

        protected override bool IsValid => _action != null &&
                                           (_exitRatio <= 0
                                               ? _action.IsDone
                                               : _action.IsProgressPassedRatio(_exitRatio));

        [Required]
        [CompRef]
        [AutoParent]
        private IClipPlayProgress _action;
    }
}
