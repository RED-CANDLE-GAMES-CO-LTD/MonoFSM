using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;

namespace MonoFSM.Core.Simulate
{
    //0表示valid
    /// <summary>
    /// 從 Time 到 0 的倒數計時器，時間到會觸發事件，可以重置到最大值或特定值。
    /// FIXME: fusion有 ticktimer
    /// 搭配 ResetTimerAction 使用
    /// </summary>
    public class VarFloatCountDownTimer : AbstractDescriptionBehaviour, IUpdateSimulate, IResetStart
    {
        public override bool IsDrawingValueInfo => true;
        public override string ValueInfo => _currentTime.Value.ToString("F2");

        public override string Description =>
            $"CountDownTimer: {_currentTime.Value:F2} / {_currentTime.Max:F2}";

        [InfoBox(
            "This timer counts down from a specified value to zero. It can be reset to a maximum value or a specific value. It is used to control the timing of events in the game.")]
        [SerializeField]
        private VarFloatWrapper _currentTime = new();

        [Tooltip("時間到後是否自動重新開始")]
        [SerializeField] bool _autoRestart = false;

        [TitleGroup("Decay 設定")] [SerializeField] [Tooltip("每秒衰減量 = _decayMultiplier * deltaTime")]
        private VarFloatWrapper _decayMultiplier = new(1f);

        [TitleGroup("Decay 設定")] [SerializeField] [Tooltip("Reset 後延遲多久才開始衰減")]
        private VarFloatWrapper _startDecayDelay = new(0f);

        [ShowInInspector] [ReadOnly] private float _delayRemaining;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        OnTimeUpHandler _onTimeUpHandler;

        public void ResetTimer()
        {
            if (!isActiveAndEnabled)
                return;
            //每一日可能還不依樣？
            SetTimer(_currentTime.Max);
        }

        /// <summary>
        /// 特定
        /// </summary>
        /// <param name="value"></param>
        public void SetTimer(float value)
        {
            // Debug.Log("ResetTimer:" + value, this);
            _currentTime.SetValue(value, this);
            _delayRemaining = _startDecayDelay.Value;
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
            if (_currentTime.Value > _currentTime.Min)
            {
                // delay 階段：倒數 delay，不衰減
                if (_delayRemaining > 0f)
                {
                    _delayRemaining -= deltaTime;
                    return;
                }

                _lastTime = _currentTime.Value;
                float rate = _decayMultiplier.Value > 0f ? _decayMultiplier.Value : 1f;
                _currentTime.SetValue(_currentTime.Value - rate * deltaTime, this);

                // 檢測時間到（從 > Min 變成 <= Min）
                if (_currentTime.Value <= _currentTime.Min)
                {
                    OnTimeUp();
                }
            }

            // 自動重新開始
            if (_currentTime.Value <= _currentTime.Min)
            {
                if (_autoRestart)
                {
                    ResetTimer();
                }
            }
        }

        void OnTimeUp()
        {
            // 觸發所有 OnTimeUpHandler
            _onTimeUpHandler?.EventHandle();



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
