using System.Collections.Generic;
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

        /// <summary>關掉 Domain Reload 時 static 會跨 PlayMode 殘留，這裡強制歸零。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            _zones.Clear();
        }
    }
}
