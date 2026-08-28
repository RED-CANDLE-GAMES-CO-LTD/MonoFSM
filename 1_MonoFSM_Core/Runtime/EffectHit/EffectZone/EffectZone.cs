using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.EffectZone
{
    /// <summary>判定「誰算在這個 zone 裡」的方式。</summary>
    public enum ZoneCoverage
    {
        [Tooltip("以自己為圓心、半徑內都算（ex: 供電廟的供電區）")]
        Radius = 0,

        [Tooltip("Hierarchy 底下的子孫都算，不看距離（ex: 掛在車廂 root，車上的東西都有電）")]
        Hierarchy = 1,

        [Tooltip("兩者任一成立都算")]
        Both = 2,
    }

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

        [Tooltip("判定方式：距離、Hierarchy 從屬、或兩者任一")]
        [SerializeField]
        private ZoneCoverage _coverage = ZoneCoverage.Radius;

        [Tooltip("生效半徑（公尺）")]
        [HideIf("@_coverage == ZoneCoverage.Hierarchy")]
        [SerializeField]
        private float _radius = 90f;

        [Tooltip(
            "半徑乘上自己 transform 的最大軸 lossyScale，對齊 SphereCollider 的物理行為。"
                + "開了之後 _radius 填 local 半徑（ex: 0.5），實例縮放 prefab 時範圍會跟著變"
        )]
        [HideIf("@_coverage == ZoneCoverage.Hierarchy")]
        [SerializeField]
        private bool _scaleRadiusWithTransform;

        [Tooltip("圓心，留空則用自己的 transform")]
        [HideIf("@_coverage == ZoneCoverage.Hierarchy")]
        [SerializeField]
        private Transform _centerOverride;

        [Tooltip("這個區域現在有沒有在運作，留空 = 永遠運作（ex: 指向廟的 d_HasPower 有電）")]
        [DropDownRef]
        [SerializeField]
        private VarBool _isActiveVar;

        [Tooltip("這個區域提供的數值（ex: 供電量、輻射強度），留空則用下面的固定值")]
        [DropDownRef]
        [SerializeField]
        private VarFloat _valueVar;

        [Tooltip("沒指定 _valueVar 時用的固定數值")]
        [HideIf("@_valueVar != null")]
        [SerializeField]
        private float _constantValue = 1f;

        [Tooltip("這個 zone 屬於誰（編輯時往上抓，給 EffectZoneEntitySource 反查來源）")]
        [AutoParent]
        [SerializeField]
        private MonoEntity _ownerEntity;

        public GeneralEffectType ZoneType => _zoneType;

        /// <summary>提供這個 zone 的 entity（ex: 供電廟本體），編輯時 cache，runtime 零 GetComponent。</summary>
        public MonoEntity OwnerEntity => _ownerEntity;

        /// <summary>這個 zone 提供的數值；有接 VarFloat 就讀它，否則用序列化的固定值。</summary>
        public float ZoneValue => _valueVar != null ? _valueVar.Value : _constantValue;

        public Vector3 Center =>
            _centerOverride != null ? _centerOverride.position : transform.position;

        /// <summary>世界半徑；有開 scale 跟隨就乘最大軸 lossyScale（和 SphereCollider 同規則）。</summary>
        public float Radius
        {
            get
            {
                if (!_scaleRadiusWithTransform)
                    return _radius;
                var s = transform.lossyScale;
                var maxScale = Mathf.Max(
                    Mathf.Abs(s.x),
                    Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z))
                );
                return _radius * maxScale;
            }
        }

        public float RadiusSqr
        {
            get
            {
                var r = Radius;
                return r * r;
            }
        }

        /// <summary>沒設 _isActiveVar 就永遠有效；有設就跟著那顆 VarBool。</summary>
        [ShowInInspector]
        public bool IsZoneActive =>
            _isActiveVar == null
            || (_isActiveVar.isActiveAndEnabled && _isActiveVar.CurrentValue);

        /// <summary>這個 zone 有沒有開啟距離判定（Hierarchy-only 的不會進 registry，這裡是保險）。</summary>
        public bool HasRadiusCoverage => _coverage != ZoneCoverage.Hierarchy;

        /// <summary>這個 zone 有沒有開啟 Hierarchy 從屬判定。</summary>
        public bool HasHierarchyCoverage => _coverage != ZoneCoverage.Radius;

        public bool Covers(Vector3 pos)
        {
            return HasRadiusCoverage && IsZoneActive && (pos - Center).sqrMagnitude <= RadiusSqr;
        }

        /// <summary>
        /// 給 IsParentEntityHasEffectZoneCondition 用：這顆 zone 現在能不能罩住自己的子孫。
        /// registry 版本靠 OnEnable/OnDisable 天然只留 enabled 的，Hierarchy 版本是往上直接抓 component，
        /// 所以要自己檢查 isActiveAndEnabled（中間某層被關掉時就不該算）。
        /// </summary>
        public bool CoversHierarchy => HasHierarchyCoverage && isActiveAndEnabled && IsZoneActive;

        private void OnEnable()
        {
            //Hierarchy-only 的不需要被掃距離，別佔 registry
            if (HasRadiusCoverage)
                EffectZoneRegistry.Register(this);
        }

        private void OnDisable()
        {
            EffectZoneRegistry.Unregister(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!HasRadiusCoverage)
                return;
            Gizmos.color = IsZoneActive ? new Color(1f, 0.9f, 0.2f, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.35f);
            Gizmos.DrawWireSphere(Center, Radius);
        }
#endif
    }
}
