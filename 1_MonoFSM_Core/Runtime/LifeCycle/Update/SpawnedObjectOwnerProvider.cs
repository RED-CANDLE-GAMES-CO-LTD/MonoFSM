using System;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSMCore.Runtime.LifeCycle
{
    public class SpawnedObjectOwnerProvider : MonoBehaviour, IBlackboardProvider, ICompProvider<MonoBlackboard>
    {
        [Required] [ShowInInspector] [AutoParent]
        private IMonoObjectProvider _monoObjectProvider; //我就是自己了...不行？

        [PreviewInInspector]
        public MonoBlackboard Blackboard
        {
            get
            {
                //editor time應該沒有ㄅ
#if UNITY_EDITOR
                if (Application.isPlaying == false)
                    _monoObjectProvider = GetComponentInParent<IMonoObjectProvider>(true);
#endif
                // return _monoObjectProvider.

                return _monoObjectProvider?.Get()?.GetComponent<MonoDescriptable>();
            }
        }

        public T GetComponentOfOwner<T>()
        {
            var owner = Blackboard;
            if (owner == null)
                return default;
            return owner.gameObject.GetComponent<T>();
        }

        public MonoBlackboard Get()
        {
            return Blackboard;
        }

        public object GetValue()
        {
            return Blackboard;
        }

        public Type ValueType => typeof(MonoBlackboard);

        public string Description => "SpawnedObjectProvider: " + _monoObjectProvider?.Get()?.name;
    }
}