using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM_InputAction
{
    //FIXME: 這顆有點多餘的custom code? 舊規，應該直接去接VarMonoInput?
    public class VarInputAction : AbstractDescriptionBehaviour
    {
        // 直接引用，優先使用
        [HideIf(nameof(_monoInput))] [SerializeField] [DropDownRef]
        MonoInputAction _inputActionRef;

        // Fallback：沒設 direct ref 時，從 VarEntity 透過 varTag 取得承載 MonoInputAction 的 Variable（VarMonoInputAction）
        [FormerlySerializedAs("_monoInputAction")] [SerializeField] [DropDownRef]
        private VarMonoInput _monoInput;

        protected override string DescriptionTag => "VarInput";

        public override string Description
        {
            get
            {
                if (_inputActionRef != null)
                    return _inputActionRef.name;
                return
                    $"{(_monoInput ? _monoInput.name : "?")}";
            }
        }

        public MonoInputAction InputAction =>
            _inputActionRef ? _inputActionRef : _monoInput.Value;
    }
}
