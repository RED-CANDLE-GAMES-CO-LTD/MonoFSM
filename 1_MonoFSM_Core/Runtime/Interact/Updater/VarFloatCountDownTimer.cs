using System;
using MonoFSM_Core.Network;
using MonoFSM.Variable;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core
{
    //0表示valid

    public class VarFloatCountDownTimer : MonoBehaviour, IUpdateSimulate
    {
        [InfoBox(
            "This timer counts down from a specified value to zero. It can be reset to a maximum value or a specific value. It is used to control the timing of events in the game.")]
        [DropDownRef] public VarFloat currentTime;

        public void ResetTimer()
        {
            //每一日可能還不依樣？
            ResetTimer(currentTime.Max);
        }

        public void ResetTimer(float value)
        {
            Debug.Log("ResetTimer:" + value, this);
            currentTime.SetValue(value, this);
        }

        [PreviewInInspector] float _lastTime;

        // private void Update()
        // {
        //   
        // }

        [PreviewInInspector] [AutoChildren] AbstractConditionComp[] _conditions;

        public void Simulate(float deltaTime)
        {
            //FIXME: 還要有condition?
            if (!_conditions.IsAllValid())
                return;
            if (currentTime.CurrentValue > currentTime.Min)
            {
                // Debug.Log("Counting down" + currentTime.CurrentValue + " " + Time.deltaTime);
                _lastTime = currentTime.CurrentValue;
                currentTime.SetValue(currentTime.CurrentValue - deltaTime); //TimeProvider
            }
        }

        public void AfterUpdate()
        {
        }
    }
}