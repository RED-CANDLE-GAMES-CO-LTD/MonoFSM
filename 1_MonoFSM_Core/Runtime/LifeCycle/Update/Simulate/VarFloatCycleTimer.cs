using MonoFSM.Variable;
using MonoFSM.Variable.Condition;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using MonoFSMCore.Runtime.LifeCycle;

namespace MonoFSM.Core.Simulate
{
    /// <summary>
    /// 正數循環計時器，適用於 24 小時制等循環時間系統。
    /// 搭配 TimeCompareCondition 使用。
    /// </summary>
    public class VarFloatCycleTimer : MonoBehaviour, IUpdateSimulate, IResetStart
    {
        [InfoBox("Counts up and wraps at max value. Use timeScale to control speed (e.g. 60 = 1 real second per game minute).")]
        [DropDownRef]
        [Component]
        public VarFloat _currentTime;

        [Tooltip("How many game-seconds pass per real-second. e.g. 60 = 1 real sec per game minute")]
        [SerializeField] private float _timeScale = 60f;

        [Tooltip("Starting time")] [SerializeField]
        private TimeOfDay _startTime = new TimeOfDay { _hours = 8, _minutes = 0 };

        [ShowInInspector] [ReadOnly]
        private string CurrentTimeDisplay => _currentTime != null
            ? TimeOfDay.FromFloat(_currentTime.Value).ToString()
            : "--:--";

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private AbstractConditionBehaviour[] _conditions;

        public void Simulate(float deltaTime)
        {
            if (!_conditions.IsAllValid())
                return;

            var newTime = _currentTime.Value + deltaTime * _timeScale / 3600f;
            _currentTime.SetValue(Mathf.Repeat(newTime, _currentTime.Max), this);
        }

        public void ResetStart()
        {
            _currentTime.SetValue(_startTime.ToFloat(), this);
        }
    }
}
