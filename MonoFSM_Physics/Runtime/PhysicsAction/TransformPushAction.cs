using MonoFSM.Core;
using MonoFSM.Core.Detection;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM_Physics.Runtime.PhysicsAction
{
    /// <summary>
    /// 沿指定 Local 軸推動 Transform，限制範圍與速度。
    /// 掛在 EffectReceiver 的 EnterNode / ExitNode 下，
    /// Enter 開始推、Exit 停止推。
    /// </summary>
    public class TransformPushAction : AbstractStateLifeCycleHandler,
        IArgEventReceiver<GeneralEffectHitData>
    {
        [Header("Push Target")]
        [Tooltip("被推動的 Transform，不填則使用自身")]
        [SerializeField]
        private Transform _pushTarget;

        [Header("Axis & Range")]
        [Tooltip("推動的 Local 軸向")]
        [SerializeField]
        private LerpAxis _pushAxis = LerpAxis.Z;

        [Tooltip("該軸上的最小位移值（相對於初始位置）")]
        [SerializeField]
        private float _minValue = 0f;

        [Tooltip("該軸上的最大位移值（相對於初始位置）")]
        [SerializeField]
        private float _maxValue = 3f;

        [Header("Speed")]
        [Tooltip("每秒推動距離")]
        public VarFloatWrapper _pushSpeed = new VarFloatWrapper(1f);

        // --- Runtime ---
        private Transform _currentDealer;
        private Vector3 _originLocalPos;
        private bool _isPushing;

        [ShowInInspector, ReadOnly]
        private float _currentOffset;

        private Transform PushTarget => _pushTarget != null ? _pushTarget : transform;

        protected override void OnStateEnter()
        {
            base.OnStateEnter();
            _originLocalPos = PushTarget.localPosition;
            _isPushing = true;
            Debug.Log($"[TransformPush] Start pushing, origin: {_originLocalPos}", this);
        }

        protected override void OnStateUpdate()
        {
            base.OnStateUpdate();
            if (!_isPushing || _currentDealer == null)
                return;

            // 計算 dealer 在推動軸上的推力方向（local space）
            var dealerDir = _currentDealer.position - PushTarget.position;
            var localDir = PushTarget.parent != null
                ? PushTarget.parent.InverseTransformDirection(dealerDir)
                : dealerDir;

            // 取得推動軸向的分量符號，決定推動正負方向
            float pushSign = GetAxisSign(localDir);
            if (Mathf.Approximately(pushSign, 0f))
                return;

            float delta = pushSign * _pushSpeed.Value * DeltaTime;
            _currentOffset += delta;
            _currentOffset = Mathf.Clamp(_currentOffset, _minValue, _maxValue);

            PushTarget.localPosition = _originLocalPos + GetAxisVector(_currentOffset);
        }

        protected override void OnStateExit()
        {
            base.OnStateExit();
            _isPushing = false;
            _currentDealer = null;
            Debug.Log($"[TransformPush] Stop pushing, offset: {_currentOffset}", this);
        }

        public new void ArgEventReceived(GeneralEffectHitData arg)
        {
            if (arg?.GeneralDealer != null)
                _currentDealer = arg.GeneralDealer.transform;

            base.ArgEventReceived(arg);
        }

        /// <summary>
        /// 取得 localDir 在指定軸上的符號 (+1 / -1 / 0)
        /// </summary>
        private float GetAxisSign(Vector3 localDir)
        {
            float value = 0f;
            if ((_pushAxis & LerpAxis.X) != 0) value += localDir.x;
            if ((_pushAxis & LerpAxis.Y) != 0) value += localDir.y;
            if ((_pushAxis & LerpAxis.Z) != 0) value += localDir.z;
            return Mathf.Sign(value);
        }

        /// <summary>
        /// 將 offset 值映射到指定軸向的 Vector3
        /// </summary>
        private Vector3 GetAxisVector(float offset)
        {
            var v = Vector3.zero;
            if ((_pushAxis & LerpAxis.X) != 0) v.x = offset;
            if ((_pushAxis & LerpAxis.Y) != 0) v.y = offset;
            if ((_pushAxis & LerpAxis.Z) != 0) v.z = offset;
            return v;
        }
    }
}
