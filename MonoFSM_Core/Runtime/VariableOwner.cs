using RCGMaker.Core.Attributes;
using MonoFSM.Variable;
using UnityEngine;

namespace RCGMaker.Runtime.FSM.RCGStateMachine
{
    public class VariableOwner : MonoBehaviour, IVariableOwner
    {
        //FIXME: 可能有多個？ multiple folder
        [Component] [PreviewInInspector] [AutoChildren]
        RCGVariableFolder _variableFolder;

        public RCGVariableFolder VariableFolder
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying == false && _variableFolder == null)
                {
                    _variableFolder = GetComponentInChildren<RCGVariableFolder>();
                    // Debug.Log("VariableFolder is null, try to find it in children", this);
                }
#endif
                return _variableFolder;
            }
        }

        public AbstractMonoVariable GetVariable(VariableTag varTag)
        {
            return VariableFolder.GetVariable(varTag);
        }

        public AbstractMonoVariable GetVariable(string varTagName)
        {
            return VariableFolder.GetVariable(varTagName);
        }
    }
}