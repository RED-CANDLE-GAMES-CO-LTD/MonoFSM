using System;
using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM.Core.Variable.Providers
{
    // [Obsolete("Use VarListCountIntProvider instead; wrap with IntToFloatValueSource if float is needed.")]
    // public class VarListCountProvider : AbstractValueSource<float>, IValueProvider<float>
    // {
    //     [DropDownRef] [SerializeField] private AbstractVarList _varList;
    //     public override string Description => $"{_varList?.name}'s Count";
    //     public override float Value => _varList?.Count ?? -1;
    // }

    public class VarListCountSource : AbstractValueSource<int>, IValueProvider<int>
    {
        [DropDownRef] [SerializeField] private AbstractVarList _varList;
        public override string Description => $"{_varList?.name}'s Count";
        public override int Value => _varList?.Count ?? -1;
    }
}
