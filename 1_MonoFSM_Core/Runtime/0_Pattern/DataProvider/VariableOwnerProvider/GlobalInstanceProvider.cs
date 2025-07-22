using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Variable;
using MonoFSM.Runtime.Mono;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    /// <summary>
    /// 提供 Global Instance 的 Provider，參考 ParentBlackboardProvider 的實作方式
    /// FIXME: 怎麼拿到目前player的Inventory? condition? runtime tag? (LocalPlayer,Inventory?) tuple key?
    /// </summary>
    public class GlobalInstanceProvider : MonoBehaviour, IMonoEntityProvider
    {
        [Required]
        [SerializeField] private MonoEntityTag _monoEntityTag;

        [PreviewInInspector] public MonoEntity monoEntity => GetBlackboardFromGlobalInstance();

        public MonoEntityTag entityTag => _monoEntityTag;

        private MonoEntity GetBlackboardFromGlobalInstance()
        {
            if (_monoEntityTag == null)
            {
                if (Application.isPlaying)
                    Debug.LogError("MonoEntityTag is null", this);
                return null;
            }

            var instance = this.GetGlobalInstance(_monoEntityTag);
            if (instance == null && Application.isPlaying)
            {
                Debug.LogError($"Global instance not found for tag: {_monoEntityTag.name}", this);
                return null;
            }

            // 如果 instance 是 MonoBlackboard，直接回傳
            // if (instance is MonoEntity blackboard)
            //     return blackboard;

            // 否則嘗試從 instance 取得 MonoBlackboard component
            return instance;
        }

        public T GetComponentOfOwner<T>()
        {
            var blackboard = GetBlackboardFromGlobalInstance();
            return blackboard != null ? blackboard.GetComponent<T>() : default(T);
        }
    }
}