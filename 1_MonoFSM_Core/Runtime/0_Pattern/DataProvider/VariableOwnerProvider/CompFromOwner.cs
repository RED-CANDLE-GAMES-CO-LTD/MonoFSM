using System;
using MonoFSM.Variable.Attributes;
using MonoFSM.Core;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    //倒著拿，好難思考
    //全部都用介面，更難了...一步步倒推，沒道理
    // [Obsolete]
    // public abstract class CompFromOwner<T> : MonoBehaviour, ICompProvider<T> where T : Component
    // {
    //     //FIXME: 還auto才能拿，悲劇QQ
    //     [CompRef][Auto] private IBlackboardProvider _ownerProvider;
    //
    //     public object GetValue()
    //     {
    //         return _ownerProvider.GetComponentOfOwner<T>();
    //     }
    //
    //     public T Get()
    //     {
    //         var component = _ownerProvider.GetComponentOfOwner<T>();
    //         if (component == null)
    //         {
    //             throw new InvalidOperationException($"Component of type {typeof(T).Name} not found in owner.");
    //         }
    //         return component;
    //     }
    //
    //
    //
    //     public Type ValueType => typeof(T);
    //
    //     public virtual string Description =>
    //         $"{typeof(T).Name} From Owner of {GetComponent<IBlackboardProvider>().Description}";
    // }
}