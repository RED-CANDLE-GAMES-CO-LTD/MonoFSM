using MonoFSM.FSM;
using MonoFSM.Core;
using MonoFSM.EditorExtension;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    /// <summary>
    /// 引用另一個既有 TransitionBehaviour 的轉移代理：條件與 target State 都取自被引用的那個 transition。
    /// 當同一組轉移條件要在多個 State 重用時，掛這個在 State 底下指過去，不必複製一整棵 condition 子樹。
    /// </summary>
    public class TransitionRef : TransitionBehaviour<MonoStateBehaviour>, IOverrideHierarchyIcon
    {
        protected override string DescriptionTag => "TransitionRef";

        public override string Description =>
            _sourceTransition != null && _sourceTransition._target != null
                ? "=>" + _sourceTransition._target.Name?.Replace("[State]", "")
                : "";

        //共用來源：condition 群與 target State 都從它身上取
        [OnValueChanged(nameof(Rename))]
        [Required]
        [DropDownRef]
        [SerializeField]
        private TransitionBehaviour _sourceTransition;

        protected override void Awake()
        {
            if (_sourceTransition == null)
            {
                Debug.LogError("TransitionRef 沒有指定 _sourceTransition，此轉移不會生效", this);
                return;
            }

            if (_sourceTransition._target == null)
            {
                Debug.LogError(
                    "TransitionRef 的來源 transition 沒有 target State，此轉移不會生效",
                    _sourceTransition
                );
                return;
            }

            _transitionData = new TransitionData<MonoStateBehaviour>(
                _sourceTransition._target,
                (from, to) =>
                {
                    if (isActiveAndEnabled == false)
                        return false;

                    //只借條件，不看來源 transition 自己的啟用狀態
                    return _sourceTransition.AreConditionsValid();
                }
            );
        }

#if UNITY_EDITOR
        public string IconName => "CollabMoved Icon";
        public bool IsDrawingIcon => true;
        public Texture2D CustomIcon => null;
#endif
    }
}
