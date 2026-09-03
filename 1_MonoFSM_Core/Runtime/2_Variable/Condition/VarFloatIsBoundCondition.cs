using MonoFSM.Condition;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    /// <summary>
    /// 拿 VarFloat 跟它自己的上下界比：已達 Max / 已達 Min，或百分比比較。
    /// 血量滿了、彈藥空了、電量低於 20% 這類「相對於上限」的判斷用這顆，
    /// 不要用 VarFloatCompareConstCondition（那個比的是絕對值，Max 一改就失準）。
    ///
    /// _boundType = Percentage 時比的是 VarFloat.Percentage = (CurrentValue - Min)/(Max - Min)，
    /// 搭 _op 與 _targetPercentage（[Range(0,1)]，所以 20% 填 0.2）。
    /// Min/Max 會沿 varRef 轉發給真正的來源 var，所以指向純 proxy 的 Var 也算得對；
    /// 但來源沒設 bound 時 Max 會 fallback 成 float.MaxValue，percentage 恆近 0。
    ///
    /// _varFloat.isActiveAndEnabled == false 時一律回 false（刻意對齊 VarBoolCompareCondition）。
    /// Odin preset 只有 Float Max / Float Min，Percentage 要手動選 _boundType。
    /// </summary>
    public class VarFloatIsBoundCondition : AbstractConditionBehaviour
    {
        [ConditionPreset("Float Max", Category = "Float", Priority = 100, ColorHex = "#FFB347")]
        private static void Preset_Max(VarFloatIsBoundCondition c)
        {
            c._boundType = BoundType.Max;
        }

        [ConditionPreset("Float Min", Category = "Float", Priority = 100, ColorHex = "#FFB347")]
        private static void Preset_Min(VarFloatIsBoundCondition c)
        {
            c._boundType = BoundType.Min;
        }


        public override string Description => _varFloat != null
            ? _boundType == BoundType.Percentage
                ? _varFloat.name + " % " + ArithmeticHelper.OperatorDescription(_op) + " " + (_targetPercentage * 100f).ToString("F0") + "%"
                : _varFloat.name + " is " +
                  (_boundType == BoundType.Max ? "max" : "min") //FIXME: InvertToken (NOT) 放這？
            : "null var";

        public enum BoundType
        {
            Max,
            Min,
            Percentage
        }

        public BoundType _boundType;
        [SerializeField] [DropDownRef] VarFloat _varFloat;

        [ShowIf(nameof(_boundType), BoundType.Percentage)]
        public Operator _op = Operator.GreaterThan;

        [ShowIf(nameof(_boundType), BoundType.Percentage)]
        [Range(0f, 1f)]
        public float _targetPercentage;

        [ShowIf(nameof(_boundType), BoundType.Percentage)]
        [ShowInInspector]
        private float CurrentPercentage => _varFloat != null ? _varFloat.Percentage : 0f;

        protected override bool IsValid
        {
            get
            {
                if (_varFloat == null) return false;

                //來源變數所在的物件被關掉 = 那個模組沒作用，條件一律不成立
                //對齊 VarBoolCompareCondition 的行為，讓「關掉模組」能連帶讓跨模組的數值判定失效
                if (_varFloat.isActiveAndEnabled == false) return false;

                return _boundType switch
                {
                    BoundType.Max => _varFloat.IsMax,
                    BoundType.Min => _varFloat.IsMin,
                    BoundType.Percentage => ArithmeticHelper.CompareValues(_varFloat.Percentage, _targetPercentage, _op),
                    _ => false
                };
            }
        }
    }
}
