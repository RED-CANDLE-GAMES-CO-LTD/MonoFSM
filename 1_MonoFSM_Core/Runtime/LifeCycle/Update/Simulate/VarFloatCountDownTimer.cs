using System;
using MonoFSM.Core.Simulate;
using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Simulate
{
    //0表示valid
    /// <summary>
    /// FIXME: fusion有 ticktimer
    /// </summary>
    public class VarFloatCountDownTimer : MonoBehaviour, IUpdateSimulate, ISceneStart
    {
        
        
        [InfoBox(
            "This timer counts down from a specified value to zero. It can be reset to a maximum value or a specific value. It is used to control the timing of events in the game.")]
        [DropDownRef] public VarFloat currentTime;

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

        [PreviewInInspector] [AutoChildren] AbstractConditionBehaviour[] _conditions;

        public void Simulate(float deltaTime)
        {
            //FIXME: 還要有condition?
            if (!_conditions.IsAllValid())
                return;
            if (currentTime.CurrentValue > currentTime.Min)
            {
                // Debug.Log("Counting down" + currentTime.CurrentValue + " " + Time.deltaTime);
                _lastTime = currentTime.CurrentValue;
                currentTime.SetValue(currentTime.CurrentValue - deltaTime, this); //TimeProvider
            }
        }

        public void AfterUpdate()
        {
        }

        public void EnterSceneStart()
        {
            ResetTimer();
        }
    }
}