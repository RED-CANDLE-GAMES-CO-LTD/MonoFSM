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
    /// <summary>
    /// 限制 VarInt 的最小最大值，比照 <see cref="VariableFloatBoundModifier"/> 的設計。
    /// SetOperation 採 inclusive clamp（與 VarFloat 對齊）；
    /// 對「index 循環」使用情境，Max 通常以 list.Count 設定，循環語意 [Min, Max) 由 IntMathAction 的 Cycle 模式自行處理。
    /// </summary>
    public class VariableIntBoundModifier : MonoBehaviour, AbstractVariableModifier<int>,
        IRestoreValueOverrider<int>, IUpdateSimulate, IResetStart
    {
        [PreviewInInspector]
        [AutoParent]
        VarInt _monoVar;

        [SerializeField]
        public VarIntWrapper _minValueWrapper;

        [SerializeField]
        public VarIntWrapper _maxValueWrapper;

        [ShowInInspector]
        public int MinValue => _minValueWrapper.Value;

        [ShowInInspector]
        public int MaxValue => _maxValueWrapper.Value != 0 ? _maxValueWrapper.Value : int.MaxValue;

        public UnityEvent OnMin;
        public UnityEvent OnMax;

        public int SetOperation(int value)
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

        public void EditorBoundCheck(ref int value)
        {
            if (value < MinValue)
                value = MinValue;

            if (value > MaxValue)
                value = MaxValue;
        }

        public int BeforeSetValueModifyCheck(int value, int currentValue) => SetOperation(value);

        public int AfterGetValueModifyCheck(int value) => value;

        #region 邊界變動時把當前值夾回範圍

        //Bound 只在 SetValue 時生效，Max/Min 自己被 modifier 改動時，VarInt 的當前值不會跟著收斂，
        //所以這裡 polling 邊界變化，一旦變動就把當前值重新 clamp。
        [GUIColor(0.6f, 0.8f, 1f)]
        [Tooltip("Min/Max 變動時（例如 Max 被 modifier 調小），把 VarInt 的當前值重新夾回範圍內")]
        public bool _isClampCurrentValueOnBoundChanged = true;

        [ShowInDebugMode]
        private bool _isBoundCached; //false: 尚未初始化，第一次 Simulate 一定會檢查一次

        [ShowInDebugMode]
        private int _lastMinValue;

        [ShowInDebugMode]
        private int _lastMaxValue;

        public void ResetStart()
        {
            //重置時強制在第一次 Simulate 重新檢查一次邊界
            _isBoundCached = false;
        }

        public void Simulate(float deltaTime)
        {
            if (!_isClampCurrentValueOnBoundChanged || _monoVar == null)
                return;

            var min = MinValue;
            var max = MaxValue;
            if (_isBoundCached && min == _lastMinValue && max == _lastMaxValue)
                return;

            _isBoundCached = true;
            _lastMinValue = min;
            _lastMaxValue = max;

            var current = _monoVar.CurrentValue;
            var clamped = Mathf.Clamp(current, min, max);
            if (clamped == current)
                return;

            _monoVar.SetValue(clamped, this, "BoundChanged");
        }

        #endregion

        [GUIColor(0.6f, 0.8f, 1f)]
        [FormerlySerializedAs("_isResetToMaxOnResetStart")]
        public bool _isResetToMaxOnRestore;

        [ShowInInspector] public bool ShouldOverrideRestoreValue => _isResetToMaxOnRestore;

        public int GetRestoreValue()
        {
            if (_isResetToMaxOnRestore)
                return MaxValue;

            return MinValue;
        }
    }
}
