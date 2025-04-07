using System;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using MonoFSM.Variable;
using RCGMaker.Runtime.FSM.RCGStateMachine;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using RCGMaker.Runtime.Mono;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Core.DataProvider
{
    public enum GetFromType
    {
        VariableOwner,
        GlobalInstance
    }

    //TODO: FIXME: drag drop reference後，自動填入tag/monoTag
    public abstract class VariableProviderRef<TVarMonoType, TValueType> : AbstractVariableProviderRef,
        IConfigVar, IVariableProvider,IStringProvider
        where TVarMonoType : AbstractMonoVariable
    {
        public GetFromType _getFromType = GetFromType.VariableOwner;

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

        private MonoBehaviour CurrentTarget
        {
            get
            {
                if (_currentTarget == null)
                    return this;
                return _currentTarget;
            }
        }

        [PreviewInInspector] private MonoBehaviour _currentTarget;

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
        //FIXME: 1. 常常會空著
        public MonoDescriptableTag _parentMonoTag; //空的話就是自己
        
        [BoxGroup("varTag")]
        [GUIColor(0.8f, 1.0f, 0.8f)]
        // [PreviewInInspector]
        [ShowInInspector]
        [DisableIf(nameof(_varTag))]
        public TVarMonoType Variable
        {
            get { return VarRaw as TVarMonoType; }
            set
            {
                //fixme: 自動爬出tag
                _varTag = value._varTag;
                //mono?
            }
        }

        //FIXME: dropdown validate? 多檢查parent的owner? dropdown tag?
        [BoxGroup("varTag")]
        [FormerlySerializedAs("varTag")]
        [InfoBox("Tag Type is wrong", InfoMessageType.Error, nameof(TypeCheckFail))]
        [Required]
        public VariableTag _varTag;

        [BoxGroup("varTag")]
        [ShowInInspector]
        [ValueDropdown(nameof(GetParentVariableTags))]
        private VariableTag DropDownVarTag
        {
            set => _varTag = value;
            get => _varTag;
        }
        //FIXME: 拿到Variable的方式還是要很多種？
        //用varTag, monoTag直接找到 variable
        //從VarMono, 拿到他的variable

        private void OnGlobalMonoTagChange()
        {
            _runtimeCachedOwner = null;
        }

        IEnumerable<ValueDropdownItem<VariableTag>> GetParentVariableTags()
        {
            var parents = CurrentTarget.GetComponentsInParent<VariableOwner>();
            var tags = new List<ValueDropdownItem<VariableTag>>();
            foreach (var parent in parents)
            {
                foreach (var variable in parent.VariableFolder.GetValues)
                {
                    if (variable is TVarMonoType)
                        tags.Add(new ValueDropdownItem<VariableTag>(variable.name, variable._varTag));
                }
            }

            return tags;
        }

        IEnumerable<ValueDropdownItem<MonoDescriptableTag>> GetParentMonoTags()
        {
            var parents = CurrentTarget.GetComponentsInParent<MonoDescriptable>();
            var tags = new List<ValueDropdownItem<MonoDescriptableTag>>();
            foreach (var parent in parents)
            {
                tags.Add(new ValueDropdownItem<MonoDescriptableTag>(parent.Tag.name, parent.Tag));
            }

            return tags;
        }

     

        [PreviewInInspector] private Type variableValueType => typeof(TValueType);
        //FIXME:也可以用string拿？
        // MonoDescriptable parentDescriptable => propertyParent.GetComponentInParent<MonoDescriptable>();

        //prefab裏可以不用有
        //FIXME: 這個auto parent是不是不會跑到？是靠Inspector code才抓到的
        //FIXME: 這樣沒有辦法提早cache?
        // [AutoParent]
        [PreviewInInspector]
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

        VariableOwner FetchOwner(MonoBehaviour target)
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
            if (Application.isPlaying)
            {
                if (_runtimeCachedOwner == null)
                    Debug.LogError("VariableOwner InParent is null at:" + target, target);
            }

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
                {
                    if (variable == null)
                        Debug.LogError($"Variable{_varTag} is null in owner{owner}", CurrentTarget);
                }

                return variable;
            }
        }

        // [ShowInInspector]
        // RCGVariableFolder GetFolder =>  owner?.VariableFolder;
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
    }
}