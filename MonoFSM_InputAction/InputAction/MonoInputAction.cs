using MonoFSM.Core.Attributes;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM_InputAction
{
    //UnityMonoInputAction / RewireMonoInputAction
    //FIXME:好像包的有點亂，這個又要polling local, 又要提供處理完的?
    //寫錯啦！應該給一個能串接的對象，然後實作抽出去
    public interface IInputActionImplementation
    {
        public int InputActionId { get; }
        public bool FetchIsPressed { get; } //不給外部用？
        protected bool FetchWasPressed { get; }
        protected bool FetchWasReleased { get; }

        protected internal bool IsPressedCached { get; }
        protected internal bool WasPressedCached { get; }
        protected internal bool WasReleasedCached { get; }
        protected internal Vector2 Vec2ValueCached { get; }

        protected internal bool IsLocalPressed { get; }
        protected internal Vector2 ReadLocalVec2 { get; }
        protected internal Vector2 FetchVec2Value { get; }
        protected internal bool IsVec2 { get; }
        protected internal float PressTime { get; } // 已按住的時間
        protected internal float LastPressedTime { get; } // 上次按下的時間戳

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

        public Vector2 ReadValueVec2 =>
            _abstractInputActionImplementation.Vec2ValueCached; //可以被Override

        //什麼時候需要用到？local直接接？
        [ShowInPlayMode]
        public bool IsPressed => _abstractInputActionImplementation.IsPressedCached; //如果外掛

        [ShowInPlayMode]
        public bool WasPressed => _abstractInputActionImplementation.WasPressedCached;

        // public abstract bool WasPressBuffered();
        [ShowInPlayMode]
        public bool WasReleased => _abstractInputActionImplementation.WasReleasedCached;
        
        public int InputActionId => _abstractInputActionImplementation.InputActionId; //還是monobehaviour自己assign就好？

        public bool IsReadingVec2 => _abstractInputActionImplementation.IsVec2;

        /// <summary>
        /// 已按住的時間（秒）
        /// </summary>
        [ShowInPlayMode]
        public float PressTime => _abstractInputActionImplementation?.PressTime ?? 0f;

        /// <summary>
        /// 上次按下的時間戳（Time.time）
        /// </summary>
        [ShowInPlayMode]
        public float LastPressedTime => _abstractInputActionImplementation?.LastPressedTime ?? -1f;

        //FIXME: Debug last press time?


        //FIXME: buffer queue坐在input還是action上？

        // [PreviewInInspector] private float _lastPressTime = -1;
        // private const float InputBufferTime = 0.25f;
        // [PreviewInInspector] private List<float> _bufferedQueue = new(); //玩家過去按下的時間 ex: 連按兩下

        // [PreviewInInspector]
        // private bool WasPressLocalBuffered() //local time
        // {
        //     // _localPlayerInput.user
        //     QueueCheck(Time.time);
        //     return _bufferedQueue.Count > 0;
        // }
        //
        // //TODO: 要自動更新還是拿取的時候更新？
        // public bool WasPressLocalBuffered(float time)
        // {
        //     QueueCheck(time);
        //     if (_bufferedQueue.Count > 0)
        //         // Debug.Log("Buffered in Queue" + name);
        //         return true;
        //     else
        //         return false;
        // }

        //FIXME: local還是可以做buffered input? 甚至就把buffered結果傳出去？

        //TODO: 也可以做成個別時間檢查不remove?
        // private void QueueCheck(float time)
        // {
        //     for (var i = 0; i < _bufferedQueue.Count; i++)
        //         //已經超過buffer時間了
        //         if (_bufferedQueue[i] + InputBufferTime < time)
        //         {
        //             _bufferedQueue.RemoveAt(i);
        //             i--;
        //         }
        // }
    }
}
