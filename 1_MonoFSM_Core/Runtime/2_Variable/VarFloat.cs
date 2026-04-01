using System.Globalization;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Simulate;
using MonoFSM.EditorExtension;
using MonoFSM.Variable.Attributes;
using MonoFSM.Variable.FieldReference;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//CountdownTimer...直接掛在這個下面？
namespace MonoFSM.Variable
{
    /// <summary>
    /// 讓 Variable 在 ResetStateRestore 時參考此 interface 來決定還原值
    /// </summary>
    public interface IRestoreValueOverrider<T>
    {
        bool ShouldOverrideRestoreValue { get; }
        T GetRestoreValue();
    }


    /// <summary>
    /// A MonoBehaviour representation of a float variable that can be bound to scriptable data.
    /// This class provides functionality for float values that can be accessed, modified, and tracked
    /// across the application.
    /// </summary>
    public class VarFloat
        : AbstractFieldVariable<GameDataFloat, FlagFieldFloat, float>,
            ISerializedFloatValue,
            IHierarchyValueInfo, IStringTokenVar
    {
        public bool IsDirty => CurrentValue != LastValue; //這樣只會一個frame耶？完全不用resolve啊...?

        //FIXME: 需要一個reset value source? 回到maxValue or minValue之類的...?
        // public override GameFlagBase FinalData => BindData;

        [ShowInDebugMode]
        public int IntValue => Mathf.CeilToInt(CurrentValue);

        [ShowInPlayMode]
        public float Percentage => (CurrentValue - Min) / (Max - Min);

        //FIXME: 要editor time的時候GetComponent嗎？
        public float Min
        {
            get
            {
                if (valueSource is IFloatBoundProvider boundProvider)
                    return boundProvider.Min;
                return _boundModifier ? _boundModifier.MinValue : 0;
            }
        }

        [FormerlyNamedAs("MaxTest")]
        public float Max
        {
            get
            {
                if (valueSource is IFloatBoundProvider boundProvider)
                    return boundProvider.Max;
                return _boundModifier ? _boundModifier.MaxValue : float.MaxValue;
            }
        }

        public override void OnBeforePrefabSave()
        {
            base.OnBeforePrefabSave();
            if (_boundModifier != null)
            {
                //FIXME: 蛤？
                // Field.ResetToDefault();
                Field.Init(TestMode.Production, this);
                _boundModifier.EditorBoundCheck(ref Field.ProductionValue);
                _boundModifier.EditorBoundCheck(ref Field.DevValue);
                Debug.Log(
                    $"VarFloat OnBeforePrefabSave: Min={Min}, Max={Max}, CurrentValue={CurrentValue}",
                    this
                );
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public bool IsMax => CurrentValue >= Max;
        public bool IsMin => CurrentValue <= Min;

        [ShowInDebugMode]
        public bool IsDecreasing =>
            _lastDecreasingTime > 0 &&
            WorldUpdateSimulator.SimulationTime - _lastDecreasingTime < 0.2f;

        [ShowInDebugMode]
        private float _lastDecreasingTime;

        /// <summary>
        /// 把值寫入，表示
        /// </summary>
        /// <param name="lastValue"></param>
        /// <param name="currentValue"></param>
        protected override void ValueCommited(float lastValue, float currentValue)
        {
            if (currentValue < lastValue)
                _lastDecreasingTime =
                    WorldUpdateSimulator.SimulationTime; //FIXME: 會有float太大的問題嗎？ //FIXME: 還是要往上問？
        }

        [ShowInDebugMode]
        public bool IsIncreasing => CurrentValue > LastValue;

        [Component(AddComponentAt.Same)]
        [AutoChildren(false)] //[PreviewInInspector]
        [SerializeField]
        private VariableFloatBoundModifier _boundModifier; //FIXME: Nested Prefab時會有髒髒狀態？ 還是要Editor都寫GetComponent...?


        [CompRef] [AutoChildren(false)]
        private IRestoreValueOverrider<float> _restoreValueOverrider;

        // [PreviewInInspector] [Component] [AutoChildren]
        // AbstractVariableModifier<float>[] _setOperations;

        // [Button]
        // void TestAdd(float value)
        // {
        //     Value += value;
        // }
        // public float Value => CurrentValue;

        public string ValueInfo => CurrentValue.ToString(CultureInfo.CurrentCulture) ?? "";
        public bool IsDrawingValueInfo => true;

        public override bool IsValueExist => Field.CurrentValue != 0f; //  CurrentValue != 0f;

        public override void ResetStateRestore()
        {
            base.ResetStateRestore();

            // 如果有 overrider 且需要覆蓋，使用 overrider 提供的值
            if (_restoreValueOverrider is not { ShouldOverrideRestoreValue: true }) return;


            var restoreValue = _restoreValueOverrider.GetRestoreValue();
            // Debug.Log(
            //     $"VarFloat '{name}' resetting state restore with overrider value: {restoreValue}",
            //     this
            // );
            SetValue(restoreValue, this, "RestoreValueOverrider");
        }
    }
}
