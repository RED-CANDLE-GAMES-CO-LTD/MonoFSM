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
    /// 取第一顆命中的（唯一語意，不做多 zone 取捨）；沒被罩住就回 null。
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

        private Vector3 Position =>
            _positionOverride != null ? _positionOverride.position : transform.position;

        public override MonoEntity monoEntity
        {
            get
            {
                if (_zoneType == null)
                    return null;

                var pos = Position;
                var zones = EffectZoneRegistry.Zones;
                //zone 數量是「幾座廟」的量級，直接線性掃就好
                for (var i = 0; i < zones.Count; i++)
                {
                    var zone = zones[i];
                    if (zone == null || zone.ZoneType != _zoneType)
                        continue;
                    if (!zone.Covers(pos))
                        continue;

                    return zone.OwnerEntity;
                }

                return null;
            }
        }

        public override string SuggestDeclarationName =>
            _zoneType != null ? _zoneType.name + "Owner" : "";

        public override string Description =>
            $"[{(_zoneType != null ? _zoneType.name : "?")}] Zone Owner";
    }
}
