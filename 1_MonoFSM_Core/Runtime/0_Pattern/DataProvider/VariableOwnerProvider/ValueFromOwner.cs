using MonoFSM.Variable.Attributes;
using RCGMaker.Core;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    public class ValueFromOwner<T> : MonoBehaviour, IValueProvider
    {
        //FIXME: 還auto才能拿，悲劇QQ
        [CompRef] [Auto] private IVariableOwnerProvider _ownerProvider;

        public object GetValue()
        {
            return _ownerProvider.GetComponentOfOwner<T>();
        }

        public virtual string Description =>
            $"{typeof(T).Name} From Owner of {GetComponent<IVariableOwnerProvider>().Description}";
    }
}