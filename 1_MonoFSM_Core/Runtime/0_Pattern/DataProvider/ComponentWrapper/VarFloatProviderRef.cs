using System.Globalization;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSM.Core.DataProvider;
using UnityEngine;

namespace MonoFSM.DataProvider
{
    /// <summary>
    /// Provide a reference to a VarFloat.
    /// </summary>
    public class VarFloatProviderRef : VariableProviderRef<VarFloat, float>, IFloatProvider //不該是IFloatProvider?
    {
        public float GetFloat()
        {
            if (_fieldValueProvider)
                return _fieldValueProvider.GetValue<float>();
            return Value;
        }

        //可以拿field?
        [CompRef] [Auto] private AbstractFieldOfVarProvider
            _fieldValueProvider; //這個是VarFloat的FieldValueProvider嗎？還是VarFloat本身的FieldValueProvider?

        //description?
        //override value? fieldValue?
        public override string Description =>
            _fieldValueProvider != null ? _fieldValueProvider.GetPathString() : base.Description;
    }

    //可以再往下拿？ 我提供float，如果要拿我的某個property (ex: max, min)
    //情境一：監聽VarFloat變化，拿VarFloat.Max來更新 (從AbstractFieldValueProvider那邊走
    //情境二：我要拿一個值，
}