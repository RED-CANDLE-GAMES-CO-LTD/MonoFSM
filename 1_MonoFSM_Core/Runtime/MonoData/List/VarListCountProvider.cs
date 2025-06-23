using MonoFSM.Core.DataProvider;
using UnityEngine;

namespace MonoFSM.Core.Variable.Providers
{
    public class VarListCountProvider : MonoBehaviour, IValueProvider<float>
    {
        [DropDownRef] [SerializeField] private AbstractVarList _varList;
        public string Description => $"{_varList?.name}'s Count";
        public float Value => _varList.Count;
    }
}