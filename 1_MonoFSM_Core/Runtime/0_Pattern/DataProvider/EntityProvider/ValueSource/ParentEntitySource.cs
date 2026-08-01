using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    public class ParentEntitySource : Foundation.AbstractEntitySource
    {
        [AutoParent]
        [SerializeField]
        private MonoEntity _parentEntity;
        public override MonoEntity Value => _parentEntity;
        public override MonoEntityTag entityTag => _parentEntity ? _parentEntity.DefaultTag : null;
    }
}
