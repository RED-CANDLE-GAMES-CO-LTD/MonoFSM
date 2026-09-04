using MonoFSM.Core.Detection;
using MonoFSM.Core.Runtime.Action;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.EffectHit.Action
{
    /// <summary>
    ///     手動控制 EffectDetector 的判定時機：掛上這顆之後 detector 就不自己每 tick 判，
    ///     只在這個 action 執行的那一刻判一次。
    ///     用在「按下才發一發」的道具（ex: 遙控器的 d_UseGrabbingItem）。
    ///     必須成對掛：EffectEnterNode 放一顆 _isClearDetection=false 的開判，
    ///     EffectExitNode 放一顆 _isClearDetection=true 的收乾淨；只掛前者的話重疊狀態會殘留，
    ///     exit 永遠不發、第二次執行只會走 Stay 而不是 Enter。
    /// </summary>
    public class ManualEffectDetectAction : AbstractStateAction, ISceneAwake
    {
        [Required]
        [DropDownRef]
        public EffectDetector _effectDetector;

        [Tooltip("勾起來 = 這顆是收尾用的：執行時把還在重疊的全部補發 exit 並清空，" +
                 "而不是跑一次偵測。掛在 EffectExitNode 上。")]
        [SerializeField]
        private bool _isClearDetection;

        public override string Description =>
            (_isClearDetection ? "Clear Detect: " : "Manual Detect: ")
            + (_effectDetector != null ? _effectDetector.name : "?");

        protected override void OnActionExecuteImplement()
        {
            if (_isClearDetection)
                _effectDetector.ClearAllDetections("ManualEffectDetectAction:" + name);
            else
                _effectDetector.DetectUpdateCheck();
        }

        public void EnterSceneAwake()
        {
            //只有開判的那顆負責把 detector 切成手動；收尾用的那顆不該單獨讓 detector 停止自動判定
            if (_isClearDetection)
                return;
            _effectDetector._manualEffectDetectAction = this;
        }
    }
}
