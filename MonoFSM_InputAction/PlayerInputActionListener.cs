using System;
using System.Collections.Generic;
using RCGInputAction;
using RCGMaker.Core.Attributes;
// using InControl;
using UnityEngine;

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
            PlayerInputActionListener._actionListenerDict.Clear();
        }
    }
    public class PlayerInputActionListener:MonoBehaviour
    {
        // static Dictionary<InputAction,PlayerInputActionListener> _actionListenerDict = new();
        //
        [PreviewInInspector]
        public Dictionary<InputAction,PlayerInputActionListener> dict => _actionListenerDict;
        public InputActionReference ActionRef;
       
        // [AutoParent] PlayerInputActionBufferManager _bufferManager;
        // public static PlayerInputActionListener GetListener(InputAction action)
        // {
        //     if (_actionListenerDict.TryGetValue(action, out var listener))
        //     {
        //         return listener;
        //     }
        //     return null;
        // }
        [AutoParent] private PlayerInput _playerInput;
        public static Dictionary<InputAction,PlayerInputActionListener> _actionListenerDict = new();
        public static PlayerInputActionListener GetListener(InputAction action)
        {
            return _actionListenerDict.GetValueOrDefault(action);
        }

        public InputAction myAction => _playerInput.actions[ActionRef.name];
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
        const float inputBufferTime = 0.1f;
        void UpdateAction()
        {
            for (var i = 0; i < _bufferedQueue.Count; i++)
            {
                if (_bufferedQueue[i] + inputBufferTime < Time.time)
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