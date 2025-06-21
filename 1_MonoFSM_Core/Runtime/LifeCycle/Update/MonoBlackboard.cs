using System;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Variable
{
    [DisallowMultipleComponent]
    public class MonoBlackboard : MonoBehaviour, IVarBlackboard, IUpdateSimulate //FIXME: 沒有必要用介面？
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

            if (DescriptableTag == null)
            {
                _errorValue = "Descriptable Tag is null"; //需要Descriptable Tag嗎？從Data取得？
                return false;
            }

            foreach (var varTag in DescriptableTag.containsVariableTypeTags)
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

            return true;
        }

        [InfoBox("$errorString", InfoMessageType.Error, nameof(IsVariableMissing))]
        [InlineEditor]
        [Required]
        [ShowInInspector]
        [SerializeField]
        [SOConfig("DescriptableTag")]
        protected MonoDescriptableTag DescriptableTag; //這有什麼用？

        public MonoDescriptableTag Tag => DescriptableTag;

        //reflection 同名還會...
        public AbstractMonoVariable this[string statName] => GetVariable(statName); //索引器，直接用GetVariable,還是也可以get comp?
        // public AbstractMonoVariable this[VariableTag varTag] => GetVariable(varTag); //索引器，直接用GetVariable,還是也可以get comp?
        // public Component this[Type type] => GetComp(type); //索引器，直接用GetVariable,還是也可以get comp?

        private Dictionary<Type, Component> _compCache = new();

        public T GetComp<T>() where T : Component
        {
            if (_compCache.TryGetValue(typeof(T), out var comp)) return comp as T;
            var component = GetComponentInChildren<T>(); //從children找
            if (component != null)
            {
                _compCache[typeof(T)] = component;
                return component;
            }

            Debug.LogError("Cannot find component of type " + typeof(T).Name + " in " + name, this);
            return null;
        }

        public Component GetComp(Type type)
        {
            if (_compCache.TryGetValue(type, out var comp)) return comp;
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
        [Component] [PreviewInInspector] [AutoChildren]
        private VariableFolder _variableFolder;

        public VariableFolder VariableFolder
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying == false && _variableFolder == null)
                    _variableFolder = GetComponentInChildren<VariableFolder>();
                // Debug.Log("VariableFolder is null, try to find it in children", this);
#endif
                if (Application.isPlaying && _variableFolder == null)
                    Debug.LogError(
                        "VariableFolder is null, please ensure it is assigned in the inspector or added as a child component.",
                        this);
                return _variableFolder;
            }
        }

        //多包一層歐，好蠢
        public AbstractMonoVariable GetVariable(VariableTag varTag)
        {
            return VariableFolder.GetVariable(varTag);
        }

        public AbstractMonoVariable GetVariable(string varTagName)
        {
            return VariableFolder.GetVariable(varTagName);
        }

        public TMonoVariable GetVariable<TMonoVariable>(VariableTag varTag) where TMonoVariable : AbstractMonoVariable
        {
            return VariableFolder.GetVariable<TMonoVariable>(varTag);
        }

        public TMonoVariable GetVariable<TMonoVariable>(string varTagName) where TMonoVariable : AbstractMonoVariable
        {
            return GetVariable(varTagName) as TMonoVariable;
        }

        public void Simulate(float deltaTime)
        {
        }

        public void AfterUpdate() //等Simulate都跑完後才CommitValue
        {
            //FIXME: 還是直接給variable folder做就好？
            VariableFolder.CommitVariableValues();
        }
    }
}