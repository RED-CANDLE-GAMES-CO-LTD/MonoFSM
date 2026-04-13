using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.EditorExtension;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.Variable
{
    //指向外部
    //需要再定義更細的class嗎？還是MonoDescriptable就夠了
    //最常用的Variable? MonoDescriptable下也會有MonoDescriptable
    //FIXME: 回到pool後，reference要清掉？還是是detector的責任？

    [FormerlyNamedAs("VarBlackboard")]
    public class VarEntity : GenericUnityObjectVariable<MonoEntity>
    {

        // [HideIf(nameof(HasValueProvider))]
        [FormerlySerializedAs("_MonoDescriptableTag")]
        [SOConfig("10_Flags/VarMono")]
        [BoxGroup("定義型別")]
        //FIXME: 這用了感覺就...沒彈性了？，限定schema如何？感覺在做差不多的事？[PropertyOrder(-1)]
        [SerializeField]
        private MonoEntityTag _monoEntityTag; //FIXME: Expected MonoEntityTag, but can be null?

        //FIXME: 好像會需要getter喔，從source來的話

        [PreviewInInspector]
        public MonoEntityTag EntityTag
        {
            get
            {
                var isProxy = HasValueSource;
                // Debug.Log("Get EntityTag from VarEntity isProxy:" + isProxy);
                if (isProxy && valueSource is IEntityValueProvider entityValueSource)
                    return entityValueSource.entityTag;
                // return valueSource.EntityTag; //hmm 怎麼額外定義QQ
                return _monoEntityTag;
            }
        }

        [PreviewInInspector]
        [AutoChildren(DepthOneOnly = true)]
        [CompRef]
        private IEntityValueProvider _entityValueSource;

        //         [BoxGroup("定義型別")]
        //         [PropertyOrder(-1)]
        //         [PreviewInInspector]
        //         public GameData SampleData
        // #if UNITY_EDITOR
        //             => _monoEntityTag ? _monoEntityTag.SamepleData : null;
        // #else
        //             => null;
        // #endif

        //FIXME: 要用T? VarComponent?

        //FIXME: 什麼意四？
        // [Header("預設值")] [SerializeField]
        // [DropDownRef(null, nameof(SiblingValueFilter))]
        // private MonoEntity _siblingDefaultValue;
        //
        // private Type SiblingValueFilter()
        // {
        //     if (_varTag == null)
        //         return typeof(MonoEntity);
        //     // Debug.Log("RestrictType is " + varTag._valueFilterType.RestrictType);
        //     return _varTag.ValueFilterType;
        // }

        //FIXME: 繼承時想要加更多attribute
        // [Header("預設值")] [HideIf(nameof(_siblingDefaultValue))] [SerializeField]
        // protected Component _defaultValue;


        // protected override MonoEntity DefaultValue => _defaultValue;


        // [Header("預設值")]
        // [DropDownRef]
        // [ShowInInspector]
        // MonoEntity SiblingDefaultValue
        // {
        //     set => _defaultValue = value;
        //     get => _defaultValue;
        // }

        // _siblingDefaultValue != null ? _siblingDefaultValue :

        //FIXME: 用Type更好嗎？
        // public override GameFlagBase FinalData => Value != null ? Value.Data : SampleData;

        //         public string IconName => "vcs_document";
        //         public bool IsDrawingIcon => true;
        //         //Fixme: 還是應該要外部登記比較好？
        // #if UNITY_EDITOR
        //         public Texture2D CustomIcon => UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.rcgmaker.fsm/RCGMakerFSMCore/Runtime/2_Variable/VarMonoIcon.png");
        // #endif
        // public string ValueInfo => Value != null ? Value.name : "null";
        // public bool IsDrawingValueInfo => true;

        // [Button]
        // private void AddEntityFromVarEntityProvider()
        // {
        //     this.AddChildrenComponent<EntityFromVarEntityProvider>("entityProvider");
        // }

        //來源需要強型別嗎？
    }
}
