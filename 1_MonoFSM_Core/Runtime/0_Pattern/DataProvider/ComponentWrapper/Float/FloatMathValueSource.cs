using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._0_Pattern.DataProvider.ComponentWrapper.Float
{
    public enum FloatMathOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Min,
        Max
    }

    /// <summary>
    /// 兩個 float 值做四則運算的 ValueSource
    /// Value = _var1 (op) _var2
    /// </summary>
    public class FloatMathValueSource : AbstractValueSource<float>, IFloatProvider
    {
        [SerializeField] private VarFloatWrapper _var1;

        [HideLabel] [EnumToggleButtons] [SerializeField]
        private FloatMathOperation _operation;

        [SerializeField] private VarFloatWrapper _var2;

        public override float Value
        {
            get
            {
                var v1 = _var1.Value;
                var v2 = _var2.Value;
                return _operation switch
                {
                    FloatMathOperation.Add => v1 + v2,
                    FloatMathOperation.Subtract => v1 - v2,
                    FloatMathOperation.Multiply => v1 * v2,
                    FloatMathOperation.Divide => v2 != 0f ? v1 / v2 : 0f,
                    FloatMathOperation.Min => Mathf.Min(v1, v2),
                    FloatMathOperation.Max => Mathf.Max(v1, v2),
                    _ => 0f
                };
            }
        }

        private static string OpSymbol(FloatMathOperation op) => op switch
        {
            FloatMathOperation.Add => "+",
            FloatMathOperation.Subtract => "-",
            FloatMathOperation.Multiply => "×",
            FloatMathOperation.Divide => "/",
            FloatMathOperation.Min => "min",
            FloatMathOperation.Max => "max",
            _ => "?"
        };

        public override string Description =>
            $"{_var1.Description} {OpSymbol(_operation)} {_var2.Description}";
    }
}
