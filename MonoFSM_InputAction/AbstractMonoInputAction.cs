using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonoFSM_InputAction
{
    //UnityMonoInputAction / RewireMonoInputAction
    public abstract class AbstractMonoInputAction : MonoBehaviour
    {
        public abstract bool IsPressed();

        // public abstract bool WasPressBuffered();
        public abstract bool WasPressed();
        public abstract bool WasReleased();

        //可以abstract
        // public void ManualUpdate(float time)
        // {
        //     //FIXME: unityinputsystem
        //     if (myAction.WasPressedThisFrame())
        //         // Debug.Log("Pressed this frame" + name);
        //         if (_lastPressTime != time)
        //         {
        //             // _bufferedQueue.Add(time);
        //             _lastPressTime = time;
        //         }
        // }

        [SOConfig("PlayerInputActionData")] [SerializeField]
        private InputActionData _inputActionData;

        public int InputActionId => _inputActionData.actionID; //還是monobehaviour自己assign就好？

        //這個是Uinput的
        // public InputActionReference _actionRef;
        //FIXME: 再抽一層？
        [PreviewInInspector] [AutoParent] private PlayerInput _localPlayerInput;

        // private InputActionMap _inputActionMap;
        public InputAction myAction => _localPlayerInput.actions[_inputActionData.inputAction.name];
        // public InputAction myAction => _localPlayerInput.currentActionMap.FindAction(_inputActionData.inputAction.name);

        public bool IsLocalPressed => myAction.IsPressed();
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