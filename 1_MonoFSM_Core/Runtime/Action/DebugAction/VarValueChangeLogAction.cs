using MonoFSM.Variable;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMakerFSMCore.Runtime.Action.DebugAction
{
    public class VarValueChangeLogAction : MonoBehaviour
    {
        [PreviewInInspector] [AutoParent] private AbstractMonoVariable _var;

        private void Awake()
        {
            if (_var == null)
            {
                Debug.LogError("ValueChangeLogAction requires a variable reference.", this);
                return;
            }

            _var.OnValueChangedRaw += OnValueChanged;
        }

        private void OnValueChanged()
        {
            if (_var == null)
            {
                Debug.LogError("ValueChangeLogAction: Variable reference is null.", this);
                return;
            }

            // Debug.Log($"ValueChanged {_var.name}: {_var.objectValue}", this);
        }

        private void OnDestroy()
        {
            if (_var != null) _var.OnValueChangedRaw -= OnValueChanged;
        }
    }
}