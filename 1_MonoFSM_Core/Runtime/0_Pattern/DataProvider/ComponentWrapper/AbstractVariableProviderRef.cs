using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.DataProvider
{
    public abstract class AbstractVariableProviderRef : MonoBehaviour, IValueProvider
    {
        // public GameFlagBase FinalData => VarRaw?.FinalData;
        public abstract AbstractMonoVariable VarRaw { get; } //還是其實這個也可以？

        // public abstract Type GetValueType { get; }
        public abstract Type GetVarType { get; }

        public abstract Type ValueType { get; }
        public abstract VariableTag varTag { get; }
        public abstract TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable;

        public override string ToString()
        {
            return VarRaw?.name;
        }

        public abstract T1 Get<T1>();


        public abstract string Description { get; }


        #region Field Path Support

        //FIXME: 放下面？
        [PropertyOrder(1)]
        [FormerlySerializedAs("pathEntries")]
        [BoxGroup("Field Path", ShowLabel = true)]
        [InfoBox("選擇變數中的特定欄位。留空表示直接使用變數值。", InfoMessageType.Info, nameof(NoFieldPath))]
        // [InfoBox("欄位路徑的最終型別與變數型別不相容", InfoMessageType.Error, nameof(IsFieldPathTypeIncompatible))]
        [OnValueChanged(nameof(OnPathEntriesChanged))]
        [ListDrawerSettings(ShowFoldout = false)]
        public List<FieldPathEntry> _pathEntries = new();

        protected Type lastPathEntryType => _pathEntries[^1].GetPropertyType();


        [PreviewInInspector] [AutoParent] private IIndexInjector _indexInjector;

        // [PreviewInInspector] [Auto] private ITypeRestrict _typeRestrict; //FIXME: 這個是最後一個...hmmm之後在想怎麼處理好了

        private void OnPathEntriesChanged()
        {
            // ReflectionUtility.UpdatePathEntryTypes(_pathEntries, GetVarType, _typeRestrict?.SupportedTypes,
            //     _indexInjector);
            ReflectionUtility.UpdatePathEntryTypes(_pathEntries, GetVarType);
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
                newEntry.SetSerializedType(GetVarType);
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

        // [BoxGroup("Field Path")]
        // [Button("驗證欄位路徑")]
        // private void ValidateFieldPath()
        // {
        //     if (!HasFieldPath)
        //     {
        //         Debug.Log("無欄位路徑需要驗證", this);
        //         return;
        //     }
        //
        //     ReflectionUtility.UpdatePathEntryTypes(pathEntries, GetVarType, _typeRestrict?.SupportedTypes,
        //         _indexInjector);
        //     var result = ReflectionUtility.GetFieldValueFromPath(VarRaw, pathEntries, gameObject);
        //
        //     if (result == null)
        //     {
        //         Debug.LogWarning("欄位路徑回傳 null 值", this);
        //         return;
        //     }
        //
        //     var resultType = result.GetType();
        //     if (typeof(TValueType).IsAssignableFrom(resultType))
        //         Debug.Log($"✓ 欄位路徑驗證成功: {resultType} 可以轉換為 {typeof(TValueType)}", this);
        //     else
        //         Debug.LogError($"✗ 型別不相容: {resultType} 無法轉換為 {typeof(TValueType)}", this);
        // }

        [BoxGroup("Field Path")]
        [ShowInInspector]
        [DisplayAsString]
        [LabelText("當前路徑")]
        private string CurrentFieldPath
        {
            get
            {
                if (!HasFieldPath) return "無欄位路徑 (直接使用變數值)";

                var varName = VarRaw?.name ?? "Variable";
                var fieldPath = string.Join(".", _pathEntries.Select(e => e.fieldName ?? "未選擇"));
                return $"{varName}.{fieldPath}";
            }
        }

        private bool IsFieldPathTypeIncompatible()
        {
            return !ReflectionUtility.IsFieldPathTypeCompatible(VarRaw, _pathEntries, ValueType);
        }

        #endregion
    }
}