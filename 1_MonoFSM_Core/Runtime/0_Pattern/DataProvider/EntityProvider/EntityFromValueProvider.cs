using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Attributes;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    // [Obsolete]
    //EntityRefFromValueProvider
    //FIXME: 不該用這個？用VarEntity?
    public class EntityFromValueProvider : AbstractEntitySource
    {
        public override string SuggestDeclarationName => "entity:";

        public override MonoEntity monoEntity =>
            _monoEntityProvider != null ? _monoEntityProvider.Get<MonoEntity>() : null;

        [ValueTypeValidate(typeof(MonoEntity))]
        [SerializeField]
        [DropDownRef]
        private ValueProvider _monoEntityProvider;
    }
}
