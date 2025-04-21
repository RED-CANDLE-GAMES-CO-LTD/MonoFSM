using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    //FIXME: 玩惹九日有用到...
    public class MonoVarFloatVirtual : VarFloat //這個是不是沒有屁用, 還是純屬拿來rebind?
    {
        //要標注等等才會有嗎？

        [FormerlySerializedAs("_monoVariableFloat")] [FormerlySerializedAs("variableFloat")]
        public VarFloat _monoVarFloat;

        public override float FinalValue 
            => _monoVarFloat 
                ? _monoVarFloat.CurrentValue 
                : 0; //用接過來的變數
    }
}