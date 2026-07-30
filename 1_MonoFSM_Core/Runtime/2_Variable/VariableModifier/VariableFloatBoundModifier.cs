using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSMCore.Runtime.LifeCycle;
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
        T BeforeSetValueModifyCheck(T value, T beforeSetValue);
        T AfterGetValueModifyCheck(T value);
    }

    /// <summary>
    /// Function: 限制VariableFloat的最小最大值
    /// //FIXME: 直接塞兩個VarFloat比較對？
    /// FIXME: 直接把MinMax一鍵生成？
    /// </summary>
    public class VariableFloatBoundModifier : MonoBehaviour, AbstractVariableModifier<float>,
        IRestoreValueOverrider<float>, IUpdateSimulate, IResetStart
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

        public float BeforeSetValueModifyCheck(float value, float currentValue) =>
            SetOperation(value);

        public float AfterGetValueModifyCheck(float value) => value; //要再bound一次嗎？

        #region 邊界變動時把當前值夾回範圍

        //Bound 只在 SetValue 時生效，Max/Min 自己被 modifier 改動時，VarFloat 的當前值不會跟著收斂，
        //所以這裡 polling 邊界變化，一旦變動就把當前值重新 clamp。
        [GUIColor(0.6f, 0.8f, 1f)]
        [Tooltip("Min/Max 變動時（例如 Max 被 modifier 調小），把 VarFloat 的當前值重新夾回範圍內")]
        public bool _isClampCurrentValueOnBoundChanged = true;

        [ShowInDebugMode]
        private float _lastMinValue = float.NaN; //NaN: 尚未初始化，第一次 Simulate 一定會檢查一次

        [ShowInDebugMode]
        private float _lastMaxValue = float.NaN;

        public void ResetStart()
        {
            //重置時強制在第一次 Simulate 重新檢查一次邊界
            _lastMinValue = float.NaN;
            _lastMaxValue = float.NaN;
        }

        public void Simulate(float deltaTime)
        {
            if (!_isClampCurrentValueOnBoundChanged || _monoVar == null)
                return;

            var min = MinValue;
            var max = MaxValue;
            //用 == 而非 Approximately，避免 Infinity 相減變成 NaN 而每幀誤判成有變動
            if (min == _lastMinValue && max == _lastMaxValue)
                return;

            _lastMinValue = min;
            _lastMaxValue = max;

            var current = _monoVar.CurrentValue;
            var clamped = Mathf.Clamp(current, min, max);
            if (clamped == current)
                return;

            // Debug.Log(
            //     $"[BoundModifier] bound changed, clamp {current} → {clamped} (min={min}, max={max})",
            //     this);
            _monoVar.SetValue(clamped, this, "BoundChanged");
        }

        #endregion

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
