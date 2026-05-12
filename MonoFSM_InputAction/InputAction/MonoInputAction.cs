using MonoFSM.Core.Attributes;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM_InputAction
{
    //UnityMonoInputAction / RewireMonoInputAction
    //FIXME:好像包的有點亂，這個又要polling local, 又要提供處理完的?
    //寫錯啦！應該給一個能串接的對象，然後實作抽出去
    /// <summary>
    ///     Optional partner component that overrides MonoInputAction's local buffer logic.
    ///     掛在同一個 GameObject 上，MonoInputAction 偵測到後會自動委派 IsInBufferTime / ConsumePress 等。
    ///     用於 Fusion 等需要 rollback-safe 計時的情境。
    /// </summary>
    public interface IInputBufferProvider
    {
        bool IsInBufferTime { get; }
        void ConsumePress();
        float PressTime { get; }
        float LastPressDuration { get; }
        float LastPressedTime { get; }
    }

    public interface IInputActionImplementation
    {
        public int InputActionId { get; }
        public bool FetchIsPressed { get; } //不給外部用？
        public bool FetchWasPressed { get; }
        public bool FetchWasReleased { get; }

        //FIXME: cached都不對
        // protected internal bool IsPressedCached { get; }
        // protected internal bool WasPressedCached { get; }
        // protected internal bool WasReleasedCached { get; }
        // protected internal Vector2 Vec2ValueCached { get; }

        protected internal bool IsLocalPressed { get; }
        protected internal Vector2 ReadLocalVec2 { get; }
        protected internal Vector2 FetchVec2Value { get; }
        protected internal bool IsVec2 { get; }
        protected internal float PressTime { get; } // 已按住的時間
        protected internal float LastPressedTime { get; } // 上次按下的時間戳
        protected internal float LastPressDuration { get; } // 最近一次 press→release 的總時長

        /// <summary>
        /// 獲取當前時間（由實作層決定時間源：Time.time 或 Runner.SimulationTime）
        /// </summary>
        protected internal float GetCurrentTime();
    }

    //抽象的input介面
    //多這層的好處是，reference拉好後，要切換實作就換上面的IMonoInputAction就好
    public class MonoInputAction : MonoBehaviour //不要綁定 InputSystem?
    {
        #region 不會被Override, local input result

        public Vector2 LocalVec2 => _abstractInputActionImplementation.ReadLocalVec2; //不會被Override

        [PreviewInInspector]
        public bool IsLocalPressed => _abstractInputActionImplementation?.IsLocalPressed ?? false; //這個是local的
        #endregion

        //FIXME: 重命名, relay?
        [CompRef]
        [Auto]
        private IInputActionImplementation _abstractInputActionImplementation;

        // Optional partner，沒掛時走下方本地 buffer 邏輯。手動 lazy lookup 避免 [Auto] missing 噴 log。
        [ShowInInspector] private IInputBufferProvider _bufferProvider;
        private bool _bufferProviderLookedUp;

        private IInputBufferProvider BufferProvider
        {
            get
            {
                if (!_bufferProviderLookedUp)
                {
                    _bufferProvider = GetComponent<IInputBufferProvider>();
                    _bufferProviderLookedUp = true;
                }
                return _bufferProvider;
            }
        }

        public Vector2 ReadValueVec2 =>
            _abstractInputActionImplementation?.FetchVec2Value ?? Vector2.zero;

        //什麼時候需要用到？local直接接？
        [ShowInPlayMode] public bool IsPressed => _abstractInputActionImplementation.FetchIsPressed;

        [ShowInPlayMode]
        public bool WasPressed =>
            _abstractInputActionImplementation.FetchWasPressed;

        // public abstract bool WasPressBuffered();
        [ShowInPlayMode]
        public bool WasReleased => _abstractInputActionImplementation.FetchWasReleased;

        public int InputActionId => _abstractInputActionImplementation.InputActionId; //還是monobehaviour自己assign就好？

        public bool IsReadingVec2 => _abstractInputActionImplementation.IsVec2;

        /// <summary>
        /// 已按住的時間（秒）
        /// </summary>
        [ShowInPlayMode]
        public float PressTime => BufferProvider?.PressTime
            ?? _abstractInputActionImplementation?.PressTime ?? 0f;

        /// <summary>
        /// 在 buffer 時間內且尚未被消費。勾選 _useBufferConsume 才生效。
        /// </summary>
        [ShowInPlayMode]
        public bool IsInBufferTime => BufferProvider?.IsInBufferTime
            ?? (_useBufferConsume && PressTime > 0 && PressTime < _bufferTime && !_isConsumed);

        [SerializeField] bool _useBufferConsume;

        [ShowIf(nameof(_useBufferConsume))]
        [SerializeField] float _bufferTime = 0.5f;

        bool _isConsumed;

        /// <summary>
        /// 標記此次 press 已被處理，IsInBufferTime 將回傳 false 直到下次 press 或 release。
        /// </summary>
        public void ConsumePress()
        {
            if (BufferProvider != null) BufferProvider.ConsumePress();
            else _isConsumed = true; //FIXME: 這個local 的isConsumed應該是虛設喔
        }

        /// <summary>
        /// 上次按下的時間戳（Time.time）
        /// </summary>
        [ShowInPlayMode]
        public float LastPressedTime => BufferProvider?.LastPressedTime
            ?? _abstractInputActionImplementation?.LastPressedTime ?? -1f;

        /// <summary>
        /// 最近一次完整 press→release 的總按壓時長（秒）。尚未有完整釋放過則為 0。
        /// </summary>
        [ShowInPlayMode]
        public float LastPressDuration => BufferProvider?.LastPressDuration
            ?? _abstractInputActionImplementation?.LastPressDuration ?? 0f;

        /// <summary>
        /// 由 IInputActionImplementation 在 CacheLocalInput 結束後呼叫，
        /// 確保在 input cache 更新後才重置 consume 狀態。
        /// </summary>
        public void OnInputCached()
        {
            if (!_useBufferConsume) return;

            // 新的 press 進來 → 重置 consume，允許新一輪判定
            if (WasPressed) _isConsumed = false;
            // release 後也重置，確保下次 press 可用
            if (WasReleased) _isConsumed = false;
        }
    }
}
