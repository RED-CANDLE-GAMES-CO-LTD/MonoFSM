using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonoFSM_InputAction
{
    //FIXME: 應該綁這個為主？DI IsPressed實作

    //Lcoal的
    [RequireComponent(typeof(MonoInputAction))]
    public class InputSystemInputActionImplementation
        : AbstractDescriptionBehaviour,
            IUpdateSimulate,
            IInputActionImplementation,
            IBeforeSimulate
    {
        [Auto] private MonoInputAction _monoInputAction;
        // [Required]
        // [PreviewInInspector] [AutoParent] private PlayerInput _localPlayerInput;
        public int InputActionId => _inputActionData.actionID; //還是monobehaviour自己assign就好？

        [InlineEditor]
        [SOConfig("PlayerInputActionData")]
        [SerializeField]
        protected InputActionData _inputActionData;

        // Cached input state（在 BeforeSimulate 中更新）
        [ShowInDebugMode] private bool _cachedIsPressed;
        [ShowInDebugMode] private bool _cachedWasPressed;
        [ShowInDebugMode] private bool _cachedWasReleased;
        [ShowInDebugMode] private Vector2 _cachedVec2;

        private bool _previousIsPressed;
        // 時間追蹤欄位
        [ShowInInspector]
        private float _pressStartTime = -1f;

        [ShowInInspector]
        private float _lastPressedTime = -1f;

        public InputAction myAction => _inputActionData?._inputAction?.action;

        //unity input system 的 action

        Vector2 IInputActionImplementation.Vec2ValueCached => _cachedVec2;

        bool IInputActionImplementation.IsLocalPressed =>
            myAction.IsPressed() ||
            myAction.WasPressedThisFrame(); //mouse需要waspressedthisFrame, 會同frame放開？

        Vector2 IInputActionImplementation.ReadLocalVec2 => myAction.ReadValue<Vector2>();


        Vector2 IInputActionImplementation.FetchVec2Value => myAction.ReadValue<Vector2>();

        [ShowInDebugMode] bool IInputActionImplementation.FetchIsPressed => myAction.IsPressed();

        [ShowInInspector]
        bool IInputActionImplementation.IsVec2 =>
            myAction.expectedControlType == "Vector2";


        bool IInputActionImplementation.FetchWasPressed =>
            myAction.WasPressedThisFrame(); //盡量不要用？因為有cache了，直接用cache的就好

        bool IInputActionImplementation.FetchWasReleased =>
            myAction.WasReleasedThisFrame(); //盡量不要用？

        bool IInputActionImplementation.IsPressedCached => _cachedIsPressed;

        bool IInputActionImplementation.WasPressedCached => _cachedWasPressed;
        bool IInputActionImplementation.WasReleasedCached => _cachedWasReleased;

        [ShowInDebugMode]
        float IInputActionImplementation.PressTime
        {
            get
            {
                if (!Application.isPlaying || _pressStartTime < 0f)
                    return 0f;

                if (_cachedIsPressed)
                    return ((IInputActionImplementation)this).GetCurrentTime() - _pressStartTime;

                return 0f;
            }
        }

        [ShowInDebugMode]
        float IInputActionImplementation.LastPressedTime => _lastPressedTime;

        /// <summary>
        /// 獲取當前時間 - 預設使用 Time.time，可在子類別中 override 使用 Runner.SimulationTime
        /// </summary>
        float IInputActionImplementation.GetCurrentTime() => WorldUpdateSimulator.SimulationTime;

        protected override string DescriptionTag => "Input";
        public override string Description => _inputActionData?.name;

        public void Simulate(
            float deltaTime
        ) //走beforesimulate?
        { }

        void IBeforeSimulate.BeforeSimulate(float deltaTime)
        {
            CacheLocalInput();
        }

        /// <summary>
        /// 從 Unity InputAction 讀取並 cache local input 狀態。
        /// 子類可 override 以跳過（例如 Fusion proxy 不需要 cache local input）。
        /// </summary>
        protected virtual void CacheLocalInput()
        {
            // Cache raw input state
            var rawIsPressed = ((IInputActionImplementation)this).FetchIsPressed;

            _cachedIsPressed = rawIsPressed;
            //好像都可以？
            // _cachedWasPressed =
            //     ((IInputActionImplementation)this)
            //     .FetchWasPressed;
            _cachedWasPressed = rawIsPressed && !_previousIsPressed;
            _cachedWasReleased = !rawIsPressed && _previousIsPressed;

            // Cache Vec2
            _cachedVec2 = ((IInputActionImplementation)this).FetchVec2Value;

            // 時間追蹤
            var currentTime = ((IInputActionImplementation)this).GetCurrentTime();
            if (_cachedWasPressed)
            {
                _pressStartTime = currentTime;
                _lastPressedTime = currentTime;
            }
            else if (_cachedWasReleased)
            {
                _pressStartTime = -1f;
            }

            _previousIsPressed = rawIsPressed;

            // 通知 MonoInputAction 更新 consume 狀態
            _monoInputAction.OnInputCached();
        }
    }
}
