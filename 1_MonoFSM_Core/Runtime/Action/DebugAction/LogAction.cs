using MonoFSM.Core.Runtime.Action;
using UnityEngine;
namespace RCGMakerFSMCore.Runtime.Action.DebugAction
{
    public class LogAction : AbstractStateAction
    {
        public string _logMessage = "LogAction";
        public bool _isLogInProvider = false;

        protected override void OnActionExecuteImplement()
        {
            if (_isLogInProvider)
                this.Log(_logMessage);
            else
                Debug.Log(_logMessage, this);
        }
    }
}