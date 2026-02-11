using System;
using MonoFSM.Core.Simulate;
using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using MonoFSM.Core;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;

namespace MonoFSM.Core.Simulate
{
    //0表示valid
    /// <summary>
    /// FIXME: fusion有 ticktimer
    /// </summary>
    public class VarFloatCountDownTimer : MonoBehaviour, IUpdateSimulate, IResetStart
    {
        [InfoBox(
            "This timer counts down from a specified value to zero. It can be reset to a maximum value or a specific value. It is used to control the timing of events in the game.")]
        [DropDownRef]
        [Component]
        public VarFloat currentTime;

        [SerializeField] bool _autoRestart = false;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        OnTimeUpHandler _onTimeUpHandler;

        public void ResetTimer()
        {
            //每一日可能還不依樣？
            SetTimer(currentTime.Max);
        }

        /// <summary>
        /// 特定
        /// </summary>
        /// <param name="value"></param>
        public void SetTimer(float value)
        {
            // Debug.Log("ResetTimer:" + value, this);
            currentTime.SetValue(value, this);
        }

        [PreviewInInspector] float _lastTime;

        // private void Update()
        // {
        //
        // }

        //FIXME: 還要有condition? 暫停？
        [CompRef] [AutoChildren(DepthOneOnly = true)]
        AbstractConditionBehaviour[] _conditions;

        public void Simulate(float deltaTime)
        {
            if (!_conditions.IsAllValid())
                return;
            if (currentTime.CurrentValue > currentTime.Min)
            {
                // Debug.Log("Counting down" + currentTime.CurrentValue + " " + Time.deltaTime);
                _lastTime = currentTime.CurrentValue;
                currentTime.SetValue(currentTime.CurrentValue - deltaTime, this); //TimeProvider

                // 檢測時間到（從 > Min 變成 <= Min）
                if (currentTime.CurrentValue <= currentTime.Min)
                {
                    OnTimeUp();
                }
            }
        }

        void OnTimeUp()
        {
            // 觸發所有 OnTimeUpHandler
            _onTimeUpHandler?.EventHandle();


            // 自動重新開始
            if (_autoRestart)
            {
                ResetTimer();
            }
        }

        public void AfterUpdate()
        {
        }

        // public void EnterSceneStart()
        // {
        //     ResetTimer(); //先後問題？
        // }

        public void ResetStart()
        {
            ResetTimer();
        }
    }
}
