using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Simulate;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.ParticleSystemActions
{
    /// <summary>
    ///     掛在 EventHandler 底下當 render action：被觸發時把火焰 pulse 一下（整體變大 / noise 變強）再衰減回原樣。
    ///     「何時閃」由 handler 決定（ex: OnValueDirectionChangedHandler 設 Increase），這裡只決定「閃多大」：
    ///     事件帶進來的 arg（變化量）配合 _fullPulseDelta 縮放 pulse 幅度。
    ///     純 Render 層表現：
    ///     - AbstractEventHandler 的 render 觸發在 ShouldSimulate gate 之前，所以 client（proxy）也會跑
    ///     - client 端的 Var 值由 NetworkedVarSync 寫入時同樣會觸發 handler，各端自己閃，不需要 render sync
    ///     衰減靠 IRenderUpdate 每幀跑（父層要有 MonoObj 才會被分派；加完 component 記得重跑 Auto）。
    /// </summary>
    public class ParticlePulseRender : AbstractRenderBehaviour,
        IArgRenderBehaviour<float>, IRenderUpdate, IResetStateRestore
    {
        public override string Description =>
            $"Pulse {(_scaleTarget != null ? _scaleTarget.name : "particles")}";

        [Title("Pulse 目標")]
        [Tooltip("可選：pulse 這個 Transform 的 localScale，火焰整體變大最明顯（通常指到火焰特效的 root）")]
        [SerializeField]
        private Transform _scaleTarget;

        [Tooltip("scale pulse 峰值倍率增量，0.25 = 峰值時放大到 1.25 倍")] [SerializeField]
        private float _scalePulseRatio = 0.25f;

        [Tooltip("可選：pulse 這些 ParticleSystem 的 startSize / noise strength（只影響 pulse 期間新生的粒子）")]
        [SerializeField]
        private ParticleSystem[] _particleSystems;

        [Tooltip("startSize pulse 峰值倍率增量，0.5 = 峰值時 1.5 倍")] [SerializeField]
        private float _startSizePulseRatio = 0.5f;

        //用加法而非倍率：noise strength 的 baseline 常常是 0（module 沒開或沒設），乘法會完全沒效果。
        //註：strength 若是 TwoConstants 模式，這裡改到的是上限（constantMax），下限不動 → noise 往正向增強。
        [Tooltip("noise strength pulse 峰值加量（加法，baseline 為 0 也有效）")] [SerializeField]
        private float _noiseStrengthPulseAdd = 1f;

        [Title("強度")]
        [Tooltip("事件帶進來的變化量（delta）達到這個值就 pulse 到滿幅；設 0 = 不看 delta，一律滿幅")]
        [SerializeField]
        private float _fullPulseDelta;

        [Title("時間")] [Tooltip("從峰值衰減回原樣要多久（秒）")] [SerializeField]
        private float _duration = 0.25f;

        [Tooltip("衰減曲線指數，越大越像「啪」一下就收；1 = 線性")] [SerializeField]
        private float _falloffPower = 2f;

        [Tooltip("勾了才印觸發 / 被略過的 log")] [SerializeField]
        private bool _debugLog;

        [ShowInInspector] private float _strength = 1f;
        [ShowInInspector] private float _elapsed;
        [ShowInInspector] private bool _isPulsing;

        private bool _hasBaseline;
        private Vector3 _baseScale;
        private float[] _baseStartSize;
        private float[] _baseNoiseStrength;

        //帶 arg 的事件：OnValueDirectionChangedHandler 傳進來的是變化量絕對值
        public void OnArgEnterRender(float arg)
        {
            //EnterArgRenderInvoke 只檢查 isActiveAndEnabled，condition 要自己檢查（比照 AbstractRenderBehaviour.OnEnterRender）
            if (IsConditionValid == false)
            {
                if (_debugLog)
                    Debug.Log("[ParticlePulse] condition invalid, skip", this);
                return;
            }

            var strength = _fullPulseDelta > 0f ? Mathf.Clamp01(arg / _fullPulseDelta) : 1f;
            if (strength <= 0f)
            {
                if (_debugLog)
                    Debug.Log("[ParticlePulse] delta " + arg + " → strength 0, skip", this);
                return;
            }

            if (_debugLog)
                Debug.Log("[ParticlePulse] pulse! delta " + arg + " strength " + strength, this);
            StartPulse(strength);
        }

        public void OnArgRender(float arg) { }

        //不帶 arg 的事件源（一般 render action 用法）：滿幅 pulse
        public override void OnEnterRenderImplement()
        {
            StartPulse(1f);
        }

        public override void OnRenderImplement() { }

        public void Render(float runnerLocalRenderTime)
        {
            if (_isPulsing == false)
                return;

            _elapsed += Time.deltaTime;
            var t = _duration > 0f ? 1f - _elapsed / _duration : 0f;
            if (t <= 0f)
            {
                ApplyPulse(0f);
                _isPulsing = false;
                return;
            }

            ApplyPulse((_falloffPower == 1f ? t : Mathf.Pow(t, _falloffPower)) * _strength);
        }

        public void ResetStateRestore(bool isHardReset)
        {
            if (_hasBaseline)
                ApplyPulse(0f);
            _isPulsing = false;
            _elapsed = 0f;
        }

        private void StartPulse(float strength)
        {
            EnsureBaseline();
            _strength = strength;
            _elapsed = 0f;
            _isPulsing = true;
            ApplyPulse(strength); //立刻推到峰值，不等下一幀
        }

        private void OnDisable()
        {
            //pulse 中被關掉的話 Render 就不會再跑，值會卡在放大狀態
            if (_isPulsing && _hasBaseline)
                ApplyPulse(0f);
            _isPulsing = false;
        }

        private void EnsureBaseline()
        {
            var count = _particleSystems != null ? _particleSystems.Length : 0;
            if (_hasBaseline && _baseStartSize != null && _baseStartSize.Length == count)
                return;

            if (_scaleTarget != null)
                _baseScale = _scaleTarget.localScale;

            _baseStartSize = new float[count];
            _baseNoiseStrength = new float[count];
            for (var i = 0; i < count; i++)
            {
                var ps = _particleSystems[i];
                if (ps == null)
                {
                    Debug.LogError("[ParticlePulse] _particleSystems[" + i + "] is null", this);
                    continue;
                }

                _baseStartSize[i] = ps.main.startSizeMultiplier;
                _baseNoiseStrength[i] = ps.noise.strengthMultiplier;
            }

            _hasBaseline = true;
        }

        /// <summary>
        ///     k = 1 是滿幅峰值，k = 0 回到原樣
        /// </summary>
        private void ApplyPulse(float k)
        {
            if (_scaleTarget != null)
                _scaleTarget.localScale = _baseScale * (1f + _scalePulseRatio * k);

            if (_particleSystems == null)
                return;

            for (var i = 0; i < _particleSystems.Length; i++)
            {
                var ps = _particleSystems[i];
                if (ps == null)
                    continue;

                if (_startSizePulseRatio != 0f)
                {
                    var main = ps.main; //struct wrapper，不產生 GC
                    main.startSizeMultiplier = _baseStartSize[i] * (1f + _startSizePulseRatio * k);
                }

                if (_noiseStrengthPulseAdd != 0f)
                {
                    var noise = ps.noise;
                    noise.strengthMultiplier = _baseNoiseStrength[i] + _noiseStrengthPulseAdd * k;
                }
            }
        }
    }
}
