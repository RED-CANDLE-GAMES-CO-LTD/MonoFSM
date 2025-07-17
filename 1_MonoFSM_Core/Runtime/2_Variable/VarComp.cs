using System;

using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    //variable
    //Monobehaviour 包著一個變數
    //需要Generic嗎...好像算了
    //沒用的東西！
    public class VarComp : GenericUnityObjectVariable<Component>
    {
        [FormerlySerializedAs("_siblingValue")]
        [Header("預設值")]
        [SerializeField]
        [DropDownRef(null, nameof(SiblingValueFilter))]
        private Component _siblingDefaultValue;

        [SerializeField] private Type _type;
        Type SiblingValueFilter()
        {
            if (_varTag == null)
                return typeof(Component);
            // Debug.Log("RestrictType is " + _varTag._valueFilterType.RestrictType);
            return _varTag.ValueFilterType;
        }

        //FIXME: 繼承時想要加更多attribute
        // [Header("預設值")] [HideIf(nameof(_siblingDefaultValue))] [SerializeField]
        // protected Component _defaultValue;


        protected override Component DefaultValue =>
            _siblingDefaultValue != null ? _siblingDefaultValue : _defaultValue;
        //FIXME: 把Variable直接丟到該模組上就好？
        // IEnumerable<Component> _filter()
        // {
        //     // var type = serializedType.Value;
        //     // if(type == null)
        //     //     return null;
        //     // return this.GetComponentsOfSibling<LevelRunner>(type);
        // }

        // [ValueDropdown(nameof(_filter))]
        // [SerializeField]
        // private Component DefaultValue;

        // [TypeDrawerSettings(BaseType = typeof(Component)), ShowInInspector]
        // public Type type; //FIXME: 要用string 回推 type?
        // public override GameFlagBase FinalData => null;
    }
}