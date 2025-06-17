using System;

using UnityEngine;
using UnityEngine.Events;

using Sirenix.OdinInspector;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;

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
    /// </summary>
    public class VariableFloatBoundModifier : MonoBehaviour, AbstractVariableModifier<float>
    {
        [PreviewInInspector] [AutoParent] VarFloat _monoVar;

        // [Auto] VariableFloat variable;
        // [HideIf(nameof(MinVar))] public float min = 0;
        //
        // [HideIf(nameof(MaxVar))] public float max = 1;
        //
        // //ex: 血量
        // //這會不會很麻煩每次都要設定？
        //
        // [DropDownRef] [SerializeField] VarFloat MinVar;
        // [DropDownRef] [SerializeField] VarFloat MaxVar; //好像應該用繼承的
        
        //FIXME: simple bound怎麼設計？
        [Component] [AutoChildren] IFloatProvider[] _floatProviderArray = Array.Empty<IFloatProvider>();
        [PreviewInInspector] [Component] IFloatProvider _minValueProvider => _floatProviderArray.Length > 0 ? _floatProviderArray[0] : null;
        [PreviewInInspector] [Component] IFloatProvider _maxValueProvider => _floatProviderArray.Length > 1 ? _floatProviderArray[1] : null;

        //FIXME: Editor time沒有...哭了
        
        [ShowInInspector]
        public float MinValue =>
            _minValueProvider?.Value ?? Mathf.NegativeInfinity; //MaxVar != null ? MaxVar.CurrentValue : max;

        [ShowInInspector]
        public float MaxValue => _maxValueProvider?.Value ?? Mathf.Infinity; //MinVar != null ? MinVar.CurrentValue : min;

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
            if (_floatProviderArray == null || _floatProviderArray.Length == 0)
            {
                _floatProviderArray = GetComponents<IFloatProvider>(); //FIXME 好煩喔，editor code還是需要自己寫
                return;
            }

            if (value < MinValue) value = MinValue;

            if (value > MaxValue) value = MaxValue;
        }

        public float BeforeSetValueModifyCheck(float value) 
            => SetOperation(value);

        public float AfterGetValueModifyCheck(float value) 
            => value; //要再bound一次嗎？
    }
}
