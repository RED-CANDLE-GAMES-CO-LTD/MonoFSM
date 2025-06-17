using System;
using MonoFSM.Core.Attributes;
using UnityEngine;

namespace MonoFSM.Runtime
{
    public interface IAnimationDoneReceiver
    {
        void OnAnimationDone(int shortNameHash);
    }

    [DisallowMultipleComponent]
    //當狀態機播完動畫，自動關掉animator，節省效能
    public class AutoDisableAnimator : MonoBehaviour
    {
        [PreviewInInspector] [Auto(false)] private Animator _animator;
        private int _lastAnimatorStateHash;
        private bool _isReceivingAnimationDone = false;
        private IAnimationDoneReceiver _receiver;

        public string defaultStateName;

        public void SetDirty()
        {
            // _lastAnimatorStateHash = 0;
            if (_animator == null)
                return;
            SetAnimatorEnable(true);
        }

        private void OnEnable()
        {
            //可能是動態add的...還是要手動加？default state用撈的？
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_animator)
            {
                // Debug.Log("Enable Animator" + gameObject.name, gameObject);
                _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                _animator.keepAnimatorStateOnDisable = true;
                _receiver = GetComponent<IAnimationDoneReceiver>(); //不一定有}
            }
            SetAnimatorEnable(true);
        }


        private void LateUpdate() //只想知道切走的那一瞬間.. 又需要reset
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                enabled = false;
                return;
            }
                
            if (_animator.IsInTransition(0))
                return;
            var currentState = _animator.GetCurrentAnimatorStateInfo(0);

            this.Log(
                "LateUpdate" , currentState.shortNameHash , " " , currentState.normalizedTime , " " , gameObject);
            //播新的動畫，重置            
            if (currentState.shortNameHash != _lastAnimatorStateHash && currentState.normalizedTime < 1)
            {
                // Debug.Log("Change State" + currentState.shortNameHash + gameObject.name, gameObject);
                _isReceivingAnimationDone = true;
                _lastAnimatorStateHash = currentState.shortNameHash;

                this.Log("Receiving Done Enable" , currentState.shortNameHash);
            }
            

            //播完動畫，關掉animator
            //FIXME: 一播完就想關掉...但有些可能是transition到別的動畫，某些動畫不能關... looping的才能關？
            if (currentState.loop)
                return;
            //FIXME: Build出來好像行為不一致！
            if (_lastAnimatorStateHash == currentState.shortNameHash && currentState.normalizedTime >= 1)
            {
                if (!_isReceivingAnimationDone) return;
                this.Log("Done" , currentState.shortNameHash);
                OnAnimationDone(currentState.shortNameHash);
            }
            //onselect是event system的update觸發，animator State還沒change
        }

        private void OnAnimationDone(int shortNameHash)
        {
            SetAnimatorEnable(false);
            _isReceivingAnimationDone = false;
            _receiver?.OnAnimationDone(shortNameHash);
            // _lastAnimatorStateHash = 0;
            // Debug.Log("Disable Animator" + gameObject.name, gameObject);
        }

        private void SetAnimatorEnable(bool enable)
        {
            // Debug.Log("Set Animator Enable:" + enable + gameObject.name, gameObject);
            _animator.enabled = enable;
            enabled = enable;
        
            if (enable)
            {
                _lastAnimatorStateHash = -1;
                if (!string.IsNullOrEmpty(defaultStateName))
                {
                    // Debug.Log("Play Default State" + defaultStateName);
                    _animator.Play(defaultStateName, 0, 0);
                    // _animator.Update(0);
    
                }    
            }
        }
    }
}