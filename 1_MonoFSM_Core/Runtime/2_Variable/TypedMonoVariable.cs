using System.Collections.Generic;
using MonoFSM.Core;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable
{
    public abstract class TypedMonoVariable<T> : AbstractMonoVariable //, ISettable<T>
    {
        protected override bool HasError()
        {
            //_valueSources / valueSource / HasValueSource / IsNeedValueSourceButNone 已上移到 AbstractMonoVariable
            return base.HasError() || IsNeedValueSourceButNone();
        }

        [ShowInInspector]
        public override string Description =>
            HasValueSource ? valueSource?.Description : base.Description;

        public abstract void CommitValue();

        /// <summary>
        /// 在已知型別 <typeparamref name="T" /> 的這一層比較兩個 Variable 的 Value。
        /// 型別相符時 <see cref="AbstractMonoVariable.GetValue{T}" /> 走 Unsafe.As / reference 判斷，不會裝箱；
        /// 再交給 <see cref="EqualityComparer{T}" /> 比較，全程無轉型。
        /// </summary>
        public override bool EqualsVar(AbstractMonoVariable other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(other, this))
                return true;
            //型別不同直接視為不相等，避免用錯誤型別重新詮釋記憶體
            if (other.ValueType != ValueType)
                return false;
            return EqualityComparer<T>.Default.Equals(GetValue<T>(), other.GetValue<T>());
        }
    }
}
