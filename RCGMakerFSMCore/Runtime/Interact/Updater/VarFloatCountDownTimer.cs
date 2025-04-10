using System;
using MonoFSM.Variable;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Core
{
    //0表示valid

    public class VarFloatCountDownTimer : MonoBehaviour
    {
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

        private void Update()
        {
            //FIXME: 還要有condition?
            if (!_conditions.IsAllValid())
                return;
            if (currentTime.CurrentValue > currentTime.Min)
            {
                // Debug.Log("Counting down" + currentTime.CurrentValue + " " + Time.deltaTime);
                _lastTime = currentTime.CurrentValue;
                currentTime.SetValue(currentTime.CurrentValue - Time.deltaTime);
            }
        }

        [PreviewInInspector] [AutoChildren] AbstractConditionComp[] _conditions;
    }
}