using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

namespace RCGInputAction
{
    public class OnPlayerInputJoined : MonoBehaviour, ISceneStart
    {
        //Editor串的...蠻爛的
        public void OnGenerate(PlayerInput playerInput)
        {
            //FIXME: 原本在場上的會多叫一次？而且太早，是在OnEnable, 應該要等到LevelStart?
            //code很醜...
            if (_isReady)
                PoolManager.PreparePoolObjectImplementation(playerInput.GetComponent<PoolObject>());
        }

        private bool _isReady = false;

        public void EnterSceneStart()
        {
            _isReady = true;
        }
    }
}