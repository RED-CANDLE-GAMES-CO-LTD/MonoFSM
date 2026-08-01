using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;

namespace MonoFSM.Core.Runtime
{
    public class GlobalEntitySource : Foundation.AbstractEntitySource
    {
        public override string Description => "Global: " + _entityTag?.name;
        public MonoEntityTag _entityTag;
        public override MonoEntity Value => this.GetGlobalInstance(_entityTag);
        public override MonoEntityTag entityTag => _entityTag;
    }
}
