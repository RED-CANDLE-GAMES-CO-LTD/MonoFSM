using _1_MonoFSM_Core.Runtime.Attributes;
using MonoFSM.Variable;
using MonoFSM.Variable.FieldReference;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Variable
{
    public class VarMonoObj : GenericUnityObjectVariable<MonoObj>
    {
        //FIxME: 要區分Prefab和Runtime Object嗎？ 提示？
        [HideIf(nameof(HasProxySource))]
        // [SOConfig("10_Flags/GameData", useVarTagRestrictType: true)] //FIXME: 痾，只有SO類才需要ㄅ
        [Required]
        [PrefabFilter(typeof(PoolObject))] //hmm?
        // [ShowDrawerChain]
        [ShowInInspector]
        protected MonoObj DefaultValueSelector //優先順序？
        {
            get => _defaultValue;
            set => _defaultValue = value;
        }
    }
}
