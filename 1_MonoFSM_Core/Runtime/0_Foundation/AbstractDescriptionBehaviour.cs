using UnityEngine;

using Sirenix.OdinInspector;

using RCGMaker.Core;
using RCGMaker.Runtime;

namespace MonoFSM.Foundation
{
    public abstract class AbstractDescriptionBehaviour : MonoBehaviour, IBeforePrefabSaveCallbackReceiver
    {
        // [AutoParent] protected MonoDescriptable _self;

        //介面上也顯示？textarea?
        public virtual string Description => $"{GetType().Name}";

        protected virtual string DescriptionPreprocess(string text)
            => text;

        protected abstract string DescriptionTag { get; }

        [InfoBox("$Description", InfoMessageType.Info)]
        [HideInInlineEditors]
        [Button]
        protected void Rename()
        {
            // gameObject.name = $"[Action] {GetType().Name.Split("Action")[0]} {renamePostfix}";
#if UNITY_EDITOR
            gameObject.name = $"[{DescriptionTag}] {DescriptionPreprocess(Description)}";
            UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
        }

        protected virtual void Awake()
        {
        }

        protected virtual void Start()
        {
        }

        public void OnBeforePrefabSave()
        {
#if UNITY_EDITOR
            AutoAttributeManager.AutoReference(this); //有些field需要autoChildren容易造成 description null
            Rename();
#endif
        }

        //FIXME: Required error? 用reflection找？DropDownRef也是？ cached field會OK嗎？每個type做一次ㄋ
        private bool HasError()
        {
            return false;
        }

        public Color BackgroundColor => new(1.0f, 0f, 0f, 0.3f);

        public bool IsDrawGUIHierarchyBackground => HasError(); //還是用icon? 
    }
}