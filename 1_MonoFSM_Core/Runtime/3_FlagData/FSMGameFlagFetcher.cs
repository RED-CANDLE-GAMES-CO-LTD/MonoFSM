using System.Collections.Generic;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.FSM
{
    public class FSMGameFlagFetcher : MonoBehaviour
    {
        [Button]
        private void FetchFlagUnderGameObject()
        {
            flags.Clear();
            variableBools.Clear();
            GetComponentsInChildren(variableBools);

            foreach (var variableBool in variableBools)
            {
                var data = variableBool.BindData;
                if (data != null && !flags.Contains(data))
                    flags.Add(data);
            }
        }

        public List<GameFlagBase> flags = new();
        public List<VarBool> variableBools = new();
    }
}