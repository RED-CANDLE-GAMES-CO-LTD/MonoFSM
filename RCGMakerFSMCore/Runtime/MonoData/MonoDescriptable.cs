using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using MonoFSM.Variable;
using RCGMaker.Runtime.FSM.RCGStateMachine;
using RCGMaker.Runtime.Interact.EffectHit;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using RCGMaker.Runtime.Mono;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace RCGMaker.Runtime
{
    [Searchable]
    public class MonoDescriptable : AbstractMonoDescriptable<DescriptableData> //這樣data也要一直繼承，好ㄇ...
    {
        public VarFloat this[string statName] => GetVariable(statName) as VarFloat;
    }

    //描述物件的monoNode, Entity? MonoEntity?
    //場景物件、角色、
    //應該要可以繼承這個嗎？Inventory
    //不該有variable嗎？
    public class AbstractMonoDescriptable<TMonoDescriptable> : VariableOwner, IMonoDescriptable, ILevelAwake
        where TMonoDescriptable : DescriptableData //,IVariableOwner //VariableOwner?
    {
        //FIXME: 更複雜的描述組合？
        public virtual string RuntimeDescription => Data.Description;

#if UNITY_EDITOR
        [RequiredIn(PrefabKind.InstanceInScene)] [PreviewInInspector] [AutoParent]
        MonoDescriptableBinder _binder;
#endif

        //GameLogic不該Nested?
        //FIXME: 太深了...會包到過多的東西
        [PreviewInInspector] [AutoChildren] GeneralEffectDealer[] _dealers; //可以互動的性質門
        // private HashSet<GeneralEffectType> _dealerTypeSet = new HashSet<GeneralEffectType>(); //可以被互動的性質

        Dictionary<GeneralEffectType, GeneralEffectDealer> _dealerTypeMap = new();

        [PreviewInInspector] private int DealerSetCount => _dealerTypeMap.Count;

        [PreviewInInspector] [AutoChildren] GeneralEffectReceiver[] _receivers; //可以互動的性質門

        // readonly HashSet<GeneralEffectType> _receiverTypeSet = new HashSet<GeneralEffectType>(); //可以被互動的性質
        Dictionary<GeneralEffectType, GeneralEffectReceiver> _receiverTypeMap = new();

        [PreviewInInspector] private int ReceiverSetCount => _receiverTypeMap.Count;


        //帶有xx性質的物件
        public bool HasReceiverType(GeneralEffectType effectType)
        {
            return _receiverTypeMap.ContainsKey(effectType);
            // return _receiverTypeSet.Contains(effectType);
        }

        public bool HasDealerType(GeneralEffectType effectType)
        {
            return _dealerTypeMap.ContainsKey(effectType);
            // return _dealerTypeSet.Contains(effectType);
        }

        public GeneralEffectDealer GetDealer(GeneralEffectType effectType)
        {
            return _dealerTypeMap[effectType];
        }

        // public DescriptableData SampleData;
        //FIXME: 型別限制？
        //FIXME: Generic?
        //FIXME: 不一定需要data?
        [SOConfig("10_Flags/GameData")] [SerializeField]
        protected TMonoDescriptable data; //config

        public virtual IDescriptableData Descriptable => data;

        public T GetData<T>() where T : DescriptableData
        {
            return data as T;
        }

        [ShowInInspector]
        public TMonoDescriptable Data
        {
            get => data;
            set => data = value;
        }


        //FIXME:  需要Descriptable Tag嗎？從Data拿就好了？
        [InfoBox("$errorString", InfoMessageType.Error, nameof(IsVariableMissing))]
        [InlineEditor]
        [Required]
        [ShowInInspector]
        [SerializeField]
        [SOConfig("DescriptableTag")]
        protected MonoDescriptableTag DescriptableTag; //這有什麼用？

        //FIXME: 還不只需要一種呢....可能需要多種tag
        [SerializeField] MonoDescriptableTag[] DescriptableTags; //

        public MonoDescriptableTag Tag => DescriptableTag;

        public virtual void OnUIEventReceived() //FIXME; 這啥XD
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
            if (VariableFolder == null)
            {
                errorValue = "Variable Folder is null";
                return false;
            }

            if (DescriptableTag == null)
            {
                errorValue = "Descriptable Tag is null"; //需要Descriptable Tag嗎？從Data取得？
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

        public MonoDescriptableTag[] GetKeys()
        {
            return DescriptableTags;
        }

        public void EnterLevelAwake()
        {
            // _receiverTypeSet = new HashSet<GeneralEffectType>();
            if (_receivers != null)
                foreach (var receiver in _receivers)
                {
                    // _receiverTypeSet.Add(receiver.EffectType);
                    _receiverTypeMap[receiver.EffectType] = receiver;
                }

            foreach (var dealer in _dealers)
            {
                if (_dealerTypeMap.TryAdd(dealer.EffectType, dealer) == false)
                    Debug.LogError($"Dealer {dealer.EffectType} already exists", this);
            }

            // _dealerTypeMap = _dealers.ToDictionary(dealer => dealer.EffectType);

            // _dealerTypeSet = new HashSet<GeneralEffectType>();
            // if (_dealers != null)
            //     foreach (var dealer in _dealers)
            //     {
            //         // _dealerTypeSet.Add(dealer.EffectType);
            //         _dealerTypeMap[dealer.EffectType] = dealer;
            //     }
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

        //繼承MonoDescriptable的class，可以透過這個方法來將所有的variable field mapping到VariableFolder
        FieldInfo[] _variableFields;

        [Button]
        void FieldMapping()
        {
            //find all fields which inherit from AbstractMonoVariable
            //Check the value is not null
            //FIXME: 用名字mapping, 不好，直接用tag map, 沒有配到表示要生variable之類的

            _variableFields = this.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.FieldType.IsSubclassOf(typeof(AbstractMonoVariable)))
                .ToArray();
            _variableFields.ForEach(field =>
            {
                //FIXME: 要加Type嗎...
                var fieldName = field.Name; //$"[{field.FieldType.Name}] {field.Name}";
                //把空白,_拿掉好了
                fieldName = fieldName.Replace(" ", "").Replace("_", "");
                //FIXME: 模糊搜尋？
                Debug.Log("fieldNameTarget: " + fieldName);
                var variable = VariableFolder.GetVariable(fieldName);
                if (variable != null)
                {
                    Debug.Log($"Set {fieldName} to {variable}", this);
                    field.SetValue(this, variable);
                }
                else
                {
                    Debug.Log("all variables count:" + VariableFolder.GetValues.Count);
                    VariableFolder.GetValues.ForEach(v => Debug.Log(v._varTag.GetStringKey, v._varTag));
                    Debug.LogError($"{fieldName} not found", this);
                }
                // var value = field.GetValue(this) as AbstractMonoVariable;
                // if (value == null)
                // {
                //     Debug.LogError($"Field {field.Name} is null", this);
                // }
            });
        }
    }
}