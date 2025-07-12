using MonoFSM.Variable;

namespace MonoFSM.Core.DataProvider
{
    /// <summary>
    /// Provide a reference to a VarFloat.
    /// </summary>
    public class VarFloatProviderRef : VariableProviderRef<VarFloat, float> //不該是IFloatProvider?
    {
        //直接在這選Field?
        //如果要string呢？
        
        //FIXME: 蛤？
        //可以拿field?
        // [CompRef] [Auto] private AbstractFieldOfVarProvider
        //     _fieldValueProvider; //這個是VarFloat的FieldValueProvider嗎？還是VarFloat本身的FieldValueProvider?
        //
        // public override string Description =>
        //     _fieldValueProvider != null && _fieldValueProvider != this
        //         ? _fieldValueProvider.GetPathString()
        //         : base.Description;
    }

    //可以再往下拿？ 我提供float，如果要拿我的某個property (ex: max, min)
    //情境一：監聽VarFloat變化，拿VarFloat.Max來更新 (從AbstractFieldValueProvider那邊走
    //情境二：我要拿一個值，
}