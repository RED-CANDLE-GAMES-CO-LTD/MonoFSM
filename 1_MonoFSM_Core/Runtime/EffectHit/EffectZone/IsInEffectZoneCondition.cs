using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Interact.EffectHit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.EffectZone
{
    /// <summary>
    /// 「我現在在某個運作中的 EffectZone 範圍內嗎」——每次求值即時算距離，不留狀態。
    /// 取代「Receiver enter 時把來源 entity 快照到 local VarEntity，再讀它身上的 var」那條路徑：
    /// 那條在 level reset 清掉 VarEntity 後要靠 enter 重放才能修復，這條沒有這個問題。
    /// </summary>
    public class IsInEffectZoneCondition : AbstractConditionBehaviour
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

        //命中的那個 zone，除錯用（Inspector 看得到現在是被誰供電）
        [ShowInInspector]
        private EffectZone _lastCoveringZone;

        protected override bool IsValid
        {
            get
            {
                if (_zoneType == null)
                    return false;

                var pos = Position;
                var zones = EffectZoneRegistry.Zones;
                //zone 數量是「幾座廟」的量級，直接線性掃就好，不需要像 ProximitySpawnDirector 那樣建 grid
                for (var i = 0; i < zones.Count; i++)
                {
                    var zone = zones[i];
                    if (zone == null || zone.ZoneType != _zoneType)
                        continue;
                    if (!zone.Covers(pos))
                        continue;

                    _lastCoveringZone = zone;
                    return true;
                }

                _lastCoveringZone = null;
                return false;
            }
        }

        public override string Description =>
            $"In [{(_zoneType != null ? _zoneType.name : "?")}] Zone";
    }
}
