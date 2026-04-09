using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Variable
{
    //get operation?
    public interface IVariableFloatOperation // //乘區  好像不是variable, 應該是 Effect的FinalValue Calculation
    {
        float ApplyOperation(float value);
    }

    public interface IVariableFloatSetOperation //很確定是set variable時的operation
    {
        float SetOperation(float value);
    }

    public interface AbstractVariableModifier<T>
    {
        T BeforeSetValueModifyCheck(T value);
        T AfterGetValueModifyCheck(T value);
    }

    /// <summary>
    /// Function: 限制VariableFloat的最小最大值
    /// //FIXME: 直接塞兩個VarFloat比較對？
    /// FIXME: 直接把MinMax一鍵生成？
    /// </summary>
    public class VariableFloatBoundModifier : MonoBehaviour, AbstractVariableModifier<float>,
        IRestoreValueOverrider<float>
    {
        [PreviewInInspector]
        [AutoParent]

        VarFloat _monoVar;

        private void Awake()
        {
            // if (_minValueWrapper._var == null && _maxValueWrapper._var == null)
            //     Debug.LogError("VariableFloatBoundModifier has no min/max value set", this);
        }

        [SerializeField]
        public VarFloatWrapper _minValueWrapper;

        [SerializeField]
        public VarFloatWrapper _maxValueWrapper;

        // 保留舊欄位以相容既有 serialize data，待 migration 後可移除
        [HideInInspector]
        [SerializeField]
        private VarFloat _minValue;

        [HideInInspector]
        [SerializeField]
        private VarFloat _maxValue;

        [ShowInInspector]
        public float MinValue => _minValueWrapper._var != null ? _minValueWrapper.Value : 0;

        [ShowInInspector]
        public float MaxValue => _maxValueWrapper._var != null ? _maxValueWrapper.Value : Mathf.Infinity;

        public float Percentage => (_monoVar.CurrentValue - MinValue) / (MaxValue - MinValue);

        public UnityEvent OnMin;
        public UnityEvent OnMax;

        public float SetOperation(float value)
        {
            if (value < MinValue)
            {
                value = MinValue;
                OnMin?.Invoke();
            }

            if (value > MaxValue)
            {
                value = MaxValue;
                OnMax?.Invoke();
            }

            return value;
        }

        public void EditorBoundCheck(ref float value)
        {
            // if (_floatProviderArray == null || _floatProviderArray.Length == 0)
            // {
            //     _floatProviderArray = GetComponents<AbstractValueProvider<float>>(); //FIXME 好煩喔，editor code還是需要自己寫
            //     return;
            // }

            if (value < MinValue)
                value = MinValue;

            if (value > MaxValue)
                value = MaxValue;
        }

        public float BeforeSetValueModifyCheck(float value) => SetOperation(value);

        public float AfterGetValueModifyCheck(float value) => value; //要再bound一次嗎？

#if UNITY_EDITOR
        private void OnValidate()
        {
            bool dirty = false;
            if (_minValue != null && _minValueWrapper._var == null)
            {
                _minValueWrapper._var = _minValue;
                dirty = true;
            }
            if (_maxValue != null && _maxValueWrapper._var == null)
            {
                _maxValueWrapper._var = _maxValue;
                dirty = true;
            }
            if (dirty)
                EditorUtility.SetDirty(this);
        }
#endif

        [GUIColor(0.6f, 0.8f, 1f)]
        [FormerlySerializedAs("_isResetToMaxOnResetStart")]
        public bool _isResetToMaxOnRestore;

        // IRestoreValueOverrider<float> implementation
        [ShowInInspector] public bool ShouldOverrideRestoreValue => _isResetToMaxOnRestore;

        /// <summary>
        /// 直接取得 Field.ProductionValue，避免順序問題（不依賴 CurrentValue）
        /// </summary>
        public float GetRestoreValue()
        {
            //FIXME: maxValue還沒reset耶...
            if (_isResetToMaxOnRestore)
            {
                // Debug.Log("_maxValue.Field.ProductionValue" + _maxValue.Field.ProductionValue,
                //     this);
                return _maxValueWrapper._var != null ? _maxValueWrapper.Value : Mathf.Infinity;
            }

            return _minValueWrapper._var != null ? _minValueWrapper.Value : 0;
        }
    }
}
