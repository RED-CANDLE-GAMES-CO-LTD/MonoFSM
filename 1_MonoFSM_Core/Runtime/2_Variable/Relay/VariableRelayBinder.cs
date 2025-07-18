using System.Linq;

using UnityEngine;

using MonoFSM.Core.Attributes;

namespace MonoFSM.Variable
{
    public class VariableRelayBinder : MonoBehaviour, IVarBlackboard
    {
        [Component] [PreviewInInspector] [AutoChildren]
        private VarBoolRelay[] _variableRelays;

        [PreviewInInspector] [AutoChildren] private VariableFolder[] _variableFolders;
        public VariableFolder VariableFolder => _variableFolders.FirstOrDefault();

        public AbstractMonoVariable GetVar(VariableTag varTag)
        {
            foreach (var folder in _variableFolders)
                if (folder.ContainsKey(varTag))
                    return folder.Get(varTag);

            Debug.LogError($"Variable {varTag} not found in any folder", this);
            return null;
        }
    }
}