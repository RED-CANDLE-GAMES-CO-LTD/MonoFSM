using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    //EntityRef?
    public class GetEntity : MonoBehaviour, ICompProvider<MonoEntity>
    {
        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private ICompProvider _compProvider; //會拿到自己？

        public string Description => "Get " + Get().name;

        [ShowInPlayMode]
        public MonoEntity Get()
        {
            var comp = _compProvider.Get();
            return comp.GetComponentInParent<MonoEntity>();
        }

        // Component ICompProvider.Get()
        // {
        //     return Get();
        // }
    }
}