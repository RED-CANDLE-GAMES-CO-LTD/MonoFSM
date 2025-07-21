using System;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSMCore.Runtime.LifeCycle
{
    //FIXME: 好像很trivial, 不好懂
    public class SpawnedObjectOwnerProvider : MonoBehaviour, IMonoEntityProvider, ICompProvider<MonoEntity>
    {
        [Required] [ShowInInspector] [AutoParent]
        private IMonoObjectProvider _monoObjectProvider; //我就是自己了...不行？

        [PreviewInInspector]
        public MonoEntity monoEntity
        {
            get
            {
                //editor time應該沒有ㄅ
#if UNITY_EDITOR
                if (Application.isPlaying == false)
                    _monoObjectProvider = GetComponentInParent<IMonoObjectProvider>(true);
#endif
                // return _monoObjectProvider.

                return _monoObjectProvider?.Get()?.GetComponent<MonoEntity>();
            }
        }

        public MonoEntityTag entityTag => _monoObjectProvider?.Get()?.GetComponent<MonoEntityTag>();

        public T GetComponentOfOwner<T>()
        {
            var owner = monoEntity;
            if (owner == null)
                return default;
            return owner.gameObject.GetComponent<T>();
        }

        public MonoEntity Get()
        {
            return monoEntity;
        }

        public object GetValue()
        {
            return monoEntity;
        }

        public Type ValueType => typeof(MonoBlackboard);

        public string Description => "SpawnedObjectProvider: " + _monoObjectProvider?.Get()?.name;
    }
}