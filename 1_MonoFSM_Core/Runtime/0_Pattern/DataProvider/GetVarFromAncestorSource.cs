using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    /// <summary>
    ///     opt-in 的 value source：沿 transform 階層往上找第一個持有 _varTag 的 VariableFolder，
    ///     用那顆變數當這顆 Var 的值來源。掛在一顆 Var 下即可（被 _valueSources 撿走）。
    ///     跟 GetVarFromParentEntitySource 的差別：
    ///     後者只看 [AutoParent] 抓到的「最近一顆 MonoEntity」，這個會一路往上走，
    ///     而且提供方只要掛 VariableFolder 就好、不需要是 MonoEntity —— 想放哪一層就放哪一層。
    ///     語意是「往上找設定，找不到就用自己的 local 值」，所以找不到不是錯誤：
    ///     IsValid 回 false 讓 ValueResolver 跳過這個 source、fallback 回 Var 的 local field。
    ///     最近的祖先優先，所以外層設全域預設、內層可以再蓋掉。
    /// </summary>
    public class GetVarFromAncestorSource : AbstractGetter, IValueProvider, IVariableProvider
    {
        [SOConfig("VariableType")]
        [Header("變數名稱")]
        [PropertyOrder(-1)]
        public VariableTag _varTag;

        //自己這層的 VariableFolder 一定要跳過：這顆 source 的擁有者（同 tag 的 Var）就在裡面，
        //不跳過就會解析回自己 → 無限遞迴。
        private VariableFolder OwnFolder => GetComponentInParent<VariableFolder>(true);

        [NonSerialized] private AbstractMonoVariable _cached;

        //階層固定，play mode 下 cache 起來避免每幀走 chain；edit mode 不 cache，改 tag 才會即時反映。
        private AbstractMonoVariable Resolved
        {
            get
            {
                if (Application.isPlaying && _cached != null)
                    return _cached;
                var found = FindFromAncestor();
                if (Application.isPlaying)
                    _cached = found;
                return found;
            }
        }

        private AbstractMonoVariable FindFromAncestor()
        {
            if (_varTag == null)
                return null;

            var ownFolder = OwnFolder;
            //用 transform 往上走而不是 GetComponentsInParent：零 GC，而且不受 GameObject active 影響
            //（起火點這類節點會被 culling 關掉）
            var t = ownFolder != null ? ownFolder.transform.parent : transform.parent;
            for (; t != null; t = t.parent)
                if (t.TryGetComponent<VariableFolder>(out var folder))
                {
                    var v = folder.GetVariable(_varTag);
                    if (v != null)
                        return v;
                }

            return null;
        }

        //點得進去、看得到解析到哪一層，不然隱式往上找會很難除錯
        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(10)]
        [LabelText("解析到")]
        private AbstractMonoVariable ResolvedVar => Resolved;

        public T1 Get<T1>()
        {
            var variable = Resolved;
            if (variable == null)
                return default;
            return variable.GetValue<T1>();
        }

        public Type ValueType => _varTag != null ? _varTag.ValueType : null;

        //找不到就讓 ValueResolver 跳過這個 source，落回 Var 自己的 local 值（不是錯誤狀態，不噴 log）
        public override bool IsValid => base.IsValid && Resolved != null;
        public override bool HasValue => Resolved != null;

        //IVariableProvider：讓 tag 指到一顆完整 Var（如 VarList）時能被當 proxy 解析，而不只是取值
        public AbstractMonoVariable VarRaw => Resolved;
        public bool IsVariableValid => Resolved != null;
        public Type VariableType => Resolved?.GetType();

        public TVariable GetVar<TVariable>()
            where TVariable : AbstractMonoVariable => Resolved as TVariable;

        public override string Description =>
            $"Get [{(_varTag != null ? _varTag.name : "?")}] from Ancestor";
    }
}
