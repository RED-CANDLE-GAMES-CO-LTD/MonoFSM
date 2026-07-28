using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.EffectZone
{
    /// <summary>
    /// 「以自己為圓心、半徑內都算受影響」的範圍效果區來源（ex: 供電廟的供電區）。
    /// 掛在提供效果的那顆物件上，enable 時自動登錄，不需要被影響的一方持有任何 reference。
    ///
    /// 和 EffectDetector + Dealer 的差別：這裡不發 enter/exit 事件、不留任何狀態，
    /// 也不需要 trigger collider（省掉大半徑 SphereCollider 的物理成本）。
    /// 要接事件的（進入區域時播特效）留給 EffectHit；只要問「現在算不算在範圍內」的用這個。
    /// </summary>
    [DisallowMultipleComponent]
    public class EffectZone : MonoBehaviour
    {
        [Required]
        [SOConfig("GeneralEffectType")]
        [Tooltip("這個區域提供什麼效果，被影響的一方用同一顆 asset 對應（ex: d_PowerZone 供電區）")]
        [SerializeField]
        private GeneralEffectType _zoneType;

        [Tooltip("生效半徑（公尺）")]
        [SerializeField]
        private float _radius = 90f;

        [Tooltip("圓心，留空則用自己的 transform")]
        [SerializeField]
        private Transform _centerOverride;

        [Tooltip("這個區域現在有沒有在運作，留空 = 永遠運作（ex: 指向廟的 d_HasPower 有電）")]
        [DropDownRef]
        [SerializeField]
        private VarBool _isActiveVar;

        public GeneralEffectType ZoneType => _zoneType;

        public Vector3 Center =>
            _centerOverride != null ? _centerOverride.position : transform.position;

        public float RadiusSqr => _radius * _radius;

        /// <summary>沒設 _isActiveVar 就永遠有效；有設就跟著那顆 VarBool。</summary>
        [ShowInInspector]
        public bool IsZoneActive =>
            _isActiveVar == null
            || (_isActiveVar.isActiveAndEnabled && _isActiveVar.CurrentValue);

        public bool Covers(Vector3 pos)
        {
            return IsZoneActive && (pos - Center).sqrMagnitude <= RadiusSqr;
        }

        private void OnEnable()
        {
            EffectZoneRegistry.Register(this);
        }

        private void OnDisable()
        {
            EffectZoneRegistry.Unregister(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsZoneActive ? new Color(1f, 0.9f, 0.2f, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.35f);
            Gizmos.DrawWireSphere(Center, _radius);
        }
#endif
    }
}
