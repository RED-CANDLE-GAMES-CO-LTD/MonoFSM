using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace MonoFSM.Core
{
    /// <summary>
    /// FSM / MonoObj 每幀分派用的 per-type ProfilerMarker 快取，讓 Profiler 跟 `uprofile top` / `gc` 能按 Action /
    /// Condition / IUpdateSimulate 的實作型別分開列（名稱 `FSM.Action/&lt;型別&gt;`、`FSM.Condition/&lt;型別&gt;`、
    /// `Simulate/&lt;型別&gt;` 等）。第一次遇到某型別才建 marker 並組名稱字串，之後查表不 alloc。
    /// 只在 Editor / Development build 存在；呼叫端也要用 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 包起來，
    /// release build 完全沒有成本。State 的 marker 用 State 名稱，存在 AbstractStateBehaviour 自己身上，不走這裡。
    /// </summary>
    public static class FsmProfilerMarkers
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        //只在 main thread 用（FSM / MonoObj 分派都在 main thread），所以不加鎖
        private static readonly Dictionary<Type, ProfilerMarker> _action = new(256);
        private static readonly Dictionary<Type, ProfilerMarker> _condition = new(256);
        private static readonly Dictionary<Type, ProfilerMarker> _beforeSimulate = new(64);
        private static readonly Dictionary<Type, ProfilerMarker> _simulate = new(128);
        private static readonly Dictionary<Type, ProfilerMarker> _afterSimulate = new(64);
        private static readonly Dictionary<Type, ProfilerMarker> _render = new(128);
        private static readonly Dictionary<Type, ProfilerMarker> _afterRender = new(64);

        /// <summary>`FSM.Action/&lt;型別&gt;`：AbstractEventHandler 分派給 receiver 執行的那一段。</summary>
        public static ProfilerMarker Action(Type type) => Get(_action, "FSM.Action/", type);

        /// <summary>`FSM.Condition/&lt;型別&gt;`：AbstractConditionBehaviour.FinalResult 裡呼叫 IsValid 的那一段。</summary>
        public static ProfilerMarker Condition(Type type) => Get(_condition, "FSM.Condition/", type);

        /// <summary>`BeforeSimulate/&lt;型別&gt;`：MonoObj 逐一呼叫 IBeforeSimulate。</summary>
        public static ProfilerMarker BeforeSimulate(Type type) => Get(_beforeSimulate, "BeforeSimulate/", type);

        /// <summary>`Simulate/&lt;型別&gt;`：MonoObj 逐一呼叫 IUpdateSimulate。</summary>
        public static ProfilerMarker Simulate(Type type) => Get(_simulate, "Simulate/", type);

        /// <summary>`AfterSimulate/&lt;型別&gt;`：MonoObj 逐一呼叫 IAfterSimulate。</summary>
        public static ProfilerMarker AfterSimulate(Type type) => Get(_afterSimulate, "AfterSimulate/", type);

        /// <summary>`Render/&lt;型別&gt;`：MonoObj 逐一呼叫 IRenderUpdate。</summary>
        public static ProfilerMarker Render(Type type) => Get(_render, "Render/", type);

        /// <summary>`AfterRender/&lt;型別&gt;`：MonoObj 逐一呼叫 IAfterRenderMono。</summary>
        public static ProfilerMarker AfterRender(Type type) => Get(_afterRender, "AfterRender/", type);

        private static ProfilerMarker Get(Dictionary<Type, ProfilerMarker> cache, string prefix, Type type)
        {
            if (cache.TryGetValue(type, out var marker))
                return marker;
            //只有第一次遇到這個型別才組字串（一次性 alloc）；同名 marker Unity 會回傳同一個 handle
            marker = new ProfilerMarker(ProfilerCategory.Scripts, prefix + type.Name);
            cache.Add(type, marker);
            return marker;
        }
#endif
    }
}
