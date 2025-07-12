using MonoFSM.Core;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._3_FlagData
{
    public class MonoSOConfig : ScriptableObject, ISceneSavingCallbackReceiver, ISceneSavingAfterCallbackReceiver
    {
        public virtual void OnBeforeSceneSave()
        {
        }

        public virtual void OnAfterSceneSave()
        {
        }
    }
}