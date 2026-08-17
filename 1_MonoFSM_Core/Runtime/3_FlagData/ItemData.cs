using System;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Runtime.FSM._3_FlagData
{
    [Obsolete("用datafunction?")]
    [CreateAssetMenu(menuName = "RCG/ItemData")]
    public class ItemData : GameData, IItemData
    {
        [BoxGroup("物品")] [SerializeField] int slotStackCount = 1;
        public float MaxStackCount => slotStackCount;

        public virtual void Use() //FIXME: 怎麼吃更多類型、參數？ 搖桿操作？直接判 UI/Action?
        {
            //食物=> 吃
            //裝備=> 裝備
            //再DI一層
        }

        public virtual bool needInstance => false;

//FIXME:要把PoolObject拿過來嗎？
        //fsmPrefab 已併入 GameData._bindPrefab（一級欄位 + _baseConfig 疊層繼承）

        public MonoObj InstantiateFsm(Transform parent)
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
            Undo.RegisterCreatedObjectUndo(instance.gameObject, "InstantiateEquipView");
#endif
            //PoolManager.Instance.BorrowOrInstantiate(
            //FIXME: 這個auto比較慢...awake先做掉了...
            if (Application.isPlaying)
                AutoAttributeManager.AutoReferenceAllChildren(instance.gameObject);
            // PoolManager.PreparePoolObjectImplementation(instance.GetComponent<PoolObject>());
            return instance;
        }

        public GameData Owner => this;
        public void SetOwner(GameData owner)
        {

        }
    }
}
