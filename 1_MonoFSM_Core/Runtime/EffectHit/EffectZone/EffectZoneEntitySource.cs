using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.EffectZone
{
    /// <summary>
    /// 「現在罩住我的那個 EffectZone 是誰提供的」——回傳該 zone 的 OwnerEntity（ex: 正在供電給我的那座廟）。
    /// 預設取第一顆命中的（唯一語意，不做多 zone 取捨）；沒被罩住就回 null。
    /// 開 `_pickNearest` 則改取圓心最近的那個 zone 的 owner（探測器指向最近的訊號源）。
    /// 純 pull 無狀態，每次求值現算，level reset 免疫。
    /// </summary>
    public class EffectZoneEntitySource : AbstractEntitySource
    {
        [Required]
        [SOConfig("GeneralEffectType")]
        [Tooltip("要找哪一種區域（ex: d_PowerZone 供電區）")]
        [SerializeField]
        private GeneralEffectType _zoneType;

        [Tooltip("判定位置，留空則用自己的 transform")]
        [SerializeField]
        private Transform _positionOverride;

        [Tooltip("多個 zone 同時罩住時取圓心最近的那個，給探測器指向最近的訊號源用；關掉 = 取第一個命中")]
        [SerializeField]
        private bool _pickNearest;

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        [Tooltip("最後一次求值時，同時罩住自己的同型 zone 有幾個")]
        private int _debugCoveringZoneCount;

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        [Tooltip("最後一次求值取到的 zone 圓心距離，沒命中是 -1")]
        private float _debugPickedZoneDistance = -1f;

        private Vector3 Position =>
            _positionOverride != null ? _positionOverride.position : transform.position;

        public override MonoEntity monoEntity
        {
            get
            {
                if (_zoneType == null)
                {
                    _debugCoveringZoneCount = 0;
                    _debugPickedZoneDistance = -1f;
                    return null;
                }

                var zone = EffectZoneRegistry.FindCovering(
                    _zoneType,
                    Position,
                    _pickNearest,
                    out _debugCoveringZoneCount,
                    out var sqrDist
                );
                _debugPickedZoneDistance = zone != null ? Mathf.Sqrt(sqrDist) : -1f;
                return zone != null ? zone.OwnerEntity : null;
            }
        }

        public override string SuggestDeclarationName =>
            _zoneType != null ? _zoneType.name + "Owner" : "";

        public override string Description =>
            $"[{(_zoneType != null ? _zoneType.name : "?")}] Zone Owner{(_pickNearest ? "（最近）" : "")}";
    }
}
