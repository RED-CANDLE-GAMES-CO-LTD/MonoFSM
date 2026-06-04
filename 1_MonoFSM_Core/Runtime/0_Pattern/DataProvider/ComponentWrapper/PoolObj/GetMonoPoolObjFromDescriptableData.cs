using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Core.DataProvider.ComponentWrapper
{
    [Obsolete("用GetBindPrefabFromGameData")]
    public class GetMonoPoolObjFromDescriptableData : AbstractValueSource<MonoObj>,
        ICompProvider<MonoObj>
    {
        [CompRef] [Auto] private IGameDataProvider _gameDataProvider;

        public string Description => "Get MonoPoolObj from DescriptableData";

        [ShowInPlayMode]
        public MonoObj Get()
        {
            return _gameDataProvider.GameData.bindPrefab;
        }

        Component ICompProvider.Get()
        {
            return Get();
        }

        public override MonoObj Value => Get();

        public T1 Get<T1>()
        {
            if (typeof(T1) != typeof(MonoObj))
            {
                Debug.LogError(
                    $"GetMonoPoolObjFromDescriptableData: Type mismatch. Requested {typeof(T1)}, but this provider only supports MonoObj.",
                    this
                );
                return default;
            }

            return (T1)(object)Get();
        }

        public Type ValueType => typeof(MonoObj);
    }
}
