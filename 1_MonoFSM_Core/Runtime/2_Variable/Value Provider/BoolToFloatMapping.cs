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

        // public T GetValue<T>()
        // {
        //     if (typeof(T) == typeof(bool))
        //         return (T)(object)_boolVar.IsTrue;
        //     if (typeof(T) == typeof(float))
        //         return (T)(object)(_boolVar.IsTrue ? 1.0f : 0.0f);
        //     return default;
        // }

        // public string GetDescription()
        // {
        //     return GetFloat().ToString();
        // }
        //
        // public float GetFloat()
        // {
        //     //FIXME: lerp? 應該用這個做嗎？
        //     return _boolVar.IsTrue ? 1.0f : 0.0f;
        // }

        // public string Description { get; }
    }
}
