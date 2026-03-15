using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Variable;
using MonoFSM.Runtime.Interact.EffectHit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.EffectHit.Action
{
    /// <summary>
    ///     想要把打到的目標放進/移出一個列表裡面
    /// </summary>
    public class EffectHitListOperationAction
        : AbstractStateAction,
            IArgEventReceiver<GeneralEffectHitData>
    {
        [Required]
        [DropDownRef]
        public VarListEntity _targetList; // 目標列表

        // [CompRef] [AutoParent] private HitDataEntityProvider _hitDataEntityProvider;

        public enum EffectHitState
        {
            Enter,
            Exit,
        }

        public EffectHitState _effectHitState = EffectHitState.Enter;

        protected override void OnActionExecuteImplement()
        {
            //從這裡是不是不可靠...
            // var monoEntity = _hitDataEntityProvider.monoEntity;
            // if (_targetList == null)
            //     Debug.LogError("EffectHitListOperationAction: Target list is null", this);
            // switch (_effectHitState)
            // {
            //     // 在列表中添加當前的MonoEntity
            //     case EffectHitState.Enter:
            //         _targetList.Add(monoEntity);
            //         break;
            //     // 從列表中移除當前的MonoEntity
            //     case EffectHitState.Exit:
            //         _targetList.Remove(monoEntity);
            //         break;
            // }
        }

        public void ArgEventReceived(GeneralEffectHitData arg)
        {
            //把thisFrameCollider.parentEntity拿出來就好了?
            var monoEntity = arg.GeneralReceiver.BindEntity;
            if (_targetList == null)
                Debug.LogError("EffectHitListOperationAction: Target list is null", this);
            switch (_effectHitState)
            {
                // 在列表中添加當前的MonoEntity
                case EffectHitState.Enter:
                    _targetList.Add(monoEntity);
                    break;
                // 從列表中移除當前的MonoEntity
                case EffectHitState.Exit:
                    _targetList.Remove(monoEntity);
                    break;
            }
        }
    }
}
