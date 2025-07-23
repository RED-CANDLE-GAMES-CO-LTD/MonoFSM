using System;
using MonoFSM.Core;
using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.FSM._3_FlagData
{
    [Obsolete("用datafunction?")]
    [CreateAssetMenu(menuName = "RCG/ItemData")]
    public class ItemData : DescriptableData, IItemData
    {
        [BoxGroup("物品")] [SerializeField] int slotStackCount = 1;
        public int MaxStackCount => slotStackCount;

        public virtual void Use() //FIXME: 怎麼吃更多類型、參數？ 搖桿操作？直接判 UI/Action?
        {
            //食物=> 吃
            //裝備=> 裝備
            //再DI一層
        }

        public virtual bool needInstance => false;

//FIXME:要把PoolObject拿過來嗎？
        [BoxGroup("物品")] [Required] public MonoPoolObj fsmPrefab;
        public override MonoPoolObj bindPrefab => fsmPrefab; //需要這個變數嗎...

        public MonoPoolObj InstantiateFsm(Transform parent)
        {
            return MyInstantiate(bindPrefab, parent);
        }

        protected T MyInstantiate<T>(T prefab, Transform parent) where T : Component
        {
            //這ㄌ
            //可以用async
            if (prefab == null)
            {
                Debug.LogError("prefab is null", this);
                return null;
            }

            //FIXME: 要先關起來...
            var instance = Instantiate(prefab, parent);
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(instance.gameObject, "InstantiateEquipView");
#endif
            //PoolManager.Instance.BorrowOrInstantiate(
            //FIXME: 這個auto比較慢...awake先做掉了...
            if (Application.isPlaying)
                AutoAttributeManager.AutoReferenceAllChildren(instance.gameObject);
            // PoolManager.PreparePoolObjectImplementation(instance.GetComponent<PoolObject>());
            return instance;
        }

        public DescriptableData Owner => this;
        public void SetOwner(DescriptableData owner)
        {
            
        }
    }
}