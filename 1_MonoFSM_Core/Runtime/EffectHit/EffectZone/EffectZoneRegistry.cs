using System.Collections.Generic;
using MonoFSM.Runtime.Interact.EffectHit;
using UnityEngine;

namespace Gameplay.EffectZone
{
    /// <summary>
    /// 範圍效果區的登錄處（參考 SpawnObserverRegistry 的作法）。
    /// EffectZone 在 OnEnable 註冊、OnDisable 反註冊，IsInEffectZoneCondition 每次求值時掃這份清單。
    ///
    /// 之所以用 registry + 距離判定而不是 EffectHit 的 detector：
    /// detector 那條要靠 enter 事件把來源 entity 快照進 local VarEntity，
    /// 是「edge 當 level 用」——level reset 會清掉那顆 runtimeOnly 的 VarEntity，
    /// 少放一次 enter 狀態就永久錯掉。這裡的判定是純 pull，沒有任何 latch，reset 免疫。
    /// </summary>
    public static class EffectZoneRegistry
    {
        private static readonly List<EffectZone> _zones = new();

        public static IReadOnlyList<EffectZone> Zones => _zones;

        public static void Register(EffectZone zone)
        {
            if (zone == null) return;
            if (_zones.Contains(zone)) return;
            _zones.Add(zone);
        }

        public static void Unregister(EffectZone zone)
        {
            if (zone == null) return;
            _zones.Remove(zone);
        }

        /// <summary>
        /// 找「現在罩住 pos 的同型 zone」：pickNearest = false 取第一顆命中（唯一語意），
        /// true 取圓心最近的那顆。沒命中回 null。coveringCount / sqrDist 給除錯欄位與訊號強度用，零 GC。
        /// EffectZoneEntitySource 與 EffectZoneSignalStrengthSource 共用這一份，避免兩邊掃法漂移。
        /// </summary>
        public static EffectZone FindCovering(
            GeneralEffectType zoneType,
            Vector3 pos,
            bool pickNearest,
            out int coveringCount,
            out float sqrDist
        )
        {
            coveringCount = 0;
            sqrDist = float.MaxValue;
            if (zoneType == null)
                return null;

            EffectZone picked = null;
            //zone 數量是「幾座廟」的量級，直接線性掃就好
            for (var i = 0; i < _zones.Count; i++)
            {
                var zone = _zones[i];
                if (zone == null || zone.ZoneType != zoneType)
                    continue;
                if (!zone.Covers(pos))
                    continue;

                coveringCount++;
                //只比大小，開根號沒意義
                var d = (zone.Center - pos).sqrMagnitude;
                if (!pickNearest)
                {
                    sqrDist = d;
                    return zone;
                }

                if (d >= sqrDist)
                    continue;
                sqrDist = d;
                picked = zone;
            }

            return picked;
        }

        /// <summary>關掉 Domain Reload 時 static 會跨 PlayMode 殘留，這裡強制歸零。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            _zones.Clear();
        }
    }
}
