using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Mono;
using RCGExtension;
using MonoFSM.Variable;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.Variable
{
    //指向外部
    //需要再定義更細的class嗎？還是MonoDescriptable就夠了
    //最常用的Variable? MonoDescriptable下也會有MonoDescriptable
    //FIXME: 還是叫別的吧 VarMono?好像還是比較好？
    [FormerlyNamedAs("VarBlackboard")]
    public class VarEntity : GenericUnityObjectVariable<MonoEntity>, IHierarchyValueInfo
    {
        //FIXME: 還能做型別限制、檢查嗎？
        //MonoSchema?
        [FormerlySerializedAs("_MonoDescriptableTag")]
        [SOConfig("10_Flags/VarMono")]
        [BoxGroup("定義型別")]
        [PropertyOrder(-1)]
        public MonoEntityTag _monoEntityTag; //Class Name?

        [BoxGroup("定義型別")]
        [PropertyOrder(-1)]
        [PreviewInInspector]
        public DescriptableData SampleData 
#if UNITY_EDITOR
            => _monoEntityTag
                ? _monoEntityTag.SamepleData 
                : null;
#else
            => null;
#endif

        //FIXME: 要用T? VarComponent?

        //FIXME: 什麼意四？
        [Header("預設值")] [SerializeField] [DropDownRef(null, nameof(SiblingValueFilter))]
        private MonoEntity _siblingDefaultValue;

        private Type SiblingValueFilter()
        {
            if (_varTag == null)
                return typeof(MonoEntity);
            // Debug.Log("RestrictType is " + varTag._valueFilterType.RestrictType);
            return _varTag.ValueFilterType;
        }

        //FIXME: 繼承時想要加更多attribute
        // [Header("預設值")] [HideIf(nameof(_siblingDefaultValue))] [SerializeField]
        // protected Component _defaultValue;


        protected override MonoEntity DefaultValue =>
            _siblingDefaultValue != null ? _siblingDefaultValue : _defaultValue;

        //FIXME: 用Type更好嗎？
        // public override GameFlagBase FinalData => Value != null ? Value.Data : SampleData;

//         public string IconName => "vcs_document";
//         public bool IsDrawingIcon => true;
//         //Fixme: 還是應該要外部登記比較好？
// #if UNITY_EDITOR
//         public Texture2D CustomIcon => UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.rcgmaker.fsm/RCGMakerFSMCore/Runtime/2_Variable/VarMonoIcon.png");
// #endif
        public string ValueInfo => Value?.name ?? "null";
        public bool IsDrawingValueInfo => true;
    }
}