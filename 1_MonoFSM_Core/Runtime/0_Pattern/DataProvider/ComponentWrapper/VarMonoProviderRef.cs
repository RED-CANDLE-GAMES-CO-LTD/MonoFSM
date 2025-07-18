using System;
using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;
using UnityEngine;

namespace MonoFSM.VarRefOld
{
    //GetVarMono?
    //FIXME: 好像不該用這個？ 自打架了？
    [Obsolete]
    public class VarMonoProviderRef : VariableProviderRef<VarEntity, MonoEntity> //, IVarMonoProvider
    {
        public DescriptableData SampleData => Variable.SampleData;

        public MonoBlackboard MonoBlackboard => Value;

        public T GetComponentOfOwner<T>()
        {
            if (Value == null)
            {
                Debug.LogError("VariableOwner is null, cannot get component.");
                return default;
            }

            return Value.GetComponent<T>();
        }
    }

    //Owner.Var (也指到另一個Owner的話...?) 本質還是component沒有順序性，設計起來太卡...好想解決QQ
    //add to children, 一路回推, 
}