using System.Diagnostics;
using MonoFSM.Core.Simulate;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.FSM
{
    /// <summary>
    /// FsmTrace 一筆紀錄的種類。State 類（Transition / Forced / Default / ProxyRender）填 machine + from/to，
    /// Effect 類（HitEnter / HitExit / HitBlocked）填 dealer + receiver。
    /// </summary>
    public enum FsmTraceKind : byte
    {
        /// <summary>StateMachine.TryActivateState → ChangeState。transition 為 null = 有人直接呼叫 TryActivateState。</summary>
        Transition,
        /// <summary>ForceActivateState（ForceChangeToStateAction、StateMachineLogic.RestoreAllPending、ForceDeactivate / ForceToggle）。</summary>
        Forced,
        /// <summary>FixedUpdate 發現 _activeStateId &lt; 0，進 default state。</summary>
        Default,
        /// <summary>Render 看到的 state 變了，但本機沒有走 ChangeState（proxy 從網路讀到、或 RestoreState 直接寫 id）。</summary>
        ProxyRender,
        /// <summary>GeneralEffectReceiver.OnEffectHitEnter（Detector 跟 ForceDirectEffectHit 兩條路都會到）。</summary>
        HitEnter,
        /// <summary>GeneralEffectReceiver.OnEffectHitExit。</summary>
        HitExit,
        /// <summary>dealer 對 receiver 的命中被擋掉，原因看 blockReason。</summary>
        HitBlocked,
        /// <summary>Var 的值真的變了（只記掛 NetworkedVarTag 的，或 FsmTrace.WatchVar 指定的）。writer = SetValue 的 byWho。</summary>
        VarChange,
        /// <summary>
        /// AbstractEventHandler 被叫到了、但 action 沒跑（原因看 skipReason）。_receiver 欄位放 handler。
        /// 同一顆 handler 連續被同一個原因擋只記第一次（OnStateUpdate 這種每 tick 叫的才不會洗掉 buffer），有跑到就重置。
        /// </summary>
        EventSkipped,
    }

    /// <summary>EventSkipped 的原因，一對一對應 AbstractEventHandler.EventHandleImplement 的每個 early return。</summary>
    public enum FsmTraceSkipReason : byte
    {
        /// <summary>不記（gameObject inactive 走這個：刻意關掉的節點，靜態讀 prefab 就看得到）。</summary>
        None,
        /// <summary>parent MonoObj IsCulling（開場 CullingEventTarget 還沒判定 near 時也是這個）。</summary>
        Culling,
        ConditionInvalid,
        NotStateAuthority,
        /// <summary>ShouldSimulate == false：render 那段有跑，simulate 的 action 沒跑。</summary>
        NotSimulating,
    }

    /// <summary>VarChange 的值存在哪個欄位：Number = _oldNum/_newNum，Object = _oldObj/_newObj，Ref = _oldRef/_newRef（string 等）。</summary>
    public enum FsmTraceValueKind : byte
    {
        None,
        Bool,
        Int,
        Float,
        Object,
        Ref,
        /// <summary>Vector3 之類沒有對應欄位的 value type，只記「變了」。</summary>
        Unsupported,
    }

    /// <summary>
    /// HitBlocked 的原因，一對一對應 GeneralEffectDealer.CanHitReceiver 的每個 early return
    /// （DealerInvalid 來自 EffectDetector.TriggerEnterForDealerAndDetectable）。
    /// </summary>
    public enum FsmTraceBlockReason : byte
    {
        None,
        ReceiverNull,
        SingleEntityLock,
        TypeMismatch,
        /// <summary>receiver.IsValid == false；failConditionIndex = receiver 第一顆失敗的 condition（-2 = receiver 沒 active）。</summary>
        ReceiverInvalid,
        ProxyDealerNull,
        /// <summary>dealer 的 _effectConditions 有一顆不過；failConditionIndex = 那顆的 index。</summary>
        EffectConditionFailed,
        /// <summary>dealer.IsValid == false；receiver 欄位放的是 EffectDetectable，failConditionIndex = dealer 第一顆失敗的 condition。</summary>
        DealerInvalid,
    }

    /// <summary>
    /// FsmTrace ring buffer 裡的一筆。只存 reference / int / enum / bool，零 GC、不組字串；
    /// 字串只在 Dump 時才組。物件被 Destroy 後 reference 會變 fake-null，dump 會印 &lt;destroyed&gt;。
    /// </summary>
    public struct FsmTraceEntry
    {
        public int _seq;
        public int _tick;
        public int _frame;
        public bool _isResim;
        public FsmTraceKind _kind;
        public FsmTraceBlockReason _blockReason;
        public FsmTraceSkipReason _skipReason;

        /// <summary>-1 = 沒有 / 不適用，-2 = resolver 自己沒 active（isActiveAndEnabled == false）。</summary>
        public int _failConditionIndex;

        public StateMachineLogic _machine;
        public Object _fromState;
        public Object _toState;
        public Object _transition;
        public Object _dealer;
        public Object _receiver;

        //VarChange
        public Object _var;
        public Object _writer;
        public FsmTraceValueKind _valueKind;
        public double _oldNum;
        public double _newNum;
        public Object _oldObj;
        public Object _newObj;
        public object _oldRef;
        public object _newRef;
    }

    /// <summary>
    /// FSM state 切換 + Effect 命中的 trace：static ring buffer（預先配好 4096 筆），給自動測試跟手動除錯用。
    /// 掛點寫法一律 <c>if (FsmTrace.Enabled) FsmTrace.RecordXxx(...)</c>，關著時只多一次 bool 判斷；
    /// 寫入 method 只在 Editor / Development Build 存在，正式 build 整個 call site 被拿掉。
    /// 「為什麼沒轉出去」不每 tick 記，改用 <see cref="CaptureSnapshot(MonoFSM.Runtime.MonoEntity)"/> 當下拍快照。
    /// 讀檔：<see cref="Dump"/> 寫到 Library/FsmTrace/&lt;name&gt;.txt，再用 `up fsm-trace` 看。
    /// </summary>
    public static partial class FsmTrace
    {
        public const int Capacity = 4096;

        /// <summary>failConditionIndex：沒有失敗 / 不適用。</summary>
        public const int NoFailIndex = -1;

        /// <summary>failConditionIndex：resolver 自己沒 active，condition 根本沒被問。</summary>
        public const int InactiveFailIndex = -2;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly FsmTraceEntry[] _buffer = new FsmTraceEntry[Capacity];
        private static int _writeIndex;
        private static int _count;
        private static int _totalWritten;
        private static bool _enabled;

#if UNITY_EDITOR
        private const string EnabledSessionKey = "MonoFSM.FsmTrace.Enabled";

        //開關存在 SessionState：進 Play Mode 的 domain reload 會把 static 清掉，使用者在選單勾的要留得住
        [UnityEditor.InitializeOnLoadMethod]
        private static void LoadEnabledFromSession()
        {
            _enabled = UnityEditor.SessionState.GetBool(EnabledSessionKey, false);
        }
#endif

        //沒開 domain reload 時 static 會留著上一輪的紀錄，進 Play Mode 一律清空
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Clear();
        }

        public static bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
#if UNITY_EDITOR
                UnityEditor.SessionState.SetBool(EnabledSessionKey, value);
#endif
            }
        }

        /// <summary>buffer 裡目前有幾筆（最多 Capacity）。</summary>
        public static int Count => _count;

        /// <summary>這輪一共寫過幾筆（超過 Capacity 的部分已被覆蓋）。</summary>
        public static int TotalWritten => _totalWritten;

        public static void Clear()
        {
            System.Array.Clear(_buffer, 0, _buffer.Length);
            _writeIndex = 0;
            _count = 0;
            _totalWritten = 0;
        }

        /// <summary>取第 i 筆（0 = 最舊還留著的那筆）。</summary>
        public static ref readonly FsmTraceEntry GetEntry(int i)
        {
            var start = _count < Capacity ? 0 : _writeIndex;
            return ref _buffer[(start + i) % Capacity];
        }

        private static ref FsmTraceEntry NextSlot(int tick)
        {
            ref var e = ref _buffer[_writeIndex];
            _writeIndex = (_writeIndex + 1) % Capacity;
            if (_count < Capacity)
                _count++;
            _totalWritten++;

            e = default;
            e._seq = _totalWritten;
            e._tick = tick;
            e._frame = Time.frameCount;
            e._isResim = WorldUpdateSimulator.IsResimulation;
            e._failConditionIndex = NoFailIndex;
            return ref e;
        }
