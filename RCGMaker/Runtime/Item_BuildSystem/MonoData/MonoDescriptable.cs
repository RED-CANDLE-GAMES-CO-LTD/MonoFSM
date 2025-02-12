using System;
using System.Collections.Generic;
using System.Reflection;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.FSM.RCGStateMachine;
using RCGMaker.Runtime.Interact.EffectHit;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using RCGMaker.Runtime.Mono;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime
{
    //描述物件的monoNode, Entity? MonoEntity?
    //場景物件、角色、
    //應該要可以繼承這個嗎？Inventory
    //不該有variable嗎？
    public class MonoDescriptable : VariableOwner, IMonoDescriptable, ILevelAwake //,IVariableOwner //VariableOwner?
    {
#if UNITY_EDITOR
        [RequiredIn(PrefabKind.InstanceInScene)] [PreviewInInspector] [AutoParent]
        MonoDescriptableBinder _binder;
#endif

        [PreviewInInspector] [AutoChildren] GeneralEffectDealer[] _dealers; //可以互動的性質門
        HashSet<GeneralEffectType> _dealerTypeSet = new HashSet<GeneralEffectType>(); //可以被互動的性質
        [PreviewInInspector] private int _dealerSetCount => _dealerTypeSet.Count;
        [PreviewInInspector] [AutoChildren] GeneralEffectReceiver[] _receivers; //可以互動的性質門
        HashSet<GeneralEffectType> _receiverTypeSet = new HashSet<GeneralEffectType>(); //可以被互動的性質

        [PreviewInInspector] private int _receiverSetCount => _receiverTypeSet.Count;

        //帶有xx性質的物件
        public bool HasReceiverType(GeneralEffectType effectType)
        {
            return _receiverTypeSet.Contains(effectType);
        }

        public bool HasDealerType(GeneralEffectType effectType)
        {
            return _dealerTypeSet.Contains(effectType);
        }

        // public DescriptableData SampleData;
        //FIXME: 型別限制？
        [SOConfig("10_Flags/GameData")] [SerializeField]
        DescriptableData data; //config

        public virtual IDescriptableData Descriptable => data;

        public T GetData<T>() where T : DescriptableData
        {
            return data as T;
        }

        public DescriptableData Data => data;

        //FIXME:  schema
        [InfoBox("$errorString", InfoMessageType.Error, nameof(IsVariableMissing))]
        [InlineEditor]
        [Required]
        [ShowInInspector]
        [SerializeField]
        [SOConfig("DescriptableTag")]
        MonoDescriptableTag DescriptableTag;

        public MonoDescriptableTag Tag => DescriptableTag;

        public virtual void OnUIEventReceived()
        {
            Debug.Log("UI Event Received", this);
        }

        private string errorValue;
        string errorString => errorValue;


        bool IsVariableMissing()
        {
            return !CheckAllVariableExists();
        }

        bool CheckAllVariableExists()
        {
            if (DescriptableTag == null || VariableFolder == null)
            {
                errorValue = "Descriptable Tag or Variable Folder is null";
                return false;
            }

            foreach (var varTag in DescriptableTag.containsVariableTypeTags)
            {
                if (varTag == null)
                {
                    errorValue = "Variable Tag is null";
                    return false;
                }

                if (!VariableFolder.GetVariable(varTag))
                {
                    errorValue = $"Variable {varTag} not found in {name}";
                    return false;
                }
            }

            return true;
        }

        public object GetValue(VariableTag varTag)
        {
            var variable = VariableFolder.GetVariable(varTag);
            if (variable == null)
            {
                Debug.LogError($"Variable {varTag} not found in {name}", this);
                return null;
            }

            return variable.objectValue;
        }

        public AbstractMonoVariable GetVariable(VariableTag varTag)
        {
            return VariableFolder.GetVariable(varTag);
        }

        public AbstractMonoVariable GetVariable(string varTagName)
        {
            return VariableFolder.GetVariable(varTagName);
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

        public void EnterLevelAwake()
        {
            // _receiverTypeSet = new HashSet<GeneralEffectType>();
            if (_receivers != null)
                foreach (var receiver in _receivers)
                {
                    _receiverTypeSet.Add(receiver.EffectType);
                }

            // _dealerTypeSet = new HashSet<GeneralEffectType>();
            if (_dealers != null)
                foreach (var dealer in _dealers)
                {
                    _dealerTypeSet.Add(dealer.EffectType);
                }
        }

        // [PreviewInInspector]
        // [Component]
        // [AutoChildren]
        // RCGVariableFolder _variableFolder; //需要這個嗎？
        //
        // public RCGVariableFolder VariableFolder
        // {
        //     get
        //     {
        //         #if UNITY_EDITOR
        //         if(Application.isPlaying == false)
        //             _variableFolder = GetComponentInChildren<RCGVariableFolder>();
        //         #endif
        //         
        //         return _variableFolder;
        //     }
        // }
    }
}