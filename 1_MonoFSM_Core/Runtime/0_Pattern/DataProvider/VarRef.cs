using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Utilities;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    //var local ref? 
    public class VarRef : AbstractVariableProviderRef, IValueProvider
    {
        //FIXME: 這裡自帶 field entry就可以找到任何東西了？
        [PropertyOrder(-1)]
        [DropDownRef] [SerializeField] private AbstractMonoVariable _monoVariable;

        public override AbstractMonoVariable VarRaw => _monoVariable;

        [PreviewInInspector]
        public override Type ValueType => !HasFieldPath ? _monoVariable?.ValueType : lastPathEntryType;

        // public override Type GetValueType => 
        public override Type GetVarType => _monoVariable?.GetType();
        public override VariableTag varTag => _monoVariable?._varTag;

        public override TVariable GetVar<TVariable>()
        {
            if (_monoVariable is TVariable variable) return variable;
            throw new InvalidCastException($"Cannot cast {_monoVariable.GetType()} to {typeof(TVariable)}");
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

            throw new InvalidCastException(
                $"無法將欄位值 {fieldValue} (型別: {fieldValue.GetType()}) 轉換為 {typeof(T1)}");
        }

        
        public override string Description => _monoVariable != null ? _monoVariable.ToString() : "VarRef is null";
    }
}