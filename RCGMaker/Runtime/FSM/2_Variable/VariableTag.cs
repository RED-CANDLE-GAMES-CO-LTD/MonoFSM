using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    [CreateAssetMenu(menuName = "RCG/VariableTag")]
    public class VariableTag : ScriptableObject//, IFloatValue
    {
        //FIXME: 下拉式巢狀分類: 
        
        //可以DI標記variable類型，像是血量？要降低對方的血量之類的
        // public float FinalValue => 1; 
        //FIXME: 為什麼要IFloatValue？
    }
}