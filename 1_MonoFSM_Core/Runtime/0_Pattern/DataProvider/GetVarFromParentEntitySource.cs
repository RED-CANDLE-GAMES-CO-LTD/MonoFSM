using System;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    /// <summary>
    ///     opt-in 的 value source：用 _varTag 從來源 entity 的 VariableFolder 取對應 variable 的值。
    ///     掛在一顆 Var 下，該 Var 就「重宣告」成 tag-mapping Var（走正規 _valueSources 路徑，
    ///     不像 _parentVarEntity proxy 會 cascade 整個 subtree）。
    ///     來源 entity：預設用 [AutoParent] 往上抓所屬 MonoEntity（in-hierarchy 零設定）；
    ///     若設了 _overrideSourceEntity 則改用它（注入場景，例如指向一顆從 VarListEntity 算出當前 item 的 VarEntity）。
    /// </summary>
    public class GetVarFromParentEntitySource : AbstractGetter, IValueProvider, IVariableProvider
    {
        [SOConfig("VariableType")] [Header("變數名稱")] [PropertyOrder(-1)]
        public VariableTag _varTag;

        // 預設來源：往上抓所屬 entity
        [ShowInInspector] [AutoParent]
        // [SerializeField]
        private MonoEntity _parentEntity;

        // 覆寫來源：注入場景用，指向要取值的那顆 entity
        [DropDownRef] [SerializeField] private VarEntity _overrideSourceEntity;

        [ShowInInspector]
        [Required]
        private MonoEntity SourceEntity =>
            _overrideSourceEntity != null ? _overrideSourceEntity.Value : _parentEntity;

        // 自我參照/遞迴防護：用 [AutoParent] 抓來源時，來源 entity 內註冊在同 _varTag 下的
        // 很可能就是擁有這顆 source 的那顆 Var → GetValue 會再繞回這裡 → 無限遞迴 → SOE 閃退。
        [NonSerialized] private bool _resolving;
        [ShowInInspector] AbstractMonoVariable mappingVar => SourceEntity?.GetVar(_varTag);

        public T1 Get<T1>()
        {
            var entity = SourceEntity;
            if (entity == null || _varTag == null)
                return default;
            var variable = entity.GetVar(_varTag);
            if (variable == null)
                return default;

            if (_resolving)
            {
                Debug.LogError(
                    $"[GetVarFromParentEntitySource] 偵測到自我參照：tag '{_varTag.name}' 在 " +
                    $"{entity.name} 解析回自己。請用 _overrideSourceEntity 指向不同的來源 entity，" +
                    "或確認該 entity 上同 tag 的變數不是這顆 Var 本身。",
                    this);
                return default;
            }

            _resolving = true;
            try
            {
                return variable.GetValue<T1>();
            }
            finally
            {
                _resolving = false;
            }
        }

        public Type ValueType => _varTag != null ? _varTag.ValueType : null;

        // IVariableProvider：讓「tag 指向一顆完整 Var（如 VarList）」的情況能被當成 proxy 解析，
        // 而不只是取值。VarRaw 只回傳變數參照、不呼叫 GetValue，因此不會觸發 _resolving 遞迴。
        public AbstractMonoVariable VarRaw => mappingVar;
        public bool IsVariableValid => mappingVar != null;
        public Type VariableType => mappingVar?.GetType();
        public TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable =>
            mappingVar as TVariable;

        public override bool HasValue =>
            SourceEntity != null && _varTag != null && SourceEntity.GetVar(_varTag) != null;

        public override string Description =>
            $"Get [{(_varTag != null ? _varTag.name : "?")}] from " +
            $"{(_overrideSourceEntity != null ? _overrideSourceEntity.name : "Parent Entity")}";
    }
}
