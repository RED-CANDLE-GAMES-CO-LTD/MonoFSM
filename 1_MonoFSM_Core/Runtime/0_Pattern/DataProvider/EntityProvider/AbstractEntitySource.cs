using _1_MonoFSM_Core.Runtime.Attributes;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.Runtime
{
    /// <summary>
    ///     FIXME: 繼承下面的
    ///     提供VariableOwner(可能會從一些奇怪的地方拿到), 必須要有HitDataProvider
    /// </summary>
    /// FIXME: 依照parent就能決定要用Dealer還是Receiver的Blackboard
    /// FIXME: 應該叫做EntityRef?
    /// 這個要自帶改名能力嗎？
    public abstract class AbstractEntitySource
        : AbstractValueSource<MonoEntity>,
            IEntityValueProvider
    {
        public virtual string SuggestDeclarationName => "";

        [Required]
        [FormerlySerializedAs("_monoEntityTag")]
        [HideIf(nameof(monoEntity))]
        [SOTypeDropdown(typeof(MonoEntityTag), true, "請選擇 MonoEntity 類型的 EntityTag")]
        public MonoEntityTag _expectedEntityTag; //這個是用來在Editor上顯示的，實際上會從HitDataProvider拿到

        [PreviewInInspector]
        public abstract MonoEntity monoEntity { get; }

        [PreviewInDebugMode]
        public MonoEntityTag entityTag => monoEntity?.DefaultTag ?? _expectedEntityTag;

        public T GetComponentOfOwner<T>() //好像有點白痴
        {
            var owner = monoEntity;
            if (owner == null)
                return default;
            return owner.GetComponent<T>();
        }

        [CompRef]
        [AutoChildren]
        private ValueProvider[] _valueProviders;

        // public abstract string NickName { get; }
        // public override string Description => $"{SuggestDeclarationName}({entityTag.name})";
        public override MonoEntity Value => monoEntity;
    }
}
