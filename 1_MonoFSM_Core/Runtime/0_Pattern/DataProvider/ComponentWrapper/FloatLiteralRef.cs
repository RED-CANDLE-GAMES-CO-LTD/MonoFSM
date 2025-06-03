using MonoFSM.DataProvider;
using RCGMaker.Core.DataProvider;
using UnityEngine;

namespace MonoFSM_Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    public class FloatLiteralRef : MonoBehaviour, IFloatProvider
    {
        //不對啊XDD

        [DropDownRef] public FloatLiteralComp _dropDownRef;

        public float GetFloat()
        {
            return _dropDownRef.GetFloat();
        }

        public object GetValue()
        {
            return GetFloat();
        }

        public string Description => "DropDownRef: " + _dropDownRef.Description;
    }
}