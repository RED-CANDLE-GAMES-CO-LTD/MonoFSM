using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.FSM
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
                var data = variableBool.ScriptableData;
                if (data != null && !flags.Contains(data))
                    flags.Add(data);
            }
        }

        public List<GameFlagBase> flags = new();
        public List<VariableBool> variableBools = new();
    }
}