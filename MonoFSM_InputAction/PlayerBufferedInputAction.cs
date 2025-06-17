using System;
using System.Collections.Generic;
using RCGInputAction;
using MonoFSM.Core.Attributes;
// using InControl;
using UnityEngine;
using UnityEngine.Serialization;

namespace PlayerActionControl
{
    using UnityEngine.InputSystem;

    static class PlayerInputManager
    {
        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            // InputManager.OnDeviceAttached += OnDeviceAttached;
            // InputManager.OnDeviceDetached += OnDeviceDetached;
            PlayerBufferedInputAction._actionListenerDict.Clear();
        }
    }
    [Obsolete]
    public class PlayerBufferedInputAction:MonoBehaviour
    {
        [PreviewInInspector]
        public Dictionary<InputAction,PlayerBufferedInputAction> dict => _actionListenerDict;
        [FormerlySerializedAs("ActionRef")] public InputActionReference _actionRef;
        
        [AutoParent] private PlayerInput _playerInput;
        public static Dictionary<InputAction,PlayerBufferedInputAction> _actionListenerDict = new();
        public static PlayerBufferedInputAction GetListener(InputAction action)
        {
            return _actionListenerDict.GetValueOrDefault(action);
        }

        public InputAction myAction => _playerInput.actions[_actionRef.name];
        private void Start()
        {
            if (_actionListenerDict.ContainsKey(myAction))
            {
                Debug.LogError("ActionRef.action already exist in dict!?",this);
                Debug.Break();
                return;
            }
            _actionListenerDict[myAction] = this;
            // _actionListenerDict[ActionRef] = this;
        }

        private void Update()
        {
            //FIXME: 檢查前叫就好
            UpdateAction();
        }
        [PreviewInInspector]
        List<float> _bufferedQueue = new();
        [PreviewInInspector]
        float _lastPressTime = -1;
        
        public void ForceWasPressAction() //自動操作時可以用 (自動格擋
        {
            _bufferedQueue.Add(Time.time);
        }
        public void ConsumedBuffer()
        {
            _bufferedQueue.RemoveAt(0);
        }
        // public float RemoveFirstBufferAndGetLatestTime(PlayerAction action) 
        // {
        //     if (actionDict.ContainsKey(action) && actionDict[action].Count > 0)
        //     {
        //         //最友善的寫法
        //         var lastTime = actionDict[action][^1];
        //         actionDict[action].RemoveAt(0); //移除掉最舊的
        //         return lastTime; //回傳最新的    
        //     }
        //     return -1;
        // }
        //FIXME: 要開出來調嗎？
        private const float InputBufferTime = 0.1f;

        //從network來？
        private void UpdateAction()
        {
            for (var i = 0; i < _bufferedQueue.Count; i++)
            {
                if (_bufferedQueue[i] + InputBufferTime < Time.time)
                {
                    _bufferedQueue.RemoveAt(i);
                    i--;
                }
            }

            if (myAction.WasPressedThisFrame())
            {
                // Debug.Log(action.Name + " in buffer WasPressed:" + Time.time);
                _bufferedQueue.Add(Time.time);
                _lastPressTime = Time.time;
            }
        }
        [PreviewInInspector]
        public bool WasPressBuffered()
        {
            return _bufferedQueue.Count > 0;
        }
    }

    
}