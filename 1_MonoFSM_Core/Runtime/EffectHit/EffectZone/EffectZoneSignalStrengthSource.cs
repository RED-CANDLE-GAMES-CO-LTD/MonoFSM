using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoValueProvider;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.EffectZone
{
    /// <summary>
    /// 「我離最近的某型 EffectZone 訊號源有多近」的 float Getter（薩爾達神廟探測器的訊號強度）。
    /// 預設回 0～1：站在 zone 圓心是 1、走到半徑邊緣是 0、沒被任何同型 zone 罩住是 0，
    /// 可直接餵 beep 間隔、燈光強度、UI 條。也可以切成回公尺距離，或乘上 zone 自己的 ZoneValue。
    /// 通常和 EffectZoneEntitySource（同一顆 zoneType、同樣開 _pickNearest）成對掛在探測器的 VarFolder 裡。
    /// 純 pull 無狀態，每次求值現算。
    /// </summary>
    public class EffectZoneSignalStrengthSource : AbstractValueSource<float>, IFloatProvider
    {
        public enum StrengthMode
        {
            /// <summary>1 - 距離/半徑，夾在 0～1；沒被罩住 = 0</summary>
            Normalized01,

            /// <summary>到最近 zone 圓心的公尺距離；沒被罩住 = _invalidValue</summary>
            DistanceMeters,

            /// <summary>Normalized01 再乘上該 zone 的 ZoneValue（zone 上的 _valueVar / _constantValue）</summary>
            ZoneValueTimesNormalized,
        }

        [Required]
        [SOConfig("GeneralEffectType")]
        [Tooltip("要量哪一種區域的訊號（ex: d_PigeonSignal 鴿子訊號）")]
        [SerializeField]
        private GeneralEffectType _zoneType;

        [Tooltip("判定位置，留空則用自己的 transform")]
        [SerializeField]
        private Transform _positionOverride;

        [Tooltip("多個 zone 同時罩住時取圓心最近的那個；關掉 = 取第一個命中（和成對的 EffectZoneEntitySource 設一樣）")]
        [SerializeField]
        private bool _pickNearest = true;

        [SerializeField]
        private StrengthMode _mode = StrengthMode.Normalized01;

        [ShowIf("_mode", StrengthMode.DistanceMeters)]
        [Tooltip("DistanceMeters 模式下沒被罩住時的回傳值，預設極大值避免距離條件誤觸發")]
        [SerializeField]
        private float _invalidValue = float.MaxValue;

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

        public override float Value
        {
            get
            {
                var zone = EffectZoneRegistry.FindCovering(
                    _zoneType,
                    Position,
                    _pickNearest,
                    out _debugCoveringZoneCount,
                    out var sqrDist
                );
                if (zone == null)
                {
                    _debugPickedZoneDistance = -1f;
                    return _mode == StrengthMode.DistanceMeters ? _invalidValue : 0f;
                }

                var dist = Mathf.Sqrt(sqrDist);
                _debugPickedZoneDistance = dist;
                if (_mode == StrengthMode.DistanceMeters)
                    return dist;

                var radius = zone.Radius;
                //Hierarchy-only 的 zone 沒有半徑概念，被罩住就當滿格
                var normalized = radius > 0f ? Mathf.Clamp01(1f - dist / radius) : 1f;
                return _mode == StrengthMode.ZoneValueTimesNormalized
                    ? normalized * zone.ZoneValue
                    : normalized;
            }
        }

        public override string Description =>
            $"[{(_zoneType != null ? _zoneType.name : "?")}] 訊號強度 {_mode}{(_pickNearest ? "（最近）" : "")}";
    }
}
