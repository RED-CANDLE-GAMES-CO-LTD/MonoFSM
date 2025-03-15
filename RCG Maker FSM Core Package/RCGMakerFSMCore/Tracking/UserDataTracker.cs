//TODO: 抽象化 Mixpanel 的 Value，讓不同的追蹤系統可以接入

namespace RCG_Maker_FSM_Core_Package.RCGMakerFSMCore.Tracking
{
    public interface ITrackableValue //到時候還要把Mixpanel的Value多包一層
    {
        void SetProperty(string key, object value);
        void Clear(); // 用來重置內容，避免 GC
    }

    public interface ITracker //override這個？assign這個？
    {
        ITrackableValue BorrowTrackableValue(); // 取得預分配的屬性容器
        void Track(string eventName, ITrackableValue value);
    }

    //singleton DI
    public static class UserDataTracker //Singleton, 實作DI掉
    {
        //RCG Mixpanel wrapper package要assign這個Tracker
        public static ITracker _tracker;

        public static ITrackableValue BorrowTrackableValue => _tracker?.BorrowTrackableValue();

        public static void Track(string eventName, ITrackableValue value)
        {
            //要傳GUID嗎？
            _tracker.Track(eventName, value);
        }
    }
}