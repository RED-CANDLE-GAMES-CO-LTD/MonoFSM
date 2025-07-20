using System;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Utilities;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    //var local ref? 
    public class VarRef : AbstractVariableProviderRef, IValueProvider
    {
        //FIXME: 這裡自帶 field entry就可以找到任何東西了？

        #region Local Variable Reference

        // [OnValueChanged(nameof(OnAssignDirectVar))]
        // [PropertyOrder(-1)]
        // [DropDownRef] //FIXME: 有tag的話就不需要 Required 了?
        // [SerializeField]
        // private AbstractMonoVariable _monoVariable;

        // private void OnAssignDirectVar()
        // {
        //     if (_monoVariable != null) _varTag = _monoVariable._varTag;
        // }

        #endregion

        [BoxGroup("varTag")]
        [ShowInInspector]
        [ValueDropdown(nameof(GetParentVariableTags), NumberOfItemsBeforeEnablingSearch = 5)]
        private VariableTag DropDownVarTag
        {
            set => _varTag = value;
            get => _varTag;
        }

        private IEnumerable<ValueDropdownItem<VariableTag>> GetParentVariableTags()
        {
            return entityProvider?.entityTag?.GetVariableTagItems() ?? ParentEntity?.GetVarTagOptions();
        }


        [ShowInDebugMode]
        [BoxGroup("varTag")]
        [Required]
        public VariableTag _varTag;
        // private bool TypeCheckFail()
        // {
        //     if (_varTag == null) return false;
        //     return typeof(TValueType).IsAssignableFrom(_varTag._valueFilterType.RestrictType) == false;
        // }

        [ShowInPlayMode]
        public override AbstractMonoVariable VarRaw
        {
            get
            {
                if (entityProvider != null) //這個可以是null...hmmm
                    return entityProvider?.Blackboard?.GetVar(_varTag);

                // if (_monoVariable != null)
                //     return _monoVariable;
                // Debug.LogError("VarRef: No variable found", this);
                return ParentEntity.GetVar(_varTag); //如果沒有黑板就從parent entity拿
            }
        }

        private MonoEntity ParentEntity
        {
            get
            {
                this.EnsureComponentInParent(ref _parentEntity);
                return _parentEntity;
            }
        }

        [AutoParent] private MonoEntity _parentEntity;

        [CompRef] [Auto] private IMonoEntityProvider _monoEntityProvider;

        private IMonoEntityProvider entityProvider
        {
            get
            {
                this.EnsureComponent(ref _monoEntityProvider, false); //不一定需要這個物件
                return _monoEntityProvider;
            }
        }

        protected override string DescriptionPreprocess(string text)
        {
            if (entityProvider != null) return entityProvider.entityTag?.name + "." + text;
            return text;
        }

        // public override AbstractMonoVariable VarRaw => _monoVariable;

        [PreviewInInspector] public override Type ValueType => !HasFieldPath ? VarRaw?.ValueType : lastPathEntryType;

        // public override Type GetValueType => 
        public override Type GetVarType => _varTag.VariableMonoType; // VarRaw?.GetType();
        public override VariableTag varTag => _varTag;

        public override TVariable GetVar<TVariable>()
        {
            if (VarRaw is TVariable variable) return variable;
            throw new InvalidCastException($"Cannot cast {VarRaw.GetType()} to {typeof(TVariable)}");
        }

        public override T1 Get<T1>()
        {
            if (!typeof(T1).IsAssignableFrom(ValueType))
            {
                Debug.LogError(
                    $"無法將 {ValueType} 轉換為 {typeof(T1)}，請檢查變數類型或欄位路徑設定。",
                    this);
                return default;
            }

            // 如果沒有設定欄位路徑，直接回傳變數值
            if (!HasFieldPath)
                return VarRaw.GetValue<T1>();

            // 使用欄位路徑存取特定欄位值
            var fieldValue = ReflectionUtility.GetFieldValueFromPath(VarRaw, _pathEntries, gameObject);

            if (fieldValue is T1 tValue) return tValue;

            // 嘗試轉型
            if (fieldValue != null)
                try
                {
                    return (T1)Convert.ChangeType(fieldValue, typeof(T1));
                }
                catch (Exception e)
                {
                    if (Application.isPlaying)
                        Debug.LogError(
                            $"無法將欄位值 {fieldValue} (型別: {fieldValue.GetType()}) 轉換為 {typeof(T1)}: {e.Message}",
                            this);
                }

            Debug.LogError($"VarRef: 欄位值為 null 或轉換失敗 Var:{VarRaw}", this);
            return default;
        }


        // protected override string DescriptionTag => "varRef";
    }
}