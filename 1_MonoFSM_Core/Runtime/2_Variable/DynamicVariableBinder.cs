using MonoFSM.Variable.VariableBinder;
using MonoFSMCore.Runtime.LifeCycle;

namespace MonoFSM.Variable
{
    public class DynamicVariableBinder : AbstractFolder, IResetStart,IBinder
    {
        [AutoChildren] [Component] AbstractVariableBindingEntry[] entries;

        public void ResetStart()
        {
            foreach (var entry in entries)
            {
                entry.Bind();
            }
        }
    }
}