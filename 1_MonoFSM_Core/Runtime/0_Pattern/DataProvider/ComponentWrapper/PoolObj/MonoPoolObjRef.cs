using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataProvider.ComponentWrapper
{
    //DirectReference
    public class MonoPoolObjRef : MonoBehaviour, IMonoObjectProvider
    {
        [Required] public MonoPoolObj _monoPoolObj;
        public string Description => $"MonoPoolObj Reference: {_monoPoolObj.name}";

        public MonoPoolObj Get()
        {
            return _monoPoolObj;
        }
    }
}