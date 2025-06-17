using MonoFSM.Core.Attributes;
using UnityEngine;

namespace MonoFSM_InputAction
{
    //UnityMonoInputAction / RewireMonoInputAction
    //FIXME:好像包的有點亂，這個又要polling local, 又要提供處理完的?
    public abstract class AbstractMonoInputAction : MonoBehaviour
    {
        [ShowInPlayMode] public abstract bool IsPressed { get; }

        [ShowInPlayMode] public abstract bool WasPressed { get; }

        // public abstract bool WasPressBuffered();
        [ShowInPlayMode] public abstract bool WasReleased { get; }

        [SOConfig("PlayerInputActionData")] [SerializeField]
        protected InputActionData _inputActionData;

        public int InputActionId => _inputActionData.actionID; //還是monobehaviour自己assign就好？

        [PreviewInInspector]
        public abstract bool IsLocalPressed { get; }

        //這個是Uinput的
        // public InputActionReference _actionRef;
        //FIXME: Unity input 再抽一層？

        //要做local buffer queue嗎？

        // private void Update()
        // {
        //     if (IsLocalPressed)
        //         Debug.Log("IsLocalPressed: " + name + " " + myAction.name + " " + myAction.triggered);
        // }
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