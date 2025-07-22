using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Utilities;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.DataProvider
{
    public abstract class PropertyOfTypeProvider : AbstractDescriptionBehaviour
    {
        public abstract Type GetObjectType { get; }
        public abstract Type ValueType { get; }
        public object ValueRaw => Get<object>();

        #region Field Path Support

        // [PropertyOrder(0)]
        // [BoxGroup("Field Path", ShowLabel = true)]
        // [InfoBox("選擇欄位路徑編輯模式", InfoMessageType.Info)]
        // [ToggleLeft]
        // public bool UseSimplePathEditor = false;

        //FIXME: 放下面？
        [PropertyOrder(1)]
        [FormerlySerializedAs("pathEntries")]
        [BoxGroup("Field Path", ShowLabel = true)]
        [InfoBox("選擇變數中的特定欄位。留空表示直接使用變數值。", InfoMessageType.Info, nameof(NoFieldPath))]
        // [InfoBox("欄位路徑的最終型別與變數型別不相容", InfoMessageType.Error, nameof(IsFieldPathTypeIncompatible))]
        [OnValueChanged(nameof(OnPathEntriesChanged))]
        [ListDrawerSettings(ShowFoldout = false)]
        // [HideIf(nameof(UseSimplePathEditor))]
        public List<FieldPathEntry> _pathEntries = new();

        //最終值
        protected Type lastPathEntryType => _pathEntries[^1].GetPropertyType();


        //FIXME: 還是不要用吧
        // [PreviewInInspector] [AutoParent] private IIndexInjector _indexInjector;

        // [PreviewInInspector] [Auto] private ITypeRestrict _typeRestrict; //FIXME: 這個是最後一個...hmmm之後在想怎麼處理好了

        private void OnPathEntriesChanged()
        {
            // ReflectionUtility.UpdatePathEntryTypes(_pathEntries, GetVarType, _typeRestrict?.SupportedTypes,
            //     _indexInjector);
            ReflectionUtility.UpdatePathEntryTypes(_pathEntries, GetObjectType);
        }

        protected bool HasFieldPath => _pathEntries is { Count: > 0 };
        private bool NoFieldPath => !HasFieldPath;

        [BoxGroup("Field Path")]
        [HorizontalGroup("Field Path/Buttons")]
        [Button("新增層級")]
        private void AddFieldLevel()
        {
            if (_pathEntries == null)
                _pathEntries = new List<FieldPathEntry>();

            var newEntry = new FieldPathEntry();


            // 如果是第一個項目，預設使用 TVarMonoType 作為起始型別
            if (_pathEntries.Count == 0)
            {
                newEntry.SetSerializedType(GetObjectType);
            }
            // 如果不是第一個項目，則使用前一個項目的型別
            else
            {
                var lastEntry = _pathEntries.Last();
                var lastType = lastEntry._serializedType.RestrictType;
                Debug.Log("Last Type: " + lastType, this);
                newEntry.SetSerializedType(lastType);
            }

            _pathEntries.Add(newEntry);
            OnPathEntriesChanged();
        }

        [HorizontalGroup("Field Path/Buttons")]
        [Button("刪除最後層級")]
        private void RemoveLastFieldLevel()
        {
            if (_pathEntries != null && _pathEntries.Count > 0)
            {
                _pathEntries.RemoveAt(_pathEntries.Count - 1);
                // ReflectionUtility.UpdatePathEntryTypes(_pathEntries, GetVarType, _typeRestrict?.SupportedTypes,
                //     _indexInjector);
                OnPathEntriesChanged();
            }
        }


        protected string FullPropertyPath
        {
            get
            {
                if (!HasFieldPath)
                    return GetObjectType.Name;

                var fieldPath = string.Join(".", _pathEntries.Select(e => e.PropertyPath ?? "未選擇"));
                return $"{GetObjectType.Name}.{fieldPath}";
            }
        }

        [BoxGroup("Field Path")]
        [ShowInInspector]
        [DisplayAsString]
        [LabelText("當前路徑")]
        protected string PropertyPath
        {
            get
            {
                if (!HasFieldPath)
                    return string.Empty;

                return string.Join(".", _pathEntries.Select(e => e.PropertyPath ?? "未選擇"));
            }
        }

        public abstract T1 Get<T1>();
        // private bool IsFieldPathTypeIncompatible()
        // {
        //     return !ReflectionUtility.IsFieldPathTypeCompatible(VarRaw, _pathEntries, ValueType);
        // }

        #endregion
    }

    //不一定有var? IVarProvider
    public abstract class AbstractVariableProviderRef : PropertyOfTypeProvider, IValueProvider
    {
        // public GameFlagBase FinalData => VarRaw?.FinalData;
        //不一定有這個？再切一層？
        public abstract AbstractMonoVariable VarRaw { get; } //還是其實這個也可以？

        //FIXME: get Object? Object Type & ValueType
        // public abstract Type GetValueType { get; }
        

        public abstract VariableTag varTag { get; }
        public abstract TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable;

        public override string ToString()
        {
            return VarRaw?.name;
        }
        
        public override string Description => PropertyPath;


        protected override string DescriptionTag => "Value";
    }
}