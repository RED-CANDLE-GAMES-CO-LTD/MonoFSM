using MonoFSM.Core.Detection;
using MonoFSM.Core.Runtime.Action;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;

namespace _1_MonoFSM_Core.Runtime.EffectHit.Action
{
    /// <summary>
    ///     自己控制更新的時間
    /// </summary>
    public class ManualEffectDetectAction : AbstractStateAction, ISceneAwake
    {
        [Required]
        [DropDownRef]
        public EffectDetector _effectDetector;

        protected override void OnActionExecuteImplement()
        {
            _effectDetector.DetectUpdateCheck();
        }

        public void EnterSceneAwake()
        {
            _effectDetector._manualEffectDetectAction = this;
        }
    }
}
