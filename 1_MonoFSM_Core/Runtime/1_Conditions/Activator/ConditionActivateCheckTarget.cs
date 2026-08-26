using MonoFSM.Core;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._1_Conditions.Activator
{
    public class ConditionActivateCheckTarget : MonoBehaviour, IActivateCheckTarget
    {
        [AutoNested] public ConditionGroup _conditionFolder;

        public bool IsValid => _conditionFolder.IsValid;
    }
}
