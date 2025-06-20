using System;
using MonoFSM.DataProvider;
using MonoFSM.Core.DataProvider;
using UnityEngine;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    public class FloatLiteralRef : MonoBehaviour, IFloatProvider
    {
        //不對啊XDD

        [DropDownRef] public FloatLiteralComp _dropDownRef;

        public float GetFloat()
        {
            return _dropDownRef.GetFloat();
        }

        public float Value => _dropDownRef.GetFloat();
        public Type ValueType => typeof(float);

        public string Description => "DropDownRef: " + _dropDownRef.Description;
    }
}