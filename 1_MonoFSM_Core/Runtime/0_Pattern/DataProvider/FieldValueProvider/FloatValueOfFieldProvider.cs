using MonoFSM.Core.Attributes;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    public class FloatValueOfFieldProvider : MonoBehaviour, IFloatProvider
    {
        [SerializeField] [Auto] private FieldOfVarOfVarProvider _fieldValueProvider;

        public string Description =>
            _fieldValueProvider != null ? _fieldValueProvider?.GetPathString() : "No Field Value Provider";


        [PreviewInInspector] public float Value => _fieldValueProvider?.GetValue<float>() ?? -1f;
    }
}