using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Utilities
{
    /// <summary>
    /// 聯網 deterministic 的隨機工具：固定 seed 搭配 tick（通常是 WorldUpdateSimulator.CurrentTick）
    /// 做整數 hash 產生隨機值。同一組 (seed, tick) 在所有 client 得到相同結果，
    /// 不用 UnityEngine.Random（各 client 狀態不同步、會 desync）。
    /// </summary>
    public static class TickRandom
    {
        /// <summary>回傳 [0,1) 的 float。</summary>
        public static float Value01(int seed, int tick) => Hash01((uint)seed, (uint)tick);

        /// <summary>回傳 [min, max] 的 float。</summary>
        public static float Range(int seed, int tick, float min, float max) =>
            Mathf.Lerp(min, max, Value01(seed, tick));

        /// <summary>
        /// 回傳 [minInclusive, maxExclusive) 的 int。maxExclusive &lt;= minInclusive 時回傳 minInclusive。
        /// Value01 恆 &lt; 1，所以結果不會等於 maxExclusive。
        /// </summary>
        public static int RangeInt(int seed, int tick, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                return minInclusive;
            var span = maxExclusive - minInclusive;
            return minInclusive + (int)(Value01(seed, tick) * span);
        }

        /// <summary>
        /// 把兩個 int 混成一個 seed（例如 base salt 混 instance 身份 id）。
        /// 用來讓「同一 tick、不同實例」抽到不同結果，而不需要讓 TickRandom 認識網路/身份來源。
        /// </summary>
        public static int Combine(int a, int b)
        {
            unchecked
            {
                var h = (uint)a * 2654435761u;
                h ^= (uint)b + 0x9E3779B9u + (h << 6) + (h >> 2);
                return (int)h;
            }
        }

        /// <summary>
        /// 把 seed 與 tick 混合成 [0,1) 的 float，使用整數 hash（避免浮點誤差造成跨平台不一致）。
        /// </summary>
        private static float Hash01(uint seed, uint tick)
        {
            var h = seed * 747796405u + 2891336453u;
            h ^= tick + 0x9E3779B9u + (h << 6) + (h >> 2);
            // final avalanche (PCG-style)
            h = ((h >> ((int)(h >> 28) + 4)) ^ h) * 277803737u;
            h = (h >> 22) ^ h;
            return (h & 0x00FFFFFFu) / 16777216f; // 24-bit 尾數精度，[0,1)
        }
    }
}
