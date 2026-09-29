using UnityEngine;

namespace MonoFSM.Core.Simulate
{
    /// <summary>
    /// 排進 <see cref="TickActionQueue"/> 的一次性工作：下一個 forward tick 內、WorldUpdateSimulator.Simulate
    /// 跑 MonoObj.Simulate 之前被呼叫。回 false = 目標還沒準備好，留在 queue 下一個 tick 再問
    /// （要延幾次、逾時怎麼處理由實作自己決定，queue 不管）。
    /// </summary>
    public interface ITickAction
    {
        bool ExecuteInTick(int tick);
    }

    /// <summary>
    /// 「在 tick 內執行一次」的 static queue：給 Editor probe（`up hit`）、scenario runner 這種在 tick 外
    /// 被呼叫、但要改 gameplay 狀態的工具用。直接在 Editor update 裡改狀態，Fusion 下 networked Var 的寫入
    /// 會落在 tick 外、被 resim 蓋掉；排進來就跟 PlayerInteractState 一樣在 Simulate 裡跑。
    /// 容量固定（預先配好，不長大），只在非 resim 的 tick 消耗；執行中又排進來的留到下一個 tick。
    /// </summary>
    public static class TickActionQueue
    {
        public const int Capacity = 64;

        private static readonly ITickAction[] _queue = new ITickAction[Capacity];
        private static int _head;
        private static int _count;

        /// <summary>還沒被執行的數量。</summary>
        public static int PendingCount => _count;

        /// <summary>最後一次消耗 queue 的 tick（除錯用，-1 = 還沒消耗過）。</summary>
        public static int LastDrainTick { get; private set; } = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            System.Array.Clear(_queue, 0, _queue.Length);
            _head = 0;
            _count = 0;
            LastDrainTick = -1;
        }

        /// <summary>排進 queue；滿了回 false（不會擴容）。</summary>
        public static bool Enqueue(ITickAction action)
        {
            if (action == null || _count >= Capacity)
                return false;
            _queue[(_head + _count) % Capacity] = action;
            _count++;
            return true;
        }

        /// <summary>由 WorldUpdateSimulator.Simulate 呼叫，resim tick 不呼叫。</summary>
        internal static void Drain(int tick)
        {
            LastDrainTick = tick;
            //只跑進來時就在的那幾個，執行中新排的留到下一個 tick
            var n = _count;
            for (var i = 0; i < n; i++)
            {
                var action = _queue[_head];
                _queue[_head] = null;
                _head = (_head + 1) % Capacity;
                _count--;
                //一顆出錯不能讓整個 Simulate 斷掉
                try
                {
                    //還沒準備好就排回隊尾；這輪只跑 n 個，所以會留到下一個 tick
                    if (!action.ExecuteInTick(tick) && !Enqueue(action))
                        Debug.LogError("[TickActionQueue] 延後的工作排不回去（queue 滿了），丟掉");
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
