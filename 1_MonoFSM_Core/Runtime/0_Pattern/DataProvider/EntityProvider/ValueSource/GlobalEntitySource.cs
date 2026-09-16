using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;

namespace MonoFSM.Core.Runtime
{
    /// <summary>
    ///     從全域 registry 取出指定 MonoEntityTag 註冊的那一顆唯一 entity（玩家、火車頭這類單例）。
    ///     registry 是 tag → 單一 entity 的 dict，同 tag 有多個實體時取不到，要找多顆得自己探測。
    /// </summary>
    public class GlobalEntitySource : Foundation.AbstractEntitySource
    {
        public override string Description => "Global: " + _entityTag?.name;
        public MonoEntityTag _entityTag;
        public override MonoEntity Value => this.GetGlobalInstance(_entityTag);
        public override MonoEntityTag entityTag => _entityTag;
    }
}
