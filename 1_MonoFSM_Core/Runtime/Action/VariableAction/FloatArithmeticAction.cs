using System;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction
{
    [Obsolete]
    public class FloatArithmeticAction : AbstractStateAction
    {
        [ValueTypeValidate(typeof(VarFloat),IsVariableNeeded = true)] [SerializeField] [DropDownRef]
        private ValueProvider _targetVarProvider;

        [SerializeField]
        private ArithmeticOperation _operation;

        [InfoBox("$TypeValidationMessage", InfoMessageType.Warning, VisibleIf = "ShowTypeWarning")]
        [InfoBox("$TypeValidationMessage", InfoMessageType.None, VisibleIf = "ShowTypeInfo")]
        [SerializeField]
        [DropDownRef]
        [ValueTypeValidate(typeof(float))] //,IsVariableNeeded = true錯了？
        private ValueProvider _operandProvider;

        [ShowIf(nameof(RequiresTwoOperands))]
        [SerializeField]
        [DropDownRef]
        [ValueTypeValidate(typeof(float), ConditionalMethod = nameof(RequiresTwoOperands))]
        private ValueProvider _secondOperandProvider;

        public enum ArithmeticOperation
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Modulo,
            AddAssign,      // target += operand
            SubtractAssign, // target -= operand
            Set             // target = operand
        }

        private bool RequiresTwoOperands()
        {
            return _operation == ArithmeticOperation.Add ||
                   _operation == ArithmeticOperation.Subtract ||
                   _operation == ArithmeticOperation.Multiply ||
                   _operation == ArithmeticOperation.Divide ||
                   _operation == ArithmeticOperation.Modulo;
        }

        private string TypeValidationMessage
        {
            get
            {
                if (_targetVarProvider == null || _operandProvider == null)
                    return "請設定目標變數和運算元提供者";

                // 檢查目標變數類型是否為 Float
                //FIXME: 想說用ValueTypeValidate就做掉了？
                var targetValueType = _targetVarProvider.ValueType;
                if (targetValueType == null)
                    return "無法取得目標變數類型";

                if (!IsFloatCompatible(targetValueType))
                    return $"目標變數必須是 Float 類型，目前是：{targetValueType.Name}";

                var operandType = _operandProvider.ValueType;
                if (operandType == null || !IsFloatCompatible(operandType))
                    return $"運算元類型不相容：{operandType?.Name}";

                if (RequiresTwoOperands())
                {
                    if (_secondOperandProvider == null)
                        return "此運算需要第二個運算元";

                    var secondOperandType = _secondOperandProvider.ValueType;
                    if (secondOperandType == null || !IsFloatCompatible(secondOperandType))
                        return $"第二運算元類型不相容：{secondOperandType?.Name}";
                }

                return $"類型相容：{targetValueType.Name} {GetOperationSymbol()} {operandType.Name}" +
                       (RequiresTwoOperands() && _secondOperandProvider != null ? $" {GetOperationSymbol()} {_secondOperandProvider.ValueType.Name}" : "");
            }
        }

        private bool ShowTypeWarning => !ShowTypeInfo && _targetVarProvider != null && _operandProvider != null;
        private bool ShowTypeInfo => _targetVarProvider != null && _operandProvider != null && IsValidEditorConfiguration();

        private bool IsValidEditorConfiguration()
        {
            if (_targetVarProvider == null || _operandProvider == null)
                return false;

            var targetVarType = _targetVarProvider.ValueType;
            if (targetVarType == null || !IsFloatCompatible(targetVarType))
                return false;

            if (!IsFloatCompatible(_operandProvider.ValueType))
                return false;

            if (RequiresTwoOperands())
                return _secondOperandProvider != null && IsFloatCompatible(_secondOperandProvider.ValueType);

            return true;
        }

        private bool IsValidConfiguration()
        {
            // var targetVar = GetTargetVariable();
            // if (!(targetVar is VarFloat) || _operandProvider == null)
            //     return false;

            if (!IsFloatCompatible(_operandProvider.ValueType))
                return false;

            if (RequiresTwoOperands())
                return _secondOperandProvider != null && IsFloatCompatible(_secondOperandProvider.ValueType);

            return true;
        }

        private bool IsFloatCompatible(System.Type type)
        {
            return type == typeof(float) || type == typeof(int) || type == typeof(double);
        }

        private AbstractMonoVariable GetTargetVariable()
        {
            return _targetVarProvider?.GetVar<AbstractMonoVariable>();
        }

        private string GetOperationSymbol()
        {
            return _operation switch
            {
                ArithmeticOperation.Add => "+",
                ArithmeticOperation.Subtract => "-",
                ArithmeticOperation.Multiply => "*",
                ArithmeticOperation.Divide => "/",
                ArithmeticOperation.Modulo => "%",
                ArithmeticOperation.AddAssign => "+=",
                ArithmeticOperation.SubtractAssign => "-=",
                ArithmeticOperation.Set => "=",
                _ => "?"
            };
        }

        protected override void OnActionExecuteImplement()
        {
            var targetVar = GetTargetVariable();
            if (!(targetVar is VarFloat floatVar))
            {
                Debug.LogError("FloatArithmeticAction: Target variable must be VarFloat", this);
                return;
            }

            if (_operandProvider == null)
            {
                Debug.LogError("FloatArithmeticAction: Operand provider not found", this);
                return;
            }

            if (!IsValidConfiguration())
            {
                Debug.LogError("FloatArithmeticAction: Invalid configuration", this);
                return;
            }

            var operand1 = _operandProvider.Get<float>();
            float result;

            switch (_operation)
            {
                case ArithmeticOperation.AddAssign:
                    result = floatVar.Value + operand1;
                    break;
                case ArithmeticOperation.SubtractAssign:
                    result = floatVar.Value - operand1;
                    break;
                case ArithmeticOperation.Set:
                    result = operand1;
                    break;
                case ArithmeticOperation.Add:
                case ArithmeticOperation.Subtract:
                case ArithmeticOperation.Multiply:
                case ArithmeticOperation.Divide:
                case ArithmeticOperation.Modulo:
                    if (_secondOperandProvider == null)
                    {
                        Debug.LogError("FloatArithmeticAction: Second operand required for this operation", this);
                        return;
                    }
                    // var operand2 = System.Convert.ToSingle(_secondOperandProvider.Get<object>());
                    var operand2 = _secondOperandProvider.Get<float>();
                    result = _operation switch
                    {
                        ArithmeticOperation.Add => operand1 + operand2,
                        ArithmeticOperation.Subtract => operand1 - operand2,
                        ArithmeticOperation.Multiply => operand1 * operand2,
                        ArithmeticOperation.Divide => operand2 != 0 ? operand1 / operand2 : 0,
                        ArithmeticOperation.Modulo => operand2 != 0 ? operand1 % operand2 : 0,
                        _ => operand1
                    };
                    break;
                default:
                    result = operand1;
                    break;
            }

            floatVar.SetValue(result, this);
        }

        public override string Description
        {
            get
            {
                // var targetVar = GetTargetVariable();
                // if (targetVar == null)
                //     return "FloatArithmeticAction: No Target Variable";
                if (_operandProvider == null)
                    return "FloatArithmeticAction: No Operand Provider";

                var targetDesc = _targetVarProvider?.Description;
                var operand1Desc = _operandProvider.Description;

                if (RequiresTwoOperands())
                {
                    var operand2Desc = _secondOperandProvider?.Description ?? "?";
                    return $"{targetDesc} = {operand1Desc} {GetOperationSymbol()} {operand2Desc}";
                }
                else
                {
                    return $"{targetDesc} {GetOperationSymbol()} {operand1Desc}";
                }
            }
        }
    }
}
