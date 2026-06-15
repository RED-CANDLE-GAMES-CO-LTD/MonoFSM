using MonoFSM.Core.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Animation
{
    public enum AnimatorPoseMode
    {
        /// <summary>Animator 正常播放（視覺），位移由 RootMotionClipMoveAction / AnimatorRootMotionRelay 負責</summary>
        AnimatorDriven,

        /// <summary>Animator 關閉，pose 由 AnimationClipPhysicsSampler / AnimationClipPlayAction 手動取樣</summary>
        ScriptSampled,
    }

    /// <summary>
    /// 掛在 rig root（Animator 同節點），統一管理 animator.enabled / applyRootMotion。
    /// 取代各 Action / Sampler 自己開關 Animator 的散落邏輯——
    /// Animator 的開關是 rig 的靜態使用模式，不是 per-state 的事。
    /// </summary>
    public class AnimatorControlModeHandle : MonoBehaviour, ISceneAwake
    {
        [Auto] private Animator _animator;

        [Tooltip("AnimatorDriven：Animator 照常播視覺、不套 root motion（位移交給邏輯層）。ScriptSampled：關閉 Animator，由 sampler 手動取樣")]
        [SerializeField]
        private AnimatorPoseMode _mode = AnimatorPoseMode.AnimatorDriven;

        [ShowIf(nameof(_mode), AnimatorPoseMode.AnimatorDriven)]
        [Tooltip("是否讓 Animator 套用 root motion（走 OnAnimatorMove / AnimatorRootMotionRelay 的 render-time 路徑）。用 RootMotionClipMoveAction 的 tick-based 位移時保持關閉，避免雙重位移")]
        [SerializeField]
        private bool _applyRootMotion;

        public AnimatorPoseMode Mode => _mode;

        public void EnterSceneAwake() => Apply();

        [Button("套用模式")]
        public void Apply()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError("[AnimatorControlModeHandle] 同節點上沒有 Animator", this);
                return;
            }

            _animator.enabled = _mode == AnimatorPoseMode.AnimatorDriven;
            _animator.applyRootMotion = _mode == AnimatorPoseMode.AnimatorDriven && _applyRootMotion;
        }
    }
}
