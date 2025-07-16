using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Formula
{
    //不同種valueProvider的聚合計算
    public class AggregateFloatProvider : MonoBehaviour, IFloatProvider
    {
        public enum AggregationType
        {
            Sum,
            Average,
            Min,
            Max,
            Count
        }

        [AutoChildren] [CompRef] [Required] [Tooltip("The component that provides the list of objects to process.")]
        private IMonoDescriptableListProvider _inputProvider;

        [SerializeField] [Required] [Tooltip("The variable tag to look for on each object to get the float value.")]
        private VariableTag _variableToAggregate;

        [SerializeField] private AggregationType _operation = AggregationType.Sum;

        [PreviewInInspector] public float Value => GetValue();

        public float GetValue()
        {
            if (_inputProvider == null || _variableToAggregate == null) return 0f;

            var values = _inputProvider.GetDescriptables()
                .Select(GetFloatFromDescriptable)
                .ToList();

            if (!values.Any()) return 0f;

            switch (_operation)
            {
                case AggregationType.Sum:
                    return values.Sum();
                case AggregationType.Average:
                    return values.Average();
                case AggregationType.Min:
                    return values.Min();
                case AggregationType.Max:
                    return values.Max();
                case AggregationType.Count:
                    return values.Count;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float GetFloatFromDescriptable(MonoEntity entity)
        {
            if (entity == null)
            {
                Debug.LogError(
                    $"Descriptable is null in '{_inputProvider?.GetType().Name}' for variable '{_variableToAggregate.name}'.",
                    this);
                return 0f;
            }


            var variable = entity.VariableFolder.GetVariable(_variableToAggregate);
            if (variable == null)
            {
                Debug.LogError(
                    $"Variable '{_variableToAggregate.name}' not found on '{entity.name}'.",
                    entity);
                return 0f;
            }

            if (variable is IFloatProvider floatProvider) return floatProvider.Value;

            // Fallback for variables that are not IFloatProvider but can be converted
            if (variable.objectValue is float f) return f;
            if (variable.objectValue is int i) return i;

            Debug.LogWarning(
                $"Variable '{_variableToAggregate.name}' on '{entity.name}' is not a float provider or a convertible type.",
                entity);
            return 0f;
        }

        public string Description =>
            $"{_operation} of '{_variableToAggregate?.name}' from '{(_inputProvider as Component)?.name}'";
    }
}