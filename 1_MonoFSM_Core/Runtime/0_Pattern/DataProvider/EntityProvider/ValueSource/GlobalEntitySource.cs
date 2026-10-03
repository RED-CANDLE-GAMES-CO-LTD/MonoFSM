using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using MonoFSMCore.Runtime.LifeCycle;

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
        public override MonoEntity Value
        {
            get
            {
                //Edit mode 走原本那條（裡面會擋 !isPlaying），不要在 editor time 把 MonoObj cache 住
                if (!UnityEngine.Application.isPlaying)
                    return this.GetGlobalInstance(_entityTag);
                //所屬 MonoObj 不會在 runtime 換（source 跟著 prefab 走），cache 起來省每次 GetComponentInParent；
                //Destroy 後 Unity null 會重抓
                if (_monoObj == null)
                    _monoObj = GetComponentInParent<MonoObj>();
                return MonoDescriptableBinderExtension.GetGlobalInstanceFrom(_monoObj, _entityTag, this);
            }
        }

        [System.NonSerialized] private MonoObj _monoObj;
        public override MonoEntityTag entityTag => _entityTag;
    }
}
