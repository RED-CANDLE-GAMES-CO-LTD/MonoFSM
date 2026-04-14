using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using UnityEngine;
using System;
using MonoFSM.Foundation;

namespace Fusion.Addons.KCC.ECM2.Examples.Networking.Fusion_v2.Characters.Scripts.Input
{
    public class BoolToFloatMapping : AbstractValueSource<float>
    {
        public Type ValueType => typeof(float);

        [SerializeField] [DropDownRef] private VarBool _boolVar;

        //FIXME: 做一些防震盪？
        public override float Value => _boolVar?.IsTrue ?? false ? 1.0f : 0.0f;
        public override string Description => _boolVar.Description + " to float (0~1)";
    }
}
