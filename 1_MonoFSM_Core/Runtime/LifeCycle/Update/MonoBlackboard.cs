using System;
using System.Collections.Generic;
using System.Linq;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using Fusion;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Variable
{
    //FIXME: 不需要這個吧？
    [DisallowMultipleComponent]
    public class
        MonoBlackboard : MonoBehaviour, IMonoEntity,
        IAfterSimulate //FIXME: 沒有必要用介面？
    {
        private bool IsVariableMissing()
        {
            return !CheckAllVariableExists();
        }

        private string _errorValue;
        private string errorString => _errorValue;

        private bool CheckAllVariableExists()
        {
            if (VariableFolder == null)
            {
                _errorValue = "Variable Folder is null";
                return false;
            }

            if (DefaultTag == null)
            {
                _errorValue = "Descriptable Tags is null or empty"; //需要Descriptable Tag嗎？從Data取得？
                return false;
            }

            foreach (var descriptableTag in DescriptableTags)
            {
                if (descriptableTag == null)
                {
                    _errorValue = "Descriptable Tag is null";
                    return false;
                }

                foreach (var varTag in descriptableTag.containsVariableTypeTags)
                {
                    if (varTag == null)
                    {
                        _errorValue = "Variable Tag is null";
                        return false;
                    }

                    if (!VariableFolder.GetVariable(varTag))
                    {
                        _errorValue = $"Variable {varTag} not found in {name}";
                        return false;
                    }
                }
            }

            return true;
        }

        // [InlineEditor] //InlineEditor就不會畫SOTypeDropdown Drawer hmm..QQ
        [Required]
        [SOConfig("MonoEntityTag")]
        [SerializeField]
        protected MonoEntityTag _entityTag;

        // [InfoBox("$errorString", InfoMessageType.Error, nameof(IsVariableMissing))]
        [InlineEditor]
        [Required]
        [ShowInInspector]
        [SerializeField]
        [Obsolete("應該不需要多個？")]
        [SOConfig("MonoEntityTag")] //可以用一個scriptableObject/preference來改path相對路徑？
        // [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true, ListElementLabelName = "name")]
        protected List<MonoEntityTag> _descriptableTags = new(); //支援多個 DescriptableTag

        private void OnValidate()
        {
            if (_entityTag == null && _descriptableTags != null && _descriptableTags.Count > 0)
                _entityTag = _descriptableTags[0]; //如果沒有指定，則使用第一個 Descriptable Tag
        }

        //FIXME: 好像不用多個耶，錯了，多個schema就好，一個物體對一個entity比較好想
        public List<MonoEntityTag> DescriptableTags => _descriptableTags;

        //FIXME: 可以多個tag? runtime -> schema
        public MonoEntityTag DefaultTag =>
            _entityTag ?? (DescriptableTags?.Count > 0 ? DescriptableTags[0] : null);

        //reflection 同名還會...
        public AbstractMonoVariable this[string statName] => GetVar(statName); //索引器，直接用GetVariable,還是也可以get comp?

        // public AbstractMonoVariable this[VariableTag varTag] => GetVariable(varTag); //索引器，直接用GetVariable,還是也可以get comp?
        // public Component this[Type type] => GetComp(type); //索引器，直接用GetVariable,還是也可以get comp?

        private Dictionary<Type, Component> _compCache = new();

        public Rigidbody rb => GetCompCache<Rigidbody>();

        public T GetCompCache<T>()
            where T : Component
        {
            if (_compCache.TryGetValue(typeof(T), out var comp))
                return comp as T;
            var component = GetComponentInChildren<T>(true); //從children找
            //TODO 要 in parent嗎？
            _compCache[typeof(T)] = component;
            if (component == null)
            {
                this.LogError(
                    "Cannot find component of type " + typeof(T).Name + " in " + name,
                    this
                );
            }

            return component;
        }

        public Component GetCompCache(Type type)
        {
            if (_compCache.TryGetValue(type, out var comp))
                return comp;
            var component = GetComponentInChildren(type);
            if (component != null)
            {
                _compCache[type] = component;
                return component;
            }

            Debug.LogError("Cannot find component of type " + type.Name + " in " + name, this);
            return null;
        }

        //FIXME: 可能有多個？ multiple folder
        [CompRef] [AutoChildren] [Required] protected VariableFolder _variableFolder;

        [CompRef] [AutoChildren] protected StateFolder _stateFolder;

        [CompRef] [AutoChildren] protected EffectDetectable _effectDetectable;
        // [CompRef] [AutoChildren] private EffectReceiverFolder _receiverFolder;

        [CompRef] [AutoChildren] protected SchemaFolder _schemaFolder;

        //從一開始就應該做getter?? 然後用attribute來標記怎麼做的？ 像是[Networked]掛在getter上面？
        public VariableFolder VariableFolder
        {
            get
            {
                AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_variableFolder));
                // this.EnsureComponentInChildren(ref _variableFolder);
                return _variableFolder;
            }
        }


        public StateFolder StateFolder
        {
            get
            {
                AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_stateFolder));
                return _stateFolder;
            }
        }

        public EffectDetectable EffectDetectable
        {
            get
            {
                AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_effectDetectable));
                return _effectDetectable;
            }
        }

        public SchemaFolder SchemaFolder
        {
            get
            {
                AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_schemaFolder));
                return _schemaFolder;
            }
        }

        // public EffectReceiverFolder ReceiverFolder
        // {
        //     get
        //     {
        //         AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_receiverFolder));
        //         return _receiverFolder;
        //     }
        // }

        //多包一層歐，好蠢
        public AbstractMonoVariable GetVar(VariableTag varTag)
        {
            return VariableFolder?.GetVariable(varTag);
        }

        public AbstractMonoVariable GetVar(string varTagName)
        {
            return VariableFolder?.GetVariable(varTagName);
        }

        public TMonoVariable GetVar<TMonoVariable>(VariableTag varTag)
            where TMonoVariable : AbstractMonoVariable
        {
            return VariableFolder.GetVariable<TMonoVariable>(varTag);
        }

        public TMonoVariable GetVar<TMonoVariable>(string varTagName)
            where TMonoVariable : AbstractMonoVariable
        {
            return GetVar(varTagName) as TMonoVariable;
        }

        // public void Simulate(float deltaTime) { }

        // 多 Tag 支援方法
        public MonoEntityTag GetDescriptableTag(string tagName)
        {
            return DescriptableTags?.FirstOrDefault(descriptableTag =>
                descriptableTag != null && descriptableTag.GetStringKey == tagName
            );
        }

        public MonoEntityTag GetDescriptableTag(int index)
        {
            if (DescriptableTags == null || index < 0 || index >= DescriptableTags.Count)
                return null;
            return DescriptableTags[index];
        }

        public List<MonoEntityTag> GetAllDescriptableTags()
        {
            return DescriptableTags ?? new List<MonoEntityTag>();
        }

        public bool ContainsDescriptableTag(string tagName)
        {
            return GetDescriptableTag(tagName) != null;
        }

        public int DescriptableTagCount => DescriptableTags?.Count ?? 0;

        public void AfterSimulate(float deltaTime)
        {
        }
    }
}
