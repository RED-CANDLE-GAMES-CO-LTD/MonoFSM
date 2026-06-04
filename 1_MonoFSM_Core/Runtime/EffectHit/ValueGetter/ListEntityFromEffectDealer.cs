using System.Collections.Generic;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.EffectHit.ValueGetter
{
    /// <summary>
    ///     目前被這個Dealer偵測到的Receiver的Entity (list)
    /// </summary>
    public class ListEntityFromEffectDealer : AbstractValueSource<List<MonoEntity>>
    {
        [SerializeField]
        [DropDownRef]
        private GeneralEffectDealer _effectDealer;

        public override string Description =>
            _effectDealer != null
                ? $"Get hitting entities from {_effectDealer.name}"
                : "No EffectDealer";

        public override string ValueInfo => Value?.Count.ToString();

        protected override string DescriptionTag => "GetList";

        public override List<MonoEntity> Value =>
            _effectDealer != null ? _effectDealer.GetHittingEntities() : null;

        //FIXME: 對方關著要能看出來？
    }
}
