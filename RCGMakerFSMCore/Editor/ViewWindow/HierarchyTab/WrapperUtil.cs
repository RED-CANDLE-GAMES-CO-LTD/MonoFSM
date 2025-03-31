using RCGMaker.Editor;
using UnityEngine;

namespace RCGMaker.Core.Editor
{
    public static class WrapperUtil
    {
        public static WrappedEvent curEvent => _curEvent ??= typeof(Event).GetFieldValue<Event>("s_Current").Wrap();
        static WrappedEvent _curEvent;
        public static WrappedEvent Wrap(this Event e) => new(e);
    }
}