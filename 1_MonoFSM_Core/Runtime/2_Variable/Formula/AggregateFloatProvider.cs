using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Variable;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Formula
{
    //不同種valueProvider的聚合計算
    public class AggregateFloatProvider : AbstractValueSource<float>
    {
        public enum AggregationType
        {
            Sum,
            Average,
            Min,
            Max,
            Count,
        }

        // [AutoChildren] [CompRef] [Required] [Tooltip("The component that provides the list of objects to process.")]
        // private IMonoDescriptableListProvider _inputProvider;

        // [ValueTypeValidate(typeof(List<MonoEntity>))] //var -> VarListEntity, value-> MonoEntity
        // // [Auto]
        // // [CompRef]
        // [Tooltip("The MonoEntity list provider to use for aggregation.")]
        // [SerializeField]
        // private ValueProvider _monoEntityListProvider; //FIXME: 應該改用VarList?

        public VarList<MonoEntity> _monoEntityList;

        [ShowInInspector] public int ItemCount => _monoEntityList?.Value?.Count ?? 0;
        //用VarListEntity?

        [SerializeField]
        [Required]
        [Tooltip("The variable tag to look for on each object to get the float value.")]
        [SOConfig("VariableType")]
        private VariableTag _variableToAggregate;

        [SerializeField]
        private AggregationType _operation = AggregationType.Sum;

        public override float Value => GetValue();

        public float GetValue()
        {
            if (_monoEntityList == null)
            {
                Debug.LogError("MonoEntity list is not assigned.", this);
                return 0f;
            }

            var entities = _monoEntityList.Value;
            if (entities == null)
                return 0f;

            var count = entities.Count;
            if (count == 0)
                return 0f;

            if (_operation == AggregationType.Count)
                return count;

            var sum = 0f;
            var min = float.MaxValue;
            var max = float.MinValue;
            var hasValue = false;

            for (var i = 0; i < count; i++)
            {
                var value = GetFloatFromDescriptable(entities[i]);
                sum += value;

                if (hasValue == false)
                {
                    min = value;
                    max = value;
                    hasValue = true;
                    continue;
                }

                if (value < min)
                    min = value;

                if (value > max)
                    max = value;
            }

            if (hasValue == false)
                return 0f;

            switch (_operation)
            {
                case AggregationType.Sum:
                    return sum;
                case AggregationType.Average:
                    return sum / count;
                case AggregationType.Min:
                    return min;
                case AggregationType.Max:
                    return max;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float GetFloatFromDescriptable(MonoEntity entity)
        {
            // return 0;
            if (entity == null)
            {
                Debug.LogError("Entity is null, cannot get variable value.", this);
                return 0f;
            }

            var variable = entity.VariableFolder.GetVariable(_variableToAggregate);
            if (variable == null)
            {
                Debug.LogError(
                    $"Variable '{_variableToAggregate.name}' not found on '{entity.name}'.",
                    entity
                );
                return 0f;
            }

            // if (variable is IFloatProvider floatProvider)
            //     return floatProvider.Value;
            if (variable is VarFloat varFloat)
                return varFloat.Value;
            if (variable is VarInt varInt)
                return varInt.Value;


            // Fallback for variables that are not IFloatProvider but can be converted
            // if (variable.ValueType == typeof(float))
            //     return variable.Get<float>();
            //
            // if (variable.ValueType == typeof(int))
            //     return variable.Get<int>();

            Debug.LogWarning(
                $"Variable '{_variableToAggregate.name}' on '{entity.name}' is not a float provider or a convertible type.",
                entity
            );
            return 0f;
        }

        public string Description =>
            $"{_operation} of '{_variableToAggregate?.name}' from '{_monoEntityList?.name}'";
    }
}
