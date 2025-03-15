//TODO: 抽象化 Mixpanel 的 Value，讓不同的追蹤系統可以接入

namespace RCG_Maker_FSM_Core_Package.RCGMakerFSMCore.Tracking
{
    public interface ITrackableValue //到時候還要把Mixpanel的Value多包一層
    {
        void SetProperty(string key, object value); // 用來設定tracking data的屬性

        void Track(string eventName); //最後送出
        //TODO: batch track?
    }

    public interface ITracker //override這個？assign這個？
    {
        //TODO: opt in/out
        ITrackableValue BorrowTrackableValue(); // 取得預分配的屬性容器
        void RecycleTrackableValue(ITrackableValue value);
    }

    //singleton DI
    public static class UserDataTracker //Singleton, 實作DI掉
    {
        //RCG Mixpanel wrapper package要assign這個Tracker
        public static ITracker _tracker;
        public static ITrackableValue BorrowTrackableValue => _tracker?.BorrowTrackableValue();

        public static void Track(string eventName, ITrackableValue trackableValue)
        {
            //要傳GUID嗎？
            trackableValue.Track(eventName);
            //track完之後要回收
            _tracker.RecycleTrackableValue(trackableValue);
        }
    }
}