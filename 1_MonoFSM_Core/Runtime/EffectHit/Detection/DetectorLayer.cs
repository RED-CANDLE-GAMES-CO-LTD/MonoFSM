using UnityEngine;

namespace MonoFSM.Core.Detection
{
    /// <summary>
    /// trigger 型偵測器（<c>TriggerDetectorSource</c>）固定使用的 physics layer，全專案唯一的定義點。
    /// collision matrix 照這顆 layer 配（Detector × Hittable 有勾、× Player / × Detector 沒勾），
    /// 偵測器放錯 layer 會靜默多打或打不到。
    /// 目前整個專案只有一顆 Detector layer；以後為了效能要拆成多顆時，把這裡改成可設定的來源
    /// （例如 TriggerDetectorSource 上的欄位、預設值仍指向這裡）即可，呼叫端只讀 <see cref="Index"/>。
    /// </summary>
    public static class DetectorLayer
    {
        public const string Name = "Detector";

        // -2 = 還沒查過。NameToLayer 不能在 static 初始化 / 序列化執行緒呼叫，所以延後到第一次讀取
        private static int _index = -2;

        /// <summary>Detector layer 的 index；專案沒有這個 layer 時回 -1（呼叫端要跳過檢查）。</summary>
        public static int Index
        {
            get
            {
                if (_index == -2) _index = LayerMask.NameToLayer(Name);
                return _index;
            }
        }

        public static bool Exists => Index >= 0;
    }
}
