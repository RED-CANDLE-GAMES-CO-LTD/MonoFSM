using MonoFSM.Core.Simulate;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonoFSM.Core
{
    /// <summary>
    ///  把「這一幀按下」從 Input System 的 dynamic update latch 起來，交給下一個 simulate tick 消費。
    ///  用途：判定跑在 Simulate（FixedUpdate/FixedUpdateNetwork）時，一個 render frame 可能跑 0 或多個 tick，
    ///  直接查 wasPressedThisFrame 會漏按或重複觸發；而且 condition 自己的 GameObject 不保證 active，
    ///  不能靠它自己的 Update 來 poll，所以集中在這個常駐 driver 做。
    ///  查詢用 <see cref="WasPressed"/>，第一次查詢時才會開始 poll 那顆鍵。
    /// </summary>
    public static class CheatKeyLatch
    {
        private const int NotFired = int.MinValue;
        //Key enum 沒有 Count，動態取最大值（只在 static init 算一次）
        private static readonly int KeyCount = MaxKeyValue() + 1;

        private static int MaxKeyValue()
        {
            var max = 0;
            foreach (var v in System.Enum.GetValues(typeof(Key)))
            {
                var i = (int)v;
                if (i > max)
                    max = i;
            }

            return max;
        }

        private static bool[] _watched;
        private static bool[] _latched;
        private static int[] _firedTick;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _watched = new bool[KeyCount];
            _latched = new bool[KeyCount];
            _firedTick = new int[KeyCount];
            for (var i = 0; i < KeyCount; i++)
                _firedTick[i] = NotFired;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateDriver()
        {
            var go = new GameObject("[CheatKeyLatch]") { hideFlags = HideFlags.HideAndDontSave };
            go.AddComponent<CheatKeyLatchDriver>();
            Object.DontDestroyOnLoad(go);
        }

        /// <summary>
        ///  這顆鍵在「上一次被消費之後」有沒有被按下。同一個 tick 內重複查詢結果一致，之後的 tick 不再觸發。
        /// </summary>
        public static bool WasPressed(Key key)
        {
            var k = (int)key;
            if (_watched == null || k <= 0 || k >= KeyCount)
                return false;

            _watched[k] = true; //lazy 註冊：有人問了才開始 poll
            if (!_latched[k])
                return false;

            var tick = WorldUpdateSimulator.CurrentTick;
            if (_firedTick[k] == NotFired)
            {
                _firedTick[k] = tick;
                return true;
            }

            return _firedTick[k] == tick;
        }

        internal static void Poll()
        {
            if (_watched == null)
                return;
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            for (var k = 1; k < KeyCount; k++)
            {
                if (!_watched[k])
                    continue;

                //已經被某個 tick 消費過了，這一幀起失效；沒被消費的留著，避免 0 tick 的 frame 漏按
                if (_firedTick[k] != NotFired)
                {
                    _latched[k] = false;
                    _firedTick[k] = NotFired;
                }

                if (keyboard[(Key)k].wasPressedThisFrame)
                    _latched[k] = true;
            }
        }
    }

    /// <summary>
    ///  <see cref="CheatKeyLatch"/> 的每幀 driver，執行期自動生成，不需要掛在場景上。
    /// </summary>
    [AddComponentMenu("")]
    public class CheatKeyLatchDriver : MonoBehaviour
    {
        private void Update()
        {
            CheatKeyLatch.Poll();
        }
    }
}
