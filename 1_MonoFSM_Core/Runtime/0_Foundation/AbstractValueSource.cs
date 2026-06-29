using System.Collections.Generic;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.EditorExtension;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Foundation
{
    //寫的好美XD
    public abstract class AbstractGetter : AbstractDescriptionBehaviour //提供數值
    {
        protected override bool IsBracketsNeededForTag => false;
        protected override string DescriptionTag => "=>";

        [AutoNested]
        [InlineField]
        [PropertyOrder(1)]
        public ConditionGroup _conditionGroup;

        //TODO: 需要 hasValue嗎？ not null
        public virtual bool IsValid => _conditionGroup.IsValid && isActiveAndEnabled; //&& HasValue
        public abstract bool HasValue { get; }
    }

    //FIXME: 感覺還是很容易會繼承錯...
    public abstract class AbstractEntitySource
        : AbstractValueSource<MonoEntity>,
            IEntityValueProvider //只有我需要特別寫對吧？
    {
        //FIXME: 需要提供EntityTag!
        public abstract MonoEntityTag entityTag { get; }
    }

    public abstract class AbstractValueSource<T> : AbstractGetter, IValueProvider<T> //提供數值
    {
        protected override string DescriptionTag => "=>";

        [AutoParent]
        private MonoEntity _monoEntity;

        // public MonoEntity ParentEntity =>
        //     _monoEntity ? _monoEntity : _monoEntity = GetComponentInParent<MonoEntity>(true);

        [ShowInInspector]
        public abstract T Value { get; }

        public override bool HasValue =>
            !EqualityComparer<T>.Default.Equals(Value, default); //FIXME: float == 0 會被當成 null?

        public override string ValueInfo =>
            isActiveAndEnabled && HasValue ? Value?.ToString() : "Null"; //TODO: list太肥了
        public override bool IsDrawingValueInfo => true;
    }

    public static class ValueResolver
    {
        public static IValueProvider GetActiveValueSource(
            IValueProvider[] sources,
            MonoBehaviour context
        )
        {
            if (sources == null || sources.Length == 0)
                return null;

            if (!Application.isPlaying)
                return sources[0];

            foreach (var provider in sources)
                if (provider.IsValid)
                    return provider;

            // Debug.LogWarning("condition not met, use default? (last)" + sources[^1], context);
            return null;
        }

        public static bool HasValueProvider(IValueProvider[] sources)
        {
            return sources is { Length: > 0 };
        }
    }
}
