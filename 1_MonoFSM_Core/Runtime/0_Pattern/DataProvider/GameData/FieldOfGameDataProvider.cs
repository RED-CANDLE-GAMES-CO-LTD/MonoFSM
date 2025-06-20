using System;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.DataProvider
{
    //FIXME: 刪掉？
    public class FieldOfGameDataProvider : AbstractFieldOfVarProvider
    {
        [CompRef] [Auto] private IBlackboardProvider _blackboardProvider; //用這個owner去和旁邊要IGameDataProvider？
        [CompRef] [Auto] private IGameDataProvider _monoDescriptableProvider;

        private DescriptableData GetData => _monoDescriptableProvider != null
            ? _monoDescriptableProvider?.GameData
            : _blackboardProvider?.GetComponentOfOwner<IGameDataProvider>()?.GameData; //太瞎了吧XD

        // protected override AbstractMonoVariable ListenToVariable { get; } //不一定是variable啊... 還是乾脆都用？ static
        public override Object targetObject => GetData;
        public override Type targetType => GetData?.GetType();
    }
}