using _1_MonoFSM_Core.Runtime.LifeCycle.Update;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.MonoData
{
    //modulepackfolder?
    public class MonoContext : MonoBehaviour, IParentEntityProvider
    {
        // [PreviewInInspector] [AutoChildren(DepthOneOnly = true)]
        // private MonoModulePack[] _modulePack;
        //
        // public MonoModulePack[] ModulePacks => _modulePack;
        [AutoParent] MonoEntity _parentEntity;
        public MonoEntity ParentEntity => _parentEntity;
    }
}
