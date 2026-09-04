using System.Threading;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Utilities
{
    /// <summary>
    ///     判斷目前是不是 Unity 主執行緒。
    ///     用途：field initializer / 建構子 / ISerializationCallbackReceiver.OnAfterDeserialize
    ///     這些可能跑在 loading thread 的地方，只要碰到 Application.isPlaying、EditorApplication.*、
    ///     或任何 UnityEngine API 就會丟 "can only be called from the main thread"。
    ///     先用 IsMainThread 擋掉，非主執行緒時走不依賴 Unity API 的 fallback 路徑。
    /// </summary>
    public static class UnityMainThread
    {
        //RuntimeInitializeOnLoadMethod 與 InitializeOnLoad 都保證在主執行緒跑，用來記下 thread id。
        private static int _mainThreadId = -1;

        public static bool IsMainThread =>
            _mainThreadId != -1 && Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Capture()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

#if UNITY_EDITOR
        //Editor 下 domain reload 後不一定跑 RuntimeInitializeOnLoadMethod（非 play mode），要另外抓一次。
        [UnityEditor.InitializeOnLoadMethod]
        private static void CaptureInEditor()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }
#endif
    }
}
