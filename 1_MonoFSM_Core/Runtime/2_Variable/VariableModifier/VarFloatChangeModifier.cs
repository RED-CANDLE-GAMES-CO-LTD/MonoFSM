using UnityEngine;

namespace MonoFSM.Variable
{
    //priority比較前面？後面才被bound?
    public class VarFloatChangeModifier : MonoBehaviour, AbstractVariableModifier<float>
    {
        [SerializeField] VarFloatWrapper _rateVar;
        // public bool _decreaseOnly = true;

        public float BeforeSetValueModifyCheck(float value, float currentValue)
        {
            return value;
            var diff = value - currentValue;
            var actual = diff * _rateVar.Value; //???
            Debug.Log(
                $"[VarFloatChangeModifier] BeforeSetValueModifyCheck: value={value}, currentValue={currentValue}, diff={diff}, actual={actual}, rate={_rateVar.Value}",
                this);
            return value + actual; //TODO; 考慮descreasOnly
        }

        public float AfterGetValueModifyCheck(float value)
        {
            // throw new System.NotImplementedException();
            return value;
        }

        public float ProcessDelta(float delta)
        {
            return delta * _rateVar.Value;
        }
    }
}
