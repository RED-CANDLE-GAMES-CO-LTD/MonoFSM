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
    /// <summary>
    /// 限制 VarInt 的最小最大值，比照 <see cref="VariableFloatBoundModifier"/> 的設計。
    /// SetOperation 採 inclusive clamp（與 VarFloat 對齊）；
    /// 對「index 循環」使用情境，Max 通常以 list.Count 設定，循環語意 [Min, Max) 由 IntMathAction 的 Cycle 模式自行處理。
    /// </summary>
    public class VariableIntBoundModifier : MonoBehaviour, AbstractVariableModifier<int>,
        IRestoreValueOverrider<int>
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
