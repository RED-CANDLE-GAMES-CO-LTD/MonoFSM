using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    [Serializable]
    [InlineProperty]
    public struct TimeOfDay
    {
        [HorizontalGroup("Time")]
        [LabelText("H")]
        [LabelWidth(15)]
        [PropertyRange(0, 23)]
        public int _hours;

        [HorizontalGroup("Time")]
        [LabelText("M")]
        [LabelWidth(15)]
        [PropertyRange(0, 59)]
        public int _minutes;

        public float ToFloat() => _hours + _minutes / 60f;

        public override string ToString() => $"{_hours:D2}:{_minutes:D2}";

        public static TimeOfDay FromFloat(float time)
        {
            var clamped = Mathf.Repeat(time, 24f);
            var hours = (int)clamped;
            var minutes = Mathf.RoundToInt((clamped - hours) * 60f);
            if (minutes >= 60) { minutes = 0; hours = (hours + 1) % 24; }
            return new TimeOfDay { _hours = hours, _minutes = minutes };
        }
    }
}
