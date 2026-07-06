using System;
using MonoFSM.Core.DataProvider;
using MonoFSM.Variable;

namespace MonoFSM.VarRefOld
{
    /// <summary>
    /// 可以拿到一個VarGameData的MonoBehaviour
    /// </summary>
    [Obsolete("Obsolete")]
    public class VarDescriptableDataRef : VariableProviderRef<VarGameData, GameData>, IGameDataProvider
    {
        public GameData GameData => Value;
    }
}
