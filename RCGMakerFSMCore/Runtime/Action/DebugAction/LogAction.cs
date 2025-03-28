using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMakerFSMCore.Runtime.Action.DebugAction
{
    public class LogAction: AbstractStateAction
    {
        public string _logMessage = "LogAction";
        public bool _isLogInProvider = false;
        protected override void OnStateEnterImplement()
        {
            if(_isLogInProvider)
                this.Log(_logMessage);
            else
                Debug.Log(_logMessage, this);
        }
    }
}