using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using Fusion;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Animation
{
    /// <summary>
    /// Animator 路線的頭部朝向覆蓋（過渡用）。累積/平滑已經在 HeadAimAction（tick, resim-safe）做完，
    /// 這裡純套用，不重算 RotateTowards。透過 OnAnimatorIK 在 Animator evaluate 之後套用，
    /// 需在 Animator Controller 對應 Layer 打開 IK Pass。
    /// 之後敵人全面 migrate 到 AnimationClipPlayAction 後可整個刪除，改用 HeadLookAtClipModifier。
    /// </summary>
    public class HeadLookAtAnimatorApplier : AbstractRenderBehaviour
    {
        [Auto] private Animator _animator;

        [Required] [Tooltip("要覆蓋朝向的頭骨")] [SerializeField]
        private Transform _headBone;

        [Required] [SerializeField] private VarVector3 _aimForward;

        // private void OnAnimatorIK(int layerIndex)
        // {
        //     if (_headBone == null || _aimForward == null || !_aimForward.IsValueExist)
        //         return;
        //
        //     var forward = _aimForward.Value;
        //     if (forward.sqrMagnitude < 0.0001f)
        //         return;
        //
        //     _headBone.rotation = Quaternion.LookRotation(forward, Vector3.up);
        // }

        // protected override void OnActionExecuteImplement()
        // {
        //
        // }

        public override void OnEnterRenderImplement()
        {
        }

        public override void OnRenderImplement()
        {
            if (_headBone == null || _aimForward == null || !_aimForward.IsValueExist)
            {
                Debug.LogError("HeadLookAtAnimatorApplier: _headBone / _aimForward 未設定或無值", this);
                return;
            }


            var forward = _aimForward.Value;
            if (forward.sqrMagnitude < 0.0001f)
                return;

            _headBone.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }
}
