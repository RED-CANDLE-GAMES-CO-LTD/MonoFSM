using RCGMaker.Core;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._3_FlagData
{
    public class ItemData:DescriptableData, IItem
    {
        [SerializeField]
         int slotStackCount = 1;
        public int SlotStackCount => slotStackCount;
        public virtual void Use() //FIXME: 怎麼吃更多類型、參數？ 搖桿操作？直接判 UI/Action?
        {
            //食物=> 吃
            //裝備=> 裝備
            //再DI一層
        }

        public virtual bool needInstance => false;
        public PoolObject fsmPrefab;
        public override PoolObject bindPrefab => fsmPrefab; //需要這個變數嗎...

        public PoolObject InstantiateFsm(Transform parent)
        {
            return MyInstantiate(bindPrefab,parent );
        }
        
        protected T MyInstantiate<T>(T prefab, Transform parent) where T:MonoBehaviour
        {
            
            //可以用async
            var instance = Instantiate(prefab, parent);
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(instance.gameObject, "InstantiateEquipView");
#endif
            //PoolManager.Instance.BorrowOrInstantiate(
            if(Application.isPlaying)
                AutoAttributeManager.AutoReferenceAllChildren(instance.gameObject);
            return instance;
        }
    }
}