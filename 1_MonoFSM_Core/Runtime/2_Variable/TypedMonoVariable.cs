using MonoFSM.Core;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable
{
    public abstract class TypedMonoVariable<T> : AbstractMonoVariable //, ISettable<T>
    {
        protected override bool HasError()
        {
            return base.HasError() || IsNeedValueSourceButNone();
        }

        bool IsNeedValueSourceButNone()
        {
            return (_needValueSource && valueSource == null);
        }

        [SerializeField] bool _needValueSource = false; //用comp?

        [InfoBox("需要一個ValueProvider來提供數值", InfoMessageType.Error,
            VisibleIf = nameof(IsNeedValueSourceButNone))]
        [CompRef]
        [AutoChildren(DepthOneOnly = true, _isSelfInclude = true)]
        protected IValueProvider[] _valueSources;

        protected IValueProvider valueSource => GetActiveValueSource();

        //hierarchy會不知道要撈？還是開prefab時都先撈一下？
        protected IValueProvider GetActiveValueSource()
        {
            AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_valueSources));
            return ValueResolver.GetActiveValueSource(_valueSources, this);
        }

        protected override bool HasValueSource
        {
            get
            {
                // Debug.Log("Check HasValueProvider in TypedMonoVariable");
                AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_valueSources));
                // Debug.Log("_valueSources.length" + _valueSources.Length);
                return ValueResolver.HasValueProvider(_valueSources); // || base.HasValueSource;
            }
        }

        public override string Description =>
            HasValueSource ? valueSource.Description : base.Description;

        public abstract void CommitValue();
    }
}
