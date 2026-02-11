using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM_InputAction
{
    //還是應該用 VarComp?
    public class VarInputAction : AbstractDescriptionBehaviour
    {
        [DropDownRef] public MonoInputAction _inputActionRef;
        protected override string DescriptionTag => "VarInput";
    }
}
