using MonoFSM_Core.Simulate;
using RCGMaker.Core.Attributes;
using MonoFSM.Variable;
using UnityEngine;

namespace RCGMaker.Runtime.FSM.RCGStateMachine
{
    public class VariableOwner : MonoBehaviour, IVariableOwner, IUpdateSimulate
    {
        //FIXME: 可能有多個？ multiple folder
        [Component] [PreviewInInspector] [AutoChildren]
        private VariableFolder _variableFolder;

        public VariableFolder VariableFolder
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying == false && _variableFolder == null)
                    _variableFolder = GetComponentInChildren<VariableFolder>();
                // Debug.Log("VariableFolder is null, try to find it in children", this);
#endif
                if (Application.isPlaying && _variableFolder == null)
                    Debug.LogError(
                        "VariableFolder is null, please ensure it is assigned in the inspector or added as a child component.",
                        this);
                return _variableFolder;
            }
        }

        //多包一層歐，好蠢
        public AbstractMonoVariable GetVariable(VariableTag varTag)
        {
            return VariableFolder.GetVariable(varTag);
        }

        public AbstractMonoVariable GetVariable(string varTagName)
        {
            return VariableFolder.GetVariable(varTagName);
        }

        public T GetVariable<T>(VariableTag varTag) where T : AbstractMonoVariable
        {
            return VariableFolder.GetVariable<T>(varTag);
        }

        public T GetVariable<T>(string varTagName) where T : AbstractMonoVariable
        {
            return GetVariable(varTagName) as T;
        }

        public void Simulate(float deltaTime)
        {
            
        }

        public void AfterUpdate() //等Simulate都跑完後才CommitValue
        {
            //FIXME: 還是直接給variable folder做就好？
            VariableFolder.CommitVariableValues();
        }
    }
}