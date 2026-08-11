using MonoFSM.Animation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    public class AnimationDoneCondition : AbstractConditionBehaviour
    {
        [Range(0, 1)] public float _exitRatio = 0;

        public override string Description
        {
            get
            {
                var desc = _exitRatio <= 0 ? "Anim Done" : "Anim Exit at: " + _exitRatio;
#if UNITY_EDITOR
                if (Application.isPlaying && _action != null)
                    desc += " (" + _action.ProgressDebugInfo + ")";
#endif
                return desc;
            }
        }

        [Button]
        void SetExitRatio()
        {
            _exitRatio = 0.75f;
        }

        protected override bool IsValid => _action != null &&
                                           (_exitRatio <= 0
                                               ? _action.IsDone
                                               : _action.IsProgressPassedRatio(_exitRatio));

        //沒有serialized, 所以editor check會誤判..
        [SerializeField]
        [Required]
        [CompRef]
        [AutoParent]
        private AnimatorPlayAction _action; //不用選的喔
        //FIXME: autosibiling? 啊好像不行如果有兩個AnimatorPlayAction
    }
}
