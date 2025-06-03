using UnityEngine;

using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;

//CountdownTimer...直接掛在這個下面？
namespace MonoFSM.Variable
{
    /// <summary>
    /// A MonoBehaviour representation of a float variable that can be bound to scriptable data.
    /// This class provides functionality for float values that can be accessed, modified, and tracked
    /// across the application.
    /// </summary>
    public class VarFloat : GenericMonoVariable<GameDataFloat, FlagFieldFloat, float>, ISerializedFloatValue
    {
        //FIXME: 需要一個reset value source? 回到maxValue or minValue之類的...? 
        public override GameFlagBase FinalData => BindData;

        // public VariableTag Key => _varTag;
        public int IntValue => Mathf.CeilToInt(CurrentValue);
        public float Percentage => (CurrentValue - Min) / (Max - Min);
        public float Min => _boundModifier.MinValue;
        public float Max => _boundModifier.MaxValue;

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

        public bool IsDecreasing => CurrentValue < LastValue;
        public bool IsIncreasing => CurrentValue > LastValue;
        
        [AutoChildren(false)] [PreviewInInspector]
        private VariableFloatBoundModifier _boundModifier;
        // [PreviewInInspector] [Component] [AutoChildren]
        // AbstractVariableModifier<float>[] _setOperations;

        [Button]
        void TestAdd(float value)
        {
            Value += value;
        }
        // public float Value => CurrentValue;
  
    }
}