using System;
using System.Collections.Generic;
using System.Reflection;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.Item_BuildSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime
{
    //描述物件的monoNode
    public class MonoDescriptable:MonoBehaviour,IMonoDescriptable, IValueOfKey<MonoDescriptableTag>
    {
        [Component]
        [AutoChildren]
        RCGVariableFolder _variableFolder;

        public GameFlagDescriptable data;
        public virtual IDescriptable Descriptable => data;
        public virtual void OnUIEventReceived()
        {
            Debug.Log("UI Event Received",this);   
        }

        public MonoDescriptableTag Tag => DescriptableTag;

        private string errorValue;
        string errorString => errorValue;
        [InfoBox(nameof(errorString), InfoMessageType.Error, nameof(IsVariableMissing))]
        [InlineEditor] [Required] [ShowInInspector]
        [SerializeField]
        [SOConfig("DescriptableTag")]
        MonoDescriptableTag DescriptableTag;
        
        bool IsVariableMissing()
        {
            return !CheckAllVariableExists();
        }
        bool CheckAllVariableExists()
        {
            if(DescriptableTag == null)
                return false;
            foreach (var varTag in DescriptableTag.containsVariableTypeTags)
            {
                if (varTag == null)
                {
                    errorValue = "Variable Tag is null";
                    return false;
                }
                    
                if (!_variableFolder.GetVariable(varTag))
                {
                    errorValue = $"Variable {varTag} not found in {name}";
                    return false;
                }
                    
            }

            return true;
        }
        public object GetValue(VariableTag varTag)
        {
            var variable = _variableFolder.GetVariable(varTag);
            if (variable == null)
            {
                Debug.LogError($"Variable {varTag} not found in {name}", this);
                return null;
            }
            return variable.objectValue;
        }
        public AbstractVariable GetVariable(VariableTag varTag)
        {
            return _variableFolder.GetVariable(varTag);
        }

        // public int GetIntValue(VariableTypeTag typeTag)
        // {
        //     return (_variableFolder.GetVariable(typeTag) as VariableInt).CurrentValue;
        // }
        //
        // public float GetFloatValue(VariableTypeTag typeTag)
        // {
        //     return (_variableFolder.GetVariable(typeTag) as VariableFloat).CurrentValue;
        // }
        //
        // public bool GetBoolValue(VariableTypeTag typeTag)
        // {
        //     return (_variableFolder.GetVariable(typeTag) as VariableBool).CurrentValue;
        // }
        //往下找variable?
        //任何型別呢？
        
        public Dictionary<string, Func<IMonoDescriptable, object>> propertyCache = new();

        public Func<IMonoDescriptable, object> GetPropertyCache(
            string propertyName)
        {
            if (propertyCache.TryGetValue(propertyName, out var info))
                return info;


            var propertyInfo = GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            // Debug.Log($"Property {propertyName} found in {sourceObject.GetType()}", sourceObject);

            if (propertyInfo == null)
            {
                propertyCache[propertyName] = null;
                //FIXME: 可能因為unknownData所以有可能會找不到 有點危險？
                // Debug.LogError($"Property {propertyName} not found in {GetType()}");
                return null;
            }

            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
            {
                Debug.LogError($"Property {propertyName} does not have a getter in {GetType()}"
                );
                return null;
            }

            Func<IMonoDescriptable, object>
                _getMyProperty = (source) => getMethod.Invoke(source, null);
            propertyCache[propertyName] = _getMyProperty;
            return _getMyProperty;
        }

        public MonoDescriptableTag Key => DescriptableTag;
    }
}