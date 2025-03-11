using System;
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    //variable
    //Monobehaviour 包著一個變數
    //需要Generic嗎...好像算了
    //沒用的東西！
    public class ComponentVariable : GenericUnityObjectVariable<MonoBehaviour>
    {
        [FormerlySerializedAs("_siblingValue")]
        [Header("預設值")]
        [SerializeField]
        [DropDownRef(null, nameof(SiblingValueFilter))]
        private MonoBehaviour _siblingDefaultValue;

        Type SiblingValueFilter()
        {
            if (varTag == null)
                return typeof(MonoBehaviour);
            // Debug.Log("RestrictType is " + varTag._valueFilterType.RestrictType);
            return varTag._valueFilterType.RestrictType;
        }

        //FIXME: 繼承時想要加更多attribute
        // [Header("預設值")] [HideIf(nameof(_siblingDefaultValue))] [SerializeField]
        // protected Component _defaultValue;


        protected override MonoBehaviour DefaultValue =>
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
        public override GameFlagBase FinalData => null;
    }
}