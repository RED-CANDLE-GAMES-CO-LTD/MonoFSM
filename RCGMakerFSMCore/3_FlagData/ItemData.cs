using RCGMaker.Core;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._3_FlagData
{
    [CreateAssetMenu(menuName = "RCG/ItemData")]
    public class ItemData : DescriptableData, IItem
    {
        [BoxGroup("物品")] [SerializeField] int slotStackCount = 1;
        public int SlotStackCount => slotStackCount;

        public virtual void Use() //FIXME: 怎麼吃更多類型、參數？ 搖桿操作？直接判 UI/Action?
        {
            //食物=> 吃
            //裝備=> 裝備
            //再DI一層
        }

        public virtual bool needInstance => false;

//FIXME:要把PoolObject拿過來嗎？
        [BoxGroup("物品")] [Required] public Component fsmPrefab;
        public override Component bindPrefab => fsmPrefab; //需要這個變數嗎...

        public Component InstantiateFsm(Transform parent)
        {
            return MyInstantiate(bindPrefab, parent);
        }

        protected T MyInstantiate<T>(T prefab, Transform parent) where T : Component
        {
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
    }
}