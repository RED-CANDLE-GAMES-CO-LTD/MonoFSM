using System;
using UnityEngine;

namespace RCGMaker.Runtime
{
    public interface IAnimationDoneReceiver
    {
        void OnAnimationDone(int shortNameHash);
    }

    //當狀態機播完動畫，自動關掉animator，節省效能
    public class AutoDisableAnimator : MonoBehaviour
    {
        private Animator _animator;
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

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator)
            {
                _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                _animator.keepAnimatorStateOnDisable = true;
                _receiver = GetComponent<IAnimationDoneReceiver>(); //不一定有}
            }
        }

        private void OnEnable()
        {
            SetAnimatorEnable(true);
        }


        private void LateUpdate() //只想知道切走的那一瞬間.. 又需要reset
        {
            if (_animator.IsInTransition(0))
                return;
            var currentState = _animator.GetCurrentAnimatorStateInfo(0);
            
            //播新的動畫，重置            
            if (currentState.shortNameHash != _lastAnimatorStateHash && currentState.normalizedTime < 1)
            {
                // Debug.Log("Change State" + currentState.shortNameHash + gameObject.name, gameObject);
                _isReceivingAnimationDone = true;
                _lastAnimatorStateHash = currentState.shortNameHash;

                this.Log("Receiving Done Enable" + currentState.shortNameHash);
            }

            //播完動畫，關掉animator
            if (_lastAnimatorStateHash == currentState.shortNameHash && currentState.normalizedTime >= 1)
            {
                
                if (!_isReceivingAnimationDone) return;
                this.Log("Done" + currentState.shortNameHash);
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
            Debug.Log("Disable Animator" + gameObject.name, gameObject);
        }

        private void SetAnimatorEnable(bool enable)
        {
            _animator.enabled = enable;
            enabled = enable;


            if (enable)
            {
                if (!string.IsNullOrEmpty(defaultStateName))
                {
                    Debug.Log("Play Default State" + defaultStateName);
                    _animator.Play(defaultStateName, 0, 0);
                    _lastAnimatorStateHash = -1;
                }    
            }
            
                
        }
    }
}