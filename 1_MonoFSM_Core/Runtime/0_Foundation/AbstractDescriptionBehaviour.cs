using System;
using RCGExtension;
using UnityEngine;

using Sirenix.OdinInspector;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;

namespace MonoFSM.Foundation
{
    public abstract class AbstractDescriptionBehaviour : MonoBehaviour, IBeforePrefabSaveCallbackReceiver,
        IDrawHierarchyBackGround
    {
        // Cache for required fields per type
        private static readonly System.Collections.Generic.Dictionary<Type, System.Reflection.FieldInfo[]>
            _requiredFieldsCache = new();

        //沒有做AutoComponent下會顯示error? 還是應該讓prefab openstage時做一次，scene上跳過這個判定，雖然稍嫌trivial
        private static System.Reflection.FieldInfo[] GetRequiredHierarchyValidateFields(System.Type type)
        {
            if (_requiredFieldsCache.TryGetValue(type, out var cachedFields))
                return cachedFields;

            var fields = type.GetFields(System.Reflection.BindingFlags.Instance |
                                        System.Reflection.BindingFlags.NonPublic |
                                        System.Reflection.BindingFlags.Public);

            // Find all fields with [Required] or [DropDownRef] attributes that are not "interfaces"
            //interface在組合component就會看到了, 也比較不會在refactor之後掉reference
            var requiredFields = System.Array.FindAll(fields,
                f => (f.GetCustomAttributes(typeof(RequiredAttribute), false).Length > 0 ||
                      f.GetCustomAttributes(typeof(DropDownRefAttribute), false).Length > 0)
                     && !f.FieldType.IsInterface
                     && (f.IsPublic || f.GetCustomAttributes(typeof(SerializeField), false).Length > 0));
            //FIXME: 把non serialized都跳過了，要不然會一堆沒有自動抓
            
            
            _requiredFieldsCache[type] = requiredFields;

            return requiredFields;
        }

        //用reflection找到所有[Required]的field，然後檢查是否有null

        private bool CheckNullOfRequiredFields()
        {
            var requiredFields = GetRequiredHierarchyValidateFields(GetType());
            foreach (var field in requiredFields)
            {
                // Debug.Log($"Checking required field: {field.Name} in {gameObject.name}", this);
                var value = field.GetValue(this);
                if (value == null)
                {
                    _errorMessage = $"Required field '{field.Name}' is null in {gameObject.name}";
                    // Debug.LogError($"Required field '{field.Name}' is null in {gameObject.name}");
                    return true;
                }
            }

            // Debug.Log($"All required fields are set in {gameObject.name}");
            _errorMessage = "pass!";

            return false;
        }

        [AutoParent] protected MonoDescriptable _self;

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
            try
            {
                gameObject.name = $"[{DescriptionTag}] {DescriptionPreprocess(Description)}";
                UnityEditor.EditorUtility.SetDirty(gameObject);    
            }
            catch (Exception e)

            {
                Debug.LogError($"Error renaming gameObject: {gameObject.name} to [{DescriptionTag}]", this);
            }
            
#endif
        }

        protected virtual void Awake()
        {
        }

        protected virtual void Start()
        {
        }

        public virtual void OnBeforePrefabSave()
        {
#if UNITY_EDITOR
            AutoAttributeManager.AutoReference(this); //有些field需要autoChildren容易造成 description null
            Rename();
#endif
        }


        protected virtual bool HasError()
        {
            //FIXME: Reference Required error? 用reflection找？DropDownRef也是？ cached field會OK嗎？每個type做一次ㄋ
            return CheckNullOfRequiredFields();
        }

        [PreviewInInspector] private string _errorMessage;

        public Color BackgroundColor => new(1.0f, 0f, 0f, 0.3f);

        [ShowInDebugMode] public bool IsDrawGUIHierarchyBackground => !Application.isPlaying && HasError(); //還是用icon? 
    }
}