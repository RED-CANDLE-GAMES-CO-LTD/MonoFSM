using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._0_Pattern.DataProvider.ComponentWrapper.Float
{
    /// <summary>
    /// 回傳 VarFloat 的百分比值 (CurrentValue - Min) / (Max - Min)，範圍 0~1
    /// 可選 _maxOverride：未指定時用 VarFloat 自身的 Max，指定時用外部 ValueSource 的值作為 Max
    /// </summary>
    public class FloatPercentage : AbstractValueSource<float>, IFloatProvider
    {
        // protected override string DescriptionTag => "%";

        [Required] [DropDownRef] public VarFloat _varFloat;

        // [AutoChildren(DepthOneOnly = true)]
        //FIXME: 用nested還是怪怪的感覺...
        [CompRef] [Tooltip("可選：覆蓋 Max 值的來源，未指定時使用 VarFloat 自身的 Max")] [SerializeField]
        private AbstractValueSource<float> _maxOverride;

        [Tooltip("勾選後回傳 1 - percentage")] [SerializeField]
        private bool _invert;

        private bool _isEvaluating;

        [PreviewInInspector] private float Min => _varFloat != null ? _varFloat.Min : 0f;

        [PreviewInInspector]
        private float Max => _maxOverride != null ? _maxOverride.Value :
            _varFloat ? _varFloat.Max : Mathf.Infinity;

        public override float Value
        {
            get
            {
                if (_varFloat == null) return 0f;

                // 防禦性檢查：避免互相 reference 造成的無窮迴圈
                if (_isEvaluating)
                {
                    Debug.LogError(
                        $"[FloatPercentage] Infinite loop detected in {name}! Returning 0.", this);
                    return 0f;
                }

                _isEvaluating = true;
                try
                {
                    var range = Max - Min;
                    var pct = range > 0f ? (_varFloat.CurrentValue - Min) / range : 0f;
                    return _invert ? 1f - pct : pct;
                }
                finally
                {
                    _isEvaluating = false;
                }
            }
        }

        public override string Description => (_invert ? "1-% of: " : "% of: ")
            + _varFloat?.Description
            + (_maxOverride != null ? " / " + _maxOverride.Description : "");

        private void OnValidate()
        {
            if (_varFloat != null && _varFloat == GetComponentInParent<VarFloat>())
            {
                Debug.LogWarning(
                    $"[FloatPercentage] _varFloat references parent VarFloat ({_varFloat.name}), which is not allowed to prevent infinite loops. Field cleared.",
                    this);
                _varFloat = null;
            }
        }
    }
}
