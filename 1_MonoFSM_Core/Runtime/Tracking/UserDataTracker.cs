//TODO: 抽象化 Mixpanel 的 Value，讓不同的追蹤系統可以接入

namespace MonoFSM.RCGMakerFSMCore.Tracking
{
    /// <summary>
    /// Interface for trackable data containers that hold properties and values for analytics tracking.
    /// Implementations should wrap specific analytics service values (e.g., Mixpanel event properties).
    /// </summary>
    public interface ITrackableValue
    {
        /// <summary>
        /// Sets a property key-value pair to be included in the tracked event.
        /// </summary>
        /// <param name="key">The property name/key</param>
        /// <param name="value">The property value (can be string, number, boolean, etc.)</param>
        void SetProperty(string key, object value);

        /// <summary>
        /// Finalizes and sends the tracking event with the previously set properties.
        /// </summary>
        /// <param name="eventName">Name of the event to be tracked</param>
        void Track(string eventName);
        //TODO: batch track?
    }

    /// <summary>
    /// Interface for analytics tracking systems.
    /// Implementations should handle the lifecycle of tracking value objects
    /// and connect to the specific analytics service (e.g., Mixpanel).
    /// </summary>
    public interface ITracker
    {
        //TODO: opt in/out
        /// <summary>
        /// Gets a pre-allocated trackable value container.
        /// This approach uses an object pool pattern to reduce garbage collection.
        /// </summary>
        /// <returns>A reusable tracking data container</returns>
        ITrackableValue BorrowTrackableValue();
        
        /// <summary>
        /// Returns a trackable value to the pool after use.
        /// </summary>
        /// <param name="value">The trackable value to recycle</param>
        void RecycleTrackableValue(ITrackableValue value);
    }

    /// <summary>
    /// Singleton access point for user data tracking.
    /// Works as a facade for the underlying tracking implementation.
    /// To use this system, assign an implementation of ITracker to the _tracker field.
    /// </summary>
    /// <remarks>
    /// Usage example:
    /// 1. Get a trackable value: var value = UserDataTracker.BorrowTrackableValue;
    /// 2. Set properties: value.SetProperty("level", 5);
    /// 3. Track the event: UserDataTracker.Track("level_complete", value);
    /// </remarks>
    public static class UserDataTracker
    {
        /// <summary>
        /// The tracker implementation to use for analytics.
        /// This should be assigned during app initialization by a Mixpanel wrapper
        /// or any other analytics service implementation.
        /// </summary>
        public static ITracker _tracker;
        
        /// <summary>
        /// Gets a new trackable value container from the current tracker.
        /// Returns null if no tracker has been assigned.
        /// </summary>
        public static ITrackableValue BorrowTrackableValue => _tracker?.BorrowTrackableValue();

        /// <summary>
        /// Tracks an event with the provided trackable value and automatically recycles it.
        /// </summary>
        /// <param name="eventName">The name of the event to track</param>
        /// <param name="trackableValue">The trackable value containing event properties</param>
        public static void Track(string eventName, ITrackableValue trackableValue)
        {
            //要傳GUID嗎？
            trackableValue.Track(eventName);
            //track完之後要回收
            _tracker.RecycleTrackableValue(trackableValue);
        }
    }
}