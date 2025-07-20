using System;
using System.Diagnostics;
using RCGExtension;
using UnityEngine;

using Sirenix.OdinInspector;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using Debug = UnityEngine.Debug;

namespace MonoFSM.Foundation
{
    public abstract class AbstractDescriptionBehaviour : MonoBehaviour, IBeforePrefabSaveCallbackReceiver,
        IAfterPrefabStageOpenCallbackReceiver, IDrawHierarchyBackGround
    {
        // Cache for required fields per type (serialized only)
        private static readonly System.Collections.Generic.Dictionary<Type, System.Reflection.FieldInfo[]>
            _requiredFieldsCache = new();

        // Cache for required fields per type (including non-serialized)
        private static readonly System.Collections.Generic.Dictionary<Type, System.Reflection.FieldInfo[]>
            _requiredFieldsCacheWithNonSerialized = new();

        // Track if we're in prefab stage mode for more detailed error checking
        private bool _isPrefabStageMode = false;

        //沒有做AutoComponent下會顯示error? 還是應該讓prefab openstage時做一次，scene上跳過這個判定，雖然稍嫌trivial
        private static System.Reflection.FieldInfo[] GetRequiredHierarchyValidateFields(Type type,
            bool includeNonSerialized = false)
        {
            var cache = includeNonSerialized ? _requiredFieldsCacheWithNonSerialized : _requiredFieldsCache;

            if (cache.TryGetValue(type, out var cachedFields))
                return cachedFields;

            var fields = type.GetFields(System.Reflection.BindingFlags.Instance |
                                        System.Reflection.BindingFlags.NonPublic |
                                        System.Reflection.BindingFlags.Public);

            // Find all fields with [Required] or [DropDownRef] attributes that are not "interfaces"
            //interface在組合component就會看到了, 也比較不會在refactor之後掉reference
            System.Reflection.FieldInfo[] requiredFields;

            if (includeNonSerialized)
                // Include all fields with required attributes, regardless of serialization
                requiredFields = Array.FindAll(fields,
                    f => (f.GetCustomAttributes(typeof(RequiredAttribute), false).Length > 0 ||
                          f.GetCustomAttributes(typeof(DropDownRefAttribute), false).Length > 0)
                         && !f.FieldType.IsInterface);
            else
                // Only include public or SerializeField attributes (original behavior)
                requiredFields = Array.FindAll(fields,
                    f => (f.GetCustomAttributes(typeof(RequiredAttribute), false).Length > 0 ||
                          f.GetCustomAttributes(typeof(DropDownRefAttribute), false).Length > 0)
                         && !f.FieldType.IsInterface
                         && (f.IsPublic || f.GetCustomAttributes(typeof(SerializeField), false).Length > 0));

            cache[type] = requiredFields;

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
                    // UnityEditor.EditorGUIUtility.PingObject(this);
                    return true;
                }
            }

            // Debug.Log($"All required fields are set in {gameObject.name}");
            _errorMessage = "pass!";

            return false;
        }

        // Prefab stage specific error checking that includes non-serialized fields
        private bool CheckNullOfRequiredFieldsForPrefabStage(bool isShowError = false)
        {
            var requiredFields = GetRequiredHierarchyValidateFields(GetType(), true);
            foreach (var field in requiredFields)
            {
                var value = field.GetValue(this);
                if (value == null)
                {
                    _errorMessage = $"Required field '{field.Name}' is null in {gameObject.name} (Prefab Stage Check)";
                    //FIXME: 一打開prefab想log?

                    if (isShowError)
                    {
                        Debug.LogError($"Required field '{field.Name}' is null in {gameObject.name}", this);
                        UnityEditor.EditorGUIUtility.PingObject(this);
                    }
                        
                    return true;
                }
            }

            _errorMessage = "pass!";
            return false;
        }

        [AutoParent] protected MonoEntity _self;

        //介面上也顯示？textarea?
        public virtual string Description => $"{GetType().Name}";

        protected virtual string DescriptionPreprocess(string text)
            => text;

        protected abstract string DescriptionTag { get; }

        [InfoBox("$Description")]
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
            CheckNullOfRequiredFieldsForPrefabStage(true);
            Rename();
#endif
        }

        public virtual void OnAfterPrefabStageOpen()
        {
            _isPrefabStageMode = true;
            // Trigger error checking with non-serialized fields included
            CheckNullOfRequiredFieldsForPrefabStage(true);
        }


        protected virtual bool HasError()
        {
            // Use different checking logic based on environment
            var isInPrefabStage = _isPrefabStageMode;

#if UNITY_EDITOR
            // Double-check using Unity's API for more reliability
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null) isInPrefabStage = true;


            if (isInPrefabStage)
                return CheckNullOfRequiredFieldsForPrefabStage();
            else
                return CheckNullOfRequiredFields();
#else
            return false;
#endif
            
        }

        [PreviewInInspector] private string _errorMessage;

        public Color BackgroundColor => new(1.0f, 0f, 0f, 0.3f);

        [ShowInDebugMode] public bool IsDrawGUIHierarchyBackground => !Application.isPlaying && HasError(); //還是用icon? 
    }
}