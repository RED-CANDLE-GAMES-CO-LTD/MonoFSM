using System;
using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM.Core.Variable.Providers
{
    public class VarListCountProvider : AbstractValueSource<float>, IValueProvider<float>
    {
        [DropDownRef] [SerializeField] private AbstractVarList _varList;
        public override string Description => $"{_varList?.name}'s Count";
        public override float Value => _varList.Count;
    }
}
