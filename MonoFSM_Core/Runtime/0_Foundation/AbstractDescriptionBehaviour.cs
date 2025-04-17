using RCGMaker.Core;
using RCGMaker.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Foundation
{
    public abstract class AbstractDescriptionBehaviour : MonoBehaviour, IBeforePrefabSaveCallbackReceiver
    {
        [AutoParent] protected MonoDescriptable _self;

        //介面上也顯示？textarea?
        protected virtual string Description => $"{GetType().Name}";

        protected virtual string DescriptionPreprocess(string text)
        {
            return text;
        }

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
    }
}