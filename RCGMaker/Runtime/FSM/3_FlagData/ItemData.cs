using RCGMaker.Core;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._3_FlagData
{
    public class ItemData:GameFlagDescriptable, IItem
    {
        [SerializeField]
         int slotStackCount = 1;
        public int SlotStackCount => slotStackCount;
        public virtual void Use()
        {
            //食物=> 吃
            //裝備=> 裝備
            //再DI一層
        }

        public virtual bool needInstance => false;
        public PoolObject fsmPrefab;
        public override PoolObject bindPrefab => fsmPrefab; //需要這個變數嗎...

        public PoolObject InstantiateFsm()
        {
            //FIXME: 要放在level runner下面？動態的fsm要怎麼處理？
            return MyInstantiate(bindPrefab, StateMachineManager.Instance.transform);
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