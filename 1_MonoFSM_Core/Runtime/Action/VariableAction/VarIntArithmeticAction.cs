using System;
using jerryee.UnityMCP;
using MonoFSM.Core.Runtime.Action;
using Sirenix.OdinInspector;

namespace MonoFSM.Variable
{
    [Serializable]
    public class IntValueWrapper
    {
        public enum SelectMode
        {
            Const,
            SimpleVar,
            Provider
        }

        public SelectMode _mode;

        //const, simple var, complex
        [ShowIf("@_mode == SelectMode.Const")] public int _value;

        [ShowIf("@_mode == SelectMode.SimpleVar")] [DropDownRef]
        public VarInt _variable;
    }
// [Obsolete]
    // public class VarIntArithmeticAction : AbstractStateAction
    // {
    //     public override string Description => target?._varTag?.name + " " + Arithmetic + " " + Value;
    //     [MCPExtractable] [DropDownRef] public VarInt target;
    //     [MCPExtractable] public ArithmeticOperator Arithmetic;
    //
    //     [MCPExtractable] public int Value; //FIXME: 需要DI? 
    //
    //     // public IntValueWrapper operand2;
    //     protected override void OnStateEnterImplement()
    //     {
    //         this.Log("Arithmetic: ", Arithmetic, " Value: ", Value);
    //         switch (Arithmetic)
    //         {
    //             case ArithmeticOperator.Add:
    //                 target.SetValue(target.CurrentValue + Value, this);
    //                 break;
    //             case ArithmeticOperator.Sub:
    //                 target.SetValue(target.CurrentValue - Value, this);
    //                 break;
    //             case ArithmeticOperator.Mul:
    //                 target.SetValue(target.CurrentValue * Value, this);
    //                 break;
    //             case ArithmeticOperator.Div:
    //                 target.SetValue(target.CurrentValue / Value, this);
    //                 break;
    //         }
    //     }
    // }
}