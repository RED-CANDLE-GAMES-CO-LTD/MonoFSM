using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.TypeTag;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable
{


    //variable
    //Monobehaviour 包著一個變數
    //需要Generic嗎...好像算了
    //沒用的東西！
    //FIXME: 怎麼表達Required / Optional 的variable?
    public class VarComp : GenericUnityObjectVariable<Component>
    {
        //FIXME: typeRestrict

        [Header("類型限制")]
        [SerializeField]
        [Tooltip("限定 Component 的類型")]
        [SOConfig("TypeTag")]
        private CompTypeTag _componentTypeTag;

        //FIXME: isConst時要Required? 怎麼在 AbstractDescriptionBehaviour 檢查？
        [HideIf(nameof(HasParentVarEntity))]
        [Header("預設值")]
        [ShowInInspector]
        [DropDownRef(null, nameof(SiblingValueFilter))]
        private Component SiblingDefaultValue
        {
            set => _defaultValue = value;
            get => _defaultValue;
        } //用property?

        public override Type ValueType => SiblingValueFilter() ?? Value?.GetType();

        protected override bool HasDefaultValueError()
        {
            if (_componentTypeTag == null || _componentTypeTag.Type == null || _defaultValue == null)
                return false;
            var restrictType = _componentTypeTag.Type;
            var actualType = _defaultValue.GetType();
            return !restrictType.IsAssignableFrom(actualType);
        }

        protected override string DefaultValueErrorMessage()
        {
            if (_componentTypeTag == null || _componentTypeTag.Type == null || _defaultValue == null)
                return string.Empty;
            var restrictType = _componentTypeTag.Type;
            var actualType = _defaultValue.GetType();
            return $"型別不匹配: {actualType.Name} 不符合 {_componentTypeTag.name} 限制的 {restrictType.Name}";
        }

        //可以篩選type, 就是宣告
        Type SiblingValueFilter()
        {
            // 優先使用 MonoTypeTag 的類型限制
            if (_componentTypeTag != null && _componentTypeTag.Type != null)
                return _componentTypeTag.Type;

            // 退回到使用 _varTag 的類型限制
            if (_varTag == null)
                return typeof(Component);
            // Debug.Log("RestrictType is " + _varTag.ValueFilterType, _varTag);
            return _varTag.ValueFilterType;
        }

        protected override bool HasError()
        {
            // 檢查 _componentTypeTag 的型別限制
            if (_componentTypeTag != null && _componentTypeTag.Type != null &&
                _defaultValue != null)
            {
                var restrictType = _componentTypeTag.Type;
                var actualType = _defaultValue.GetType();
                if (!restrictType.IsAssignableFrom(actualType))
                {
                    _errorMessage =
                        $"型別不匹配: _defaultValue 型別為 {actualType.Name}，但 CompTypeTag '{_componentTypeTag.name}' 限制型別為 {restrictType.Name}";
                    return true;
                }
            }

            return base.HasError();
        }
    }
}
