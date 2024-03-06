using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable
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
        public VariableFloat CriticalRate;
        public VariableFloat CriticalDamageRate;

        //TODO preview/

        [Button]
        float PreviewOperation(float value)
        {
            return ApplyOperation(value);
        }

        public float ApplyOperation(float value)
        {
            //FIXME: get random state?
            var random = Random.Range(0f, 1f);
            if (random < CriticalRate.FinalValue)
            {
                return value * CriticalDamageRate.FinalValue;
            }

            return value;
        }
    }
}