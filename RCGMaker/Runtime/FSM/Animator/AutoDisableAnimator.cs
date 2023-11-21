using System;
using UnityEngine;

namespace RCGMaker.Runtime
{
    public interface IAnimationDoneReceiver
    {
        void OnAnimationDone(int shortNameHash);
    }

    public class AutoDisableAnimator : MonoBehaviour
    {
        private Animator _animator;
        private int _lastAnimatorStateHash;
        private bool _isReceivingAnimationDone = false;
        private IAnimationDoneReceiver _receiver;

        public void SetDirty()
        {
            // _lastAnimatorStateHash = 0;
            SetAnimatorEnable(true);
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            _animator.keepAnimatorStateOnDisable = true;
            _receiver = GetComponent<IAnimationDoneReceiver>(); //不一定有
        }

        private void OnEnable()
        {
            SetAnimatorEnable(true);
        }


        private void Update() //只想知道切走的那一瞬間.. 又需要reset
        {
            var currentState = _animator.GetCurrentAnimatorStateInfo(0);
            //播新的動畫，重置            
            if (currentState.shortNameHash != _lastAnimatorStateHash)
            {
                // Debug.Log("Change State" + currentState.shortNameHash + gameObject.name, gameObject);
                _isReceivingAnimationDone = true;
                _lastAnimatorStateHash = currentState.shortNameHash;
            }

            //播完動畫，關掉animator
            if (_lastAnimatorStateHash == currentState.shortNameHash && currentState.normalizedTime >= 1)
            {
                if (!_isReceivingAnimationDone) return;
                OnAnimationDone(currentState.shortNameHash);
            }
        }

        private void OnAnimationDone(int shortNameHash)
        {
            SetAnimatorEnable(false);
            _isReceivingAnimationDone = false;
            _receiver?.OnAnimationDone(shortNameHash);
            // Debug.Log("Disable Animator" + gameObject.name, gameObject);
        }

        private void SetAnimatorEnable(bool enable)
        {
            _animator.enabled = enable;
            enabled = enable;
        }
    }
}