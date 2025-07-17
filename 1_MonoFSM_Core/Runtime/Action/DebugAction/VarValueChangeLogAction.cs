using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using UnityEngine;

namespace RCGMakerFSMCore.Runtime.Action.DebugAction
{
    public class VarValueChangeLogAction : MonoBehaviour, IVarChangedListener
    {
        [PreviewInInspector] [AutoParent] private AbstractMonoVariable _var;

        private void Awake()
        {
            if (_var == null)
            {
                Debug.LogError("ValueChangeLogAction requires a variable reference.", this);
                return;
            }

            _var.AddListener(this);
        }

        // private void OnValueChanged()
        // {
        //     if (_var == null)
        //     {
        //         Debug.LogError("ValueChangeLogAction: Variable reference is null.", this);
        //         return;
        //     }
        //
        //     // Debug.Log($"ValueChanged {_var.name}: {_var.objectValue}", this);
        // }

        private void OnDestroy()
        {
            if (_var != null)
                _var.RemoveListener(this);
            else
                Debug.LogError("ValueChangeLogAction: Variable reference is null on destroy.", this);
        }

        public void OnVarChanged(AbstractMonoVariable variable)
        {
            if (variable == null)
            {
                Debug.LogError("VarValueChangeLogAction: Variable reference is null.", this);
                return;
            }

            // Log the variable change
            Debug.Log($"Variable '{variable.name}' changed to: {variable.objectValue}", this);
        }
    }
}