#else
        public static bool Enabled
        {
            get => false;
            set { }
        }

        public static int Count => 0;
        public static int TotalWritten => 0;

        public static void Clear() { }
#endif

        /// <summary>記一筆 state 切換（Transition / Forced / Default / ProxyRender）。</summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void RecordState(
            FsmTraceKind kind,
            int tick,
            StateMachineLogic machine,
            Object fromState,
            Object toState,
            Object transition
        )
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_enabled)
                return;
            ref var e = ref NextSlot(tick);
            e._kind = kind;
            e._machine = machine;
            e._fromState = fromState;
            e._toState = toState;
            e._transition = transition;
#endif
        }

        /// <summary>
        /// 這顆 Var 的 VarChange 要不要記。預設只記掛了 NetworkedVarTag 的（core 不 reference Fusion，用型別名
        /// GetComponent，每顆 Var 只查一次、結果快取在 Var 身上）；WatchVar / UnwatchVar 可以手動蓋掉。
        /// </summary>
        public static bool ShouldTraceVar(MonoFSM.Variable.AbstractMonoVariable v)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_enabled || v == null)
                return false;
            if (v._fsmTraceWatch == WatchUnknown)
                v._fsmTraceWatch = v.GetComponent(NetworkedVarTagTypeName) != null ? WatchNetworked : WatchOff;
            return v._fsmTraceWatch == WatchNetworked || v._fsmTraceWatch == WatchManual;
