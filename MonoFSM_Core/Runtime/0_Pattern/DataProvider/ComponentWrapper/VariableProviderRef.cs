using System;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using RCGMaker.Runtime.FSM.RCGStateMachine;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using RCGMaker.Runtime.Mono;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Core.DataProvider
{
    public enum GetFromType
    {
        ParentVarOwner,
        GlobalInstance,
        VariableOwnerProvider,
    }

    //TODO: FIXME: drag drop reference後，自動填入tag/monoTag
    public abstract class VariableProviderRef<TVarMonoType, TValueType> : AbstractVariableProviderRef,
        IValueProvider, IVariableProvider, IStringProvider
        where TVarMonoType : AbstractMonoVariable
    {
        // private void OnValidate()
        // {
        //     if (Application.isPlaying) return;
        //     if (GetComponent<IVariableOwnerProvider>() != null)
        //         _getFromType = GetFromType.VariableOwnerProvider;
        //     else if (GetComponentInParent<VariableOwner>() != null)
        //         _getFromType = GetFromType.ParentVarOwner;
        //     else
        //         _getFromType = GetFromType.GlobalInstance;
        //
        // }

        public override Type GetVarType => typeof(TVarMonoType);

        [TabGroup("Owner Setting")] public GetFromType _getFromType = GetFromType.ParentVarOwner;

        public override string ToString()
        {
            return varTag.name;
        }

        public string GetString()
        {
            return Value.ToString();
        }

        public override Type GetValueType => typeof(TValueType);

        public TVarMonoType GetVar()
        {
            return GetVar<TVarMonoType>();
        }

        [ShowInDebugMode]
        private MonoBehaviour CurrentTarget
        {
            get
            {
                if (_currentTarget == null)
                    return this;
                return _currentTarget;
            }
        }

        private MonoBehaviour _currentTarget;

        //Dynamic Parent
        public AbstractMonoVariable GetMonoVariableFrom(MonoBehaviour target)
        {
            _currentTarget = target;
            FetchOwner(target);
            //FIXME:
            return VarRaw;
        }

        public TValueType GetValueFrom(MonoBehaviour target)
        {
            _currentTarget = target;
            FetchOwner(target);
            return Value;
        }

        private bool TypeCheckFail()
        {
            if (_varTag == null) return false;
            return typeof(TValueType).IsAssignableFrom(_varTag._valueFilterType.RestrictType) == false;
        }

        // [ValueDropdown(nameof(GetGlobalMonoTags))] [OnValueChanged(nameof(OnGlobalMonoTagChange))]
        //FIXME: 常常會空著?
        //globalTag
        //a(object).b(variable)
        //VariableOwner的話就可以往parent找，不是的話可以從asset找？ auto assign? 或是根本不需要
        [TabGroup("Owner Setting")] [HideIf(nameof(IsFromParentOwner))]
        public MonoDescriptableTag _parentMonoTag; //空的話就是自己

        private bool IsFromParentOwner()
        {
            if (_getFromType == GetFromType.ParentVarOwner)
                return true;
            return false;
        }
        
        [BoxGroup("varTag")]
        [ShowInInspector]
        [ValueDropdown(nameof(GetParentVariableTags))]
        private VariableTag DropDownVarTag
        {
            set => _varTag = value;
            get => _varTag;
        }

        [BoxGroup("varTag")]
        [GUIColor(0.8f, 1.0f, 0.8f)]
        // [PreviewInInspector]
        [ShowInInspector]
        [DisableIf(nameof(_varTag))]
        public TVarMonoType Variable
        {
            get => VarRaw as TVarMonoType;
            set =>
                //fixme: 自動爬出tag
                _varTag = value._varTag;
            //mono?
        }

        //FIXME: dropdown validate? 多檢查parent的owner? dropdown tag?
        [ShowInDebugMode]
        [BoxGroup("varTag")]
        [FormerlySerializedAs("varTag")]
        [InfoBox("Tag Type is wrong", InfoMessageType.Error, nameof(TypeCheckFail))]
        [Required]
        public VariableTag _varTag;


        //FIXME: 拿到Variable的方式還是要很多種？
        //用varTag, monoTag直接找到 variable
        //從VarMono, 拿到他的variable

        private void OnGlobalMonoTagChange()
        {
            _runtimeCachedOwner = null;
        }
        
        // IEnumerable<ValueDropdownItem<

        [ShowIf(nameof(_getFromType), GetFromType.VariableOwnerProvider)]
        [CompRef]
        // [Component(AddComponentAt.Same)]
        [Auto]
        [TabGroup("Owner Setting")]
        public IVariableOwnerProvider variableOwnerProvider;

        private IEnumerable<ValueDropdownItem<VariableTag>> GetParentVariableTags() //editor time?
        {
            
            var tagDropdownItems = new List<ValueDropdownItem<VariableTag>>();
            switch (_getFromType)
            {
                case GetFromType.VariableOwnerProvider:

                    if (variableOwnerProvider == null)
                        return tagDropdownItems;
                    if (Application.isPlaying)
                    {
                        var variables = variableOwnerProvider.GetVariableOwner().VariableFolder.GetValues;
                        foreach (var variable in variables)
                            if (variable is TVarMonoType)
                                tagDropdownItems.Add(
                                    new ValueDropdownItem<VariableTag>(variable.name, variable._varTag));
                    }
                    else
                    {
#if UNITY_EDITOR
                        var tags = _parentMonoTag.containsVariableTypeTags;
                        foreach (var varTag in tags)
                            tagDropdownItems.Add(new ValueDropdownItem<VariableTag>(varTag.name, varTag));
#endif
                    }

                    break;


                case GetFromType.GlobalInstance:
                    
                    var instance = CurrentTarget.GetGlobalInstance(_parentMonoTag);
                    if (instance == null)
                    {
                        //從MonoDescriptableTag找到varTag (schema一定會一致嗎？不一定)
                        var parentMonoVarTags = _parentMonoTag.containsVariableTypeTags;
                        foreach (var parentVarTag in parentMonoVarTags)
                        {
                            tagDropdownItems.Add(new ValueDropdownItem<VariableTag>(parentVarTag.name, parentVarTag));
                        }
                        return tagDropdownItems;
                    }

                    //從instance直接找variable
                    foreach (var variable in instance.VariableFolder.GetValues)
                        if (variable is TVarMonoType)
                            tagDropdownItems.Add(new ValueDropdownItem<VariableTag>(variable.name, variable._varTag));

                    break;
                case GetFromType.ParentVarOwner:
                {
                    var parents = CurrentTarget.GetComponentsInParent<VariableOwner>();

                    foreach (var parent in parents)
                    {
                        if (parent.VariableFolder == null)
                            // Debug.LogError("Parent VariableFolder is null", parent);
                            continue;

                        foreach (var variable in parent.VariableFolder.GetValues)
                            if (variable is TVarMonoType)
                                tagDropdownItems.Add(new ValueDropdownItem<VariableTag>(variable.name, variable._varTag));
                    }

                    if (tagDropdownItems.Count == 0)
                    {
                        Debug.LogError("All Parent VariableFolder has no Variable", CurrentTarget);
                        foreach (var parent in parents) Debug.LogError("Parent  has no Variable?", parent);
                    }

                    break;
                }
            }


            return tagDropdownItems;
        }

        private IEnumerable<ValueDropdownItem<MonoDescriptableTag>> GetParentMonoTags()
        {
            var parents = CurrentTarget.GetComponentsInParent<MonoDescriptable>();
            var tags = new List<ValueDropdownItem<MonoDescriptableTag>>();
            foreach (var parent in parents)
                tags.Add(new ValueDropdownItem<MonoDescriptableTag>(parent.Tag.name, parent.Tag));

            return tags;
        }


        [ShowInDebugMode]
        [PreviewInInspector] private Type variableValueType => typeof(TValueType);
        //FIXME:也可以用string拿？
        // MonoDescriptable parentDescriptable => propertyParent.GetComponentInParent<MonoDescriptable>();

        //prefab裏可以不用有
        //FIXME: 這個auto parent是不是不會跑到？是靠Inspector code才抓到的
        //FIXME: 這樣沒有辦法提早cache?
        // [AutoParent]
        [ShowInDebugMode]
        public VariableOwner owner
        {
            get
            {
                if (Application.isPlaying && _runtimeCachedOwner != null) //runtime才要cache
                    return _runtimeCachedOwner;

                _runtimeCachedOwner = FetchOwner(CurrentTarget);
                return _runtimeCachedOwner;
            }
        }

        private VariableOwner FetchOwner(MonoBehaviour target)
        {
            if (target == null)
            {
                if (Application.isPlaying)
                    Debug.LogError("Target is null", this);
                return null;
            }

            if (_parentMonoTag != null)
            {
                var monoCompInParent = target.GetMonoCompInParent(_parentMonoTag);
                if (monoCompInParent == null) return null;
                //FIXME: 
                return monoCompInParent;
            }

            _runtimeCachedOwner = target.GetComponentInParent<VariableOwner>();
            //FIXME: 有variable folder的才算？
            if (Application.isPlaying)
                if (_runtimeCachedOwner == null)
                    Debug.LogError("VariableOwner InParent is null at:" + target, target);

            return _runtimeCachedOwner;
            // return _runtimeCachedOwner;
        }

        private VariableOwner _runtimeCachedOwner;

        public void SetValue(TValueType value, MonoBehaviour byWho)
        {
            VarRaw.SetValue(value, byWho);
        }

        public override TMonoVar GetVar<TMonoVar>()
        {
            return VarRaw as TMonoVar;
        }


        // [GUIColor(0.8f, 1.0f, 0.8f)]
        // [PreviewInInspector]
        // [PreviewInInspector]
        // public AbstractMonoVariable VarRaw
        // {
        //     get
        //     {
        //         if (varTag == null && Application.isPlaying)
        //         {
        //             Debug.LogError("Variable Tag is null", propertyParent);
        //             return null;
        //         }
        //
        //         var descriptable = propertyParent.GetGlobalInstance(monoDescriptableTag);
        //         if (descriptable == null) return null;
        //         return descriptable.GetVariable(varTag);
        //     }
        // }


        public override AbstractMonoVariable VarRaw
        {
            get
            {

                if (_getFromType == GetFromType.GlobalInstance)
                {
                    var descriptable = CurrentTarget.GetGlobalInstance(_parentMonoTag);
                    if (descriptable == null) return null;
                    return descriptable.GetVariable(_varTag);
                }
                
                if (_getFromType == GetFromType.VariableOwnerProvider)
                {
                    if (Application.isPlaying == false)
                        return null;
                    
                    Debug.Log("_getFromType == GetFromType.VariableOwnerProvider",this);
                    
                    if (this.variableOwnerProvider == null)
                        return null;
                    return this.variableOwnerProvider.GetVariableOwner().GetVariable(_varTag);
                }

                if (owner == null)
                {
                    if (Application.isPlaying)
                        Debug.LogError("Owner is null", CurrentTarget);
                    return null;
                }

                if (owner.VariableFolder == null)
                {
                    if (Application.isPlaying)
                        Debug.LogError("VariableFolder is null", CurrentTarget);
                    return null;
                }
                
                
        
                
                var variable = owner.GetVariable(_varTag);
                if (Application.isPlaying)
                    if (variable == null)
                        Debug.LogError($"Variable{_varTag} is null in owner{owner}", CurrentTarget);

                return variable;
            }
        }

        // [ShowInInspector]
        // RCGVariableFolder GetFolder =>  owner?.VariableFolder;
        [ShowInDebugMode]
        [PreviewInInspector] public TValueType Value => VarRaw == null ? default : VarRaw.GetValue<TValueType>();

        public override VariableTag varTag
        {
            get => _varTag;
            set => _varTag = value;
        }

        public object GetValue()
        {
            return Value;
        }

        public T GetValue<T>()
        {
            var value = GetValue();
            switch (value)
            {
                case null:
                    return default;
                case T value1:
                    return value1;
                default:
                    Debug.LogError($"Cannot cast {value} to {typeof(T)}", this);
                    return default;
            }
        }

        public string GetDescription()
        {
            var str = string.Empty;
            if (_parentMonoTag) str = _parentMonoTag.name + ".";
            str += varTag?.name;
            return str;
        }

        public string Description => GetDescription();
        
    }
}