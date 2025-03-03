using System;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.Mono;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables
{
    //最常用的Variable? MonoDescriptable下也會有MonoDescriptable
    public class VarMono : GenericUnityObjectVariable<MonoDescriptable>
    {
        //FIXME: 還能做型別限制、檢查嗎？
        //MonoSchema?
        [SOConfig("10_Flags/VarMono")] [BoxGroup("定義型別")] [PropertyOrder(-1)]
        public MonoDescriptableTag _MonoDescriptableTag; //Class Name?

        [BoxGroup("定義型別")]
        [PropertyOrder(-1)]
        [PreviewInInspector]
        public DescriptableData SampleData => _MonoDescriptableTag ? _MonoDescriptableTag.SamepleData : null;

        //FIXME: 要用T? VarComponent?

        [Header("預設值")] [SerializeField] [DropDownRef(null, nameof(SiblingValueFilter))]
        private MonoDescriptable _siblingDefaultValue;

        Type SiblingValueFilter()
        {
            if (varTag == null)
                return typeof(MonoDescriptable);
            // Debug.Log("RestrictType is " + varTag._valueFilterType.RestrictType);
            return varTag._valueFilterType.RestrictType;
        }

        //FIXME: 繼承時想要加更多attribute
        // [Header("預設值")] [HideIf(nameof(_siblingDefaultValue))] [SerializeField]
        // protected Component _defaultValue;


        protected override MonoDescriptable DefaultValue =>
            _siblingDefaultValue != null ? _siblingDefaultValue : _defaultValue;
        //FIXME: 用Type更好嗎？
    }
}