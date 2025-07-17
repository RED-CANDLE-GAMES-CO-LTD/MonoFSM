using System;
using RCGExtension;
using UnityEngine;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

//CountdownTimer...直接掛在這個下面？
namespace MonoFSM.Variable
{
    /// <summary>
    /// A MonoBehaviour representation of a float variable that can be bound to scriptable data.
    /// This class provides functionality for float values that can be accessed, modified, and tracked
    /// across the application.
    /// </summary>
    public class VarFloat : GenericMonoVariable<GameDataFloat, FlagFieldFloat, float>, ISerializedFloatValue,
        IHierarchyValueInfo
    {
        public bool IsDirty => CurrentValue != LastValue; //這樣只會一個frame耶？完全不用resolve啊...?
        //FIXME: 需要一個reset value source? 回到maxValue or minValue之類的...? 
        // public override GameFlagBase FinalData => BindData;

        // public VariableTag Key => _varTag;
        [ShowInDebugMode]
        public int IntValue => Mathf.CeilToInt(CurrentValue);

        [ShowInPlayMode] public float Percentage => (CurrentValue - Min) / (Max - Min);

        //FIXME: 要editor time的時候GetComponent嗎？
        public float Min => _boundModifier ? _boundModifier.MinValue : float.MinValue;

        [FormerlyNamedAs("MaxTest")] public float Max => _boundModifier ? _boundModifier.MaxValue : float.MaxValue;

    
        public override void OnBeforePrefabSave()
        {
            base.OnBeforePrefabSave();
            if (_boundModifier != null)
            {
                _boundModifier.EditorBoundCheck(ref Field.ProductionValue);
                _boundModifier.EditorBoundCheck(ref Field.DevValue);
                Debug.Log($"VarFloat OnBeforePrefabSave: Min={Min}, Max={Max}, CurrentValue={CurrentValue}", this);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        public bool IsMax => CurrentValue >= Max;

        [ShowInDebugMode]
        public bool IsDecreasing =>
            _lastDecreasingTime > 0 && Time.time - _lastDecreasingTime < 0.2f;

        private float _lastDecreasingTime;

        /// <summary>
        /// 把值寫入，表示
        /// </summary>
        /// <param name="lastValue"></param>
        /// <param name="currentValue"></param>
        protected override void ValueCommited(float lastValue, float currentValue)
        {
            if (currentValue < lastValue) _lastDecreasingTime = Time.time; //FIXME: 還是要往上問？
        }

        
        [ShowInDebugMode]
        public bool IsIncreasing => CurrentValue > LastValue;

        [AutoChildren(false)] //[PreviewInInspector]
        [SerializeField]
        private VariableFloatBoundModifier _boundModifier; //FIXME: Nested Prefab時會有髒髒狀態？ 還是要Editor都寫GetComponent...?
        // [PreviewInInspector] [Component] [AutoChildren]
        // AbstractVariableModifier<float>[] _setOperations;

        // [Button]
        // void TestAdd(float value)
        // {
        //     Value += value;
        // }
        // public float Value => CurrentValue;

        public string ValueInfo => CurrentValue.ToString();
        public bool IsDrawingValueInfo => true;

        public override bool IsValueExist => CurrentValue != 0f;
    }
}