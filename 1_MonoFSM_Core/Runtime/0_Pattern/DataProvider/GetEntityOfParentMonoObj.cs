using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;

namespace _1_MonoFSM_Core.Runtime._0_Pattern.DataProvider
{
    public class GetEntityOfParentMonoObj : AbstractValueSource<MonoEntity>
    {
        public VarEntity _varEntity;
        public override MonoEntity Value => _varEntity?.Value?.ParentEntity;
        public override string Description => _varEntity?.Description + "'s Parent";
    }
}
