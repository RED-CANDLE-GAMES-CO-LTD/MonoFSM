using UnityEngine;

using Sirenix.OdinInspector;

namespace MonoFSM.Variable
{
    //Operation?
    //變動的，input不同就可能有不同的output
    //內部也有狀態


    //modifier是functional的，可以直接從baseValue得到finalValue，內部沒有狀態
    public class CriticalHitDamageOperation : MonoBehaviour, IVariableFloatOperation //乘區
    {
        //要抽象到裝個type就結束了？還是要接到一個假的實體
        //假的實體會有同時出現兩個的混淆問題，搜尋...  
        //VariableDictionary中不可以出現同樣type?
        public VarFloat CriticalRate;
        public VarFloat CriticalDamageRate;

        //TODO preview/

        [Button]
        private float PreviewOperation(float value) 
            => ApplyOperation(value);

        public float ApplyOperation(float value) 
            //FIXME: get random state?
            => Random.Range(0f, 1f) < CriticalRate.FinalValue 
                ? value * CriticalDamageRate.FinalValue 
                : value;
    }
}