using System;
using _1_MonoFSM_Core.Runtime.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.LifeCycle.Update
{
    // [Obsolete] //獨立一個節點很難找，直接放context下就好，要掛上去？有什麼幫助嗎？好像也不需要
    public class MonoModuleFolder : MonoBehaviour
    {
        [ShowInInspector]
        // [PrefabFilter(typeof(MonoModulePack))]
        [AutoChildren(DepthOneOnly = true)]
        private MonoModulePack[] _modulePack;

        public MonoModulePack[] ModulePacks => _modulePack;
    }
}
