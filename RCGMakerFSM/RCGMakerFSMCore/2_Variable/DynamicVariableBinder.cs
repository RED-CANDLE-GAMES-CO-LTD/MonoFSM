using RCGMaker.Core;
using RCGMaker.Runtime.FSM._2_Variable.VariableBinder;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class DynamicVariableBinder : AbstractFolder, ILevelResetStart,IBinder
    {
        // VariableFloat[] variableFloats;
        // VariableFloatVirtual[] variableFloatVirtuals;
        //
        // // public FloatValueSource floatSource1;
        // // public FloatValueSource floatSource2;
        // [Header("When")] public VariableBool V1;
        // [Header("is Set, Also Set")] public VariableBool V2;

        [AutoChildren] [Component] AbstractVariableBindingEntry[] entries;

        // void bind()
        // {
        //     V1.Field.AddListener((value) => { V2.SetValue(value, V1); }, this);
        // }

        // void Bind()
        // {
        //     //應該在這裡用entry宣告而不是在下面事先綁好？
        //     foreach (var variableFloat in variableFloats)
        //     {
        //         foreach (var variableFloatVirtual in variableFloatVirtuals)
        //         {
        //             if (variableFloatVirtual.VarType == variableFloat.VarType)
        //             {
        //                 variableFloatVirtual.variableFloat = variableFloat;
        //             }
        //         }
        //     }
        // }

        // public void LevelResetPrepareRuntimeData() //太早了，被清掉了？
        // {
        //     // Bind();
        //     // bind();
        //     foreach (var entry in entries)
        //     {
        //         entry.Bind();
        //     }
        // }

        public void LevelResetStart()
        {
            foreach (var entry in entries)
            {
                entry.Bind();
            }
        }
    }
}