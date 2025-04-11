using System.Linq;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace MonoFSM.Variable
{
    public class VariableRelayBinder:MonoBehaviour,IVariableOwner
    {
        [Component] [PreviewInInspector]
        [AutoChildren]
        private VarBoolRelay[] _variableRelays;
        
        [PreviewInInspector] [AutoChildren]
        RCGVariableFolder[] _variableFolders;
        public RCGVariableFolder VariableFolder => _variableFolders.FirstOrDefault();
        public AbstractMonoVariable GetVariable(VariableTag varTag)
        {
            foreach (var folder in _variableFolders)
            {
                if (folder.ContainsKey(varTag))
                {
                    return folder.Get(varTag);
                }
            }
            Debug.LogError($"Variable {varTag} not found in any folder", this);
            return null;
        }
    }
}