using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Animation.Optimization
{
    public class BudgetUpdateAnimator : MonoBehaviour
    {
        private static readonly bool IsBudgetUpdateActive = false;
        [Auto] private Animator animator;

        private void Update()
        {
            if (IsBudgetUpdateActive)
                UpdateAnimCheck();
            else
            {
                animator.speed = 1;
                enabled = false;
            }
        }

        [ShowInInspector] protected int _animUpdateCount;
        // protected float _animUpdateTimer = 0;

        private bool UpdateAnimCheck() //兩個frame更新一次
        {
            // _animUpdateTimer += RCGTime.deltaTime;
            if (_animUpdateCount == 0)
            {
                animator.speed = 2;
                _animUpdateCount = 1;
                // SetBudgetActive(true);
                return false;
            }

            animator.speed = 0;
            _animUpdateCount = 0;
            // SetBudgetActive(false);
            return true;
        }
    }
}