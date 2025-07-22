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
    //用這顆就夠了，其他應該都不需要了？除了literal
    public class ValueProvider : AbstractVariableProviderRef, IValueProvider
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

        [PropertyOrder(-1)]
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
        // [Required]
        [SerializeField]
        private VariableTag _varTag;
        // private bool TypeCheckFail()
        // {
        //     if (_varTag == null) return false;
        //     return typeof(TValueType).IsAssignableFrom(_varTag._valueFilterType.RestrictType) == false;
        // }

        [ShowInPlayMode]
        public override AbstractMonoVariable VarRaw //可以去拿MonoEntity的資料？而不是一定要透過Var?
        {
            get
            {
                if (entityProvider != null) //這個可以是null...hmmm
                    return entityProvider?.monoEntity?.GetVar(_varTag);

                // if (_monoVariable != null)
                //     return _monoVariable;
                // Debug.LogError("VarRef: No variable found", this);
                return ParentEntity?.GetVar(_varTag); //如果沒有黑板就從parent entity拿
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
            if (entityProvider != null) return entityProvider.entityTag?.name + "(entity)." + text;
            return text;
        }

        // public override AbstractMonoVariable VarRaw => _monoVariable;

        [PreviewInInspector]
        public override Type ValueType => !HasFieldPath ? GetTarget()?.ValueType : lastPathEntryType;


        private IValueProvider GetTarget()
        {
            if (_varTag == null)
            {
                if (entityProvider != null)
                    return entityProvider.monoEntity;
                return ParentEntity;
            }
            return VarRaw;
        }


        // public override Type GetValueType => 
        [PropertyOrder(-1)]
        [PreviewInInspector]
        public override Type GetObjectType
        {
            get
            {
                if (_varTag != null)
                    return _varTag.ValueType;
                if (entityProvider != null) return entityProvider.entityTag?._entityType?.RestrictType;
                if (ParentEntity != null) return ParentEntity.GetType();

                Debug.LogError("VarRef: No target entity or variable tag found.", this);
                return typeof(object); // 如果沒有找到目標，返回 object 類型
            }
        }
        
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

            var target = GetTarget();
            // 如果沒有設定欄位路徑，直接回傳變數值
            if (!HasFieldPath)
            {
                return target.Get<T1>();
            }

            // 不選varTag的話就用Entity?
            // 使用欄位路徑存取特定欄位值
            var fieldValue = ReflectionUtility.GetFieldValueFromPath(target, _pathEntries, gameObject);

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
            else
            {
                return default; //null的時候
            }

            Debug.LogError($"VarRef: 轉換失敗 Var:{target}", this);
            return default;
        }


        // protected override string DescriptionTag => "varRef";
    }
}