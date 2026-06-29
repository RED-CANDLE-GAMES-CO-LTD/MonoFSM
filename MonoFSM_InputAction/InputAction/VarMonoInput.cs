using MonoFSM.Variable;

namespace MonoFSM_InputAction
{
    // 承載 MonoInputAction 的專屬 Variable，放在 entity 的 VariableFolder
    // 可被 VarEntity → varTag → GetValue<MonoInputAction>() 取得
    public class VarMonoInput : GenericUnityObjectVariable<MonoInputAction>
    {
    }
}
