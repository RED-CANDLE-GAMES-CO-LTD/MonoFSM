using System;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Animation
{
    /// <summary>
    /// 掛在動畫 rig root（clip 曲線路徑的基準節點）。
    /// Simulate（tick）階段以當前 active State 的 AnimationClipPlayAction 取樣 clip，
    /// 並把 pose 推給子層 kinematic Rigidbody，讓動畫驅動的物件能正確物理推擠
    /// （同 Fusion AnimatedPlatform sample 的 kinematic 模式）。
    /// 上層需有 MonoObj / Simulator 才會收到 Simulate 回調。
    /// </summary>
    public class AnimationClipPhysicsSampler : MonoBehaviour, IUpdateSimulate, ISceneAwake
    {
        [Auto] Animator _animator;

        private void Awake()
        {
            if (_animator != null) _animator.enabled = false;
        }

        [Tooltip("要跟隨動畫 pose 的 Rigidbody（自動收集子層），會被設為 kinematic + FreezeAll")] [AutoChildren]
        private Rigidbody[] _rigidbodies;

        [ShowInInspector] private readonly List<AnimationClipPlayAction> _actions = new();

        // Debug 觀察用
        [ShowInInspector] private AnimationClipPlayAction _activeAction;

        [ShowInInspector] private float _lastSampleTime;

        public void Register(AnimationClipPlayAction action)
        {
            if (!_actions.Contains(action))
                _actions.Add(action);
        }

        public void EnterSceneAwake()
        {
            if (_rigidbodies == null)
                return;
            foreach (var rb in _rigidbodies)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        public void Simulate(float deltaTime)
        {
            _activeAction = null;
            for (var i = 0; i < _actions.Count; i++)
            {
                var action = _actions[i];
                if (action != null && action.isActiveAndEnabled && action.IsActiveState &&
                    action.Clip != null)
                {
                    _activeAction = action;
                    break;
                }
            }

            if (_activeAction == null)
                return;

            _lastSampleTime = _activeAction.LogicSampleTime;
            _activeAction.Clip.SampleAnimation(gameObject, _lastSampleTime);
            SyncRigidbodies();
        }

        private void SyncRigidbodies()
        {
            if (_rigidbodies == null)
                return;
            for (var i = 0; i < _rigidbodies.Length; i++)
            {
                var rb = _rigidbodies[i];
                rb.position = rb.transform.position;
                rb.rotation = rb.transform.rotation;
            }
        }
    }
}
