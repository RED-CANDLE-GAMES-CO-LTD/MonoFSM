using System;
using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    //把MonoBlackboard當成一個值提供者, 才可以被set到varMono上
    public class EntityAsValueProvider : MonoBehaviour, IValueProvider<MonoBlackboard>
    {
        [CompRef] [Auto] private IMonoEntityProvider _monoEntityProvider;

        public MonoBlackboard Value => _monoEntityProvider.monoEntity;

        public T GetValue<T>()
        {
            if (typeof(T) != typeof(MonoBlackboard))
                throw new InvalidOperationException("GetValue<T>() can only be used with MonoBlackboard type.");

            return (T)(object)_monoEntityProvider.monoEntity;
        }

        public Type ValueType => typeof(MonoBlackboard);
        public string Description => ToString();

        public override string ToString()
        {
#if UNITY_EDITOR
            _monoEntityProvider = GetComponent<IMonoEntityProvider>();
            if (_monoEntityProvider == null) return "";
#endif
            return _monoEntityProvider.Description + " as Value";
        }
        //Assign A to B vs 
    }
}