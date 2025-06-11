using System;
using MonoFSM.Core.Simulate;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.AnimatorActions
{
    public class CustomUpdateAnimator : MonoBehaviour, IUpdateSimulate
    {
        [Auto] private Animator _animator;

        private void Awake()
        {
            _animator.enabled = false;
        }


        //FIXME: 還沒做client同步呢，好難！
        public void Simulate(float deltaTime)
        {
            _animator.Update(deltaTime); //FIXME: 兩邊都跑? state要同步，看來要用animated platform?
        }

        public void AfterUpdate()
        {
        }
    }
}