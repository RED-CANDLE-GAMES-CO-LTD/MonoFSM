using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class DynamicVariableBinder : MonoBehaviour, ILevelResetPrepare
    {
        VariableFloat[] variableFloats;
        VariableFloatVirtual[] variableFloatVirtuals;

        void Bind()
        {
            //應該在這裡用entry宣告而不是在下面事先綁好？
            foreach (var variableFloat in variableFloats)
            {
                foreach (var variableFloatVirtual in variableFloatVirtuals)
                {
                    if (variableFloatVirtual.VarType == variableFloat.VarType)
                    {
                        variableFloatVirtual.variableFloat = variableFloat;
                    }
                }
            }
        }

        public void LevelResetPrepareRuntimeData()
        {
            Bind();
        }
    }
}