#else
            return false;
#endif
        }

        private const string NetworkedVarTagTypeName = "NetworkedVarTag";

        //AbstractMonoVariable._fsmTraceWatch 的值
        internal const byte WatchUnknown = 0;
        internal const byte WatchNetworked = 1;
        internal const byte WatchOff = 2;
        internal const byte WatchManual = 3;

        /// <summary>手動把一顆 Var 加進 VarChange 白名單（沒掛 NetworkedVarTag 也記）。</summary>
        public static void WatchVar(MonoFSM.Variable.AbstractMonoVariable v)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (v != null)
                v._fsmTraceWatch = WatchManual;
#endif
        }

        /// <summary>不記這顆 Var 的 VarChange（連 networked 的也不記，給每 tick 都在變、會洗掉 buffer 的用）。</summary>
        public static void UnwatchVar(MonoFSM.Variable.AbstractMonoVariable v)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (v != null)
                v._fsmTraceWatch = WatchOff;
#endif
        }

        /// <summary>
        /// 記一筆 VarChange。T 是 bool / int / float 時塞 _oldNum/_newNum，UnityEngine.Object 塞 _oldObj/_newObj，
        /// 其他 reference type（string）存 reference，不組字串、不 boxing。呼叫端先問過 ShouldTraceVar。
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void RecordVarChange<T>(MonoFSM.Variable.AbstractMonoVariable v, T oldValue, T newValue,
            Object writer)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_enabled)
                return;
            //Vector3 / Quaternion 這類沒欄位可存的 value type 多半是每 tick 在變的連續值（實測 d_Muzzle Aim Target Pos
            //一秒 60 筆，幾分鐘就把 buffer 洗掉），記了也只印得出 "?"：只有 WatchVar 手動指定的才記
            if (typeof(T).IsValueType && typeof(T) != typeof(bool) && typeof(T) != typeof(int)
                && typeof(T) != typeof(float) && (v == null || v._fsmTraceWatch != WatchManual))
                return;
            ref var e = ref NextSlot(WorldUpdateSimulator.CurrentTick);
            e._kind = FsmTraceKind.VarChange;
            e._var = v;
            e._writer = writer;
            if (typeof(T) == typeof(bool))
            {
                e._valueKind = FsmTraceValueKind.Bool;
                e._oldNum = UnsafeUtility.As<T, bool>(ref oldValue) ? 1 : 0;
                e._newNum = UnsafeUtility.As<T, bool>(ref newValue) ? 1 : 0;
            }
            else if (typeof(T) == typeof(int))
            {
                e._valueKind = FsmTraceValueKind.Int;
                e._oldNum = UnsafeUtility.As<T, int>(ref oldValue);
                e._newNum = UnsafeUtility.As<T, int>(ref newValue);
            }
            else if (typeof(T) == typeof(float))
            {
                e._valueKind = FsmTraceValueKind.Float;
                e._oldNum = UnsafeUtility.As<T, float>(ref oldValue);
                e._newNum = UnsafeUtility.As<T, float>(ref newValue);
            }
            else if (!typeof(T).IsValueType)
            {
                //reference type 轉 object 不會配置記憶體
                var o = (object)oldValue;
                var n = (object)newValue;
                if (o is Object || n is Object || typeof(Object).IsAssignableFrom(typeof(T)))
                {
                    e._valueKind = FsmTraceValueKind.Object;
                    e._oldObj = o as Object;
                    e._newObj = n as Object;
                }
                else
                {
                    e._valueKind = FsmTraceValueKind.Ref;
                    e._oldRef = o;
                    e._newRef = n;
                }
            }
            else
            {
                e._valueKind = FsmTraceValueKind.Unsupported;
            }
#endif
        }

        /// <summary>記一筆 effect 命中（HitEnter / HitExit / HitBlocked）。tick 用 WorldUpdateSimulator.CurrentTick。</summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void RecordHit(
            FsmTraceKind kind,
            Object dealer,
            Object receiver,
            FsmTraceBlockReason blockReason = FsmTraceBlockReason.None,
            int failConditionIndex = NoFailIndex
        )
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_enabled)
                return;
            ref var e = ref NextSlot(WorldUpdateSimulator.CurrentTick);
            e._kind = kind;
            e._dealer = dealer;
            e._receiver = receiver;
            e._blockReason = blockReason;
            e._failConditionIndex = failConditionIndex;
#endif
        }

        /// <summary>記一筆 EventSkipped（handler 被叫到但 action 沒跑）。去重由呼叫端（AbstractEventHandler）負責。</summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void RecordEventSkipped(Object handler, FsmTraceSkipReason reason)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_enabled)
                return;
            ref var e = ref NextSlot(WorldUpdateSimulator.CurrentTick);
            e._kind = FsmTraceKind.EventSkipped;
            e._receiver = handler;
            e._skipReason = reason;
#endif
        }
    }
}
