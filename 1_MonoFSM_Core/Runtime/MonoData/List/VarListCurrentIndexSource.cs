using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM.Core.Variable.Providers
{
    /// <summary>
    /// 提供 VarList 目前的 current index（游標位置）。
    /// </summary>
    public class VarListCurrentIndexSource : AbstractValueSource<int>, IValueProvider<int>
    {
        [DropDownRef] [SerializeField] private AbstractVarList _varList;
        public override string Description => $"{_varList?.name}'s CurrentIndex";
        public override int Value => _varList?.CurrentIndex ?? -1;
    }
}
