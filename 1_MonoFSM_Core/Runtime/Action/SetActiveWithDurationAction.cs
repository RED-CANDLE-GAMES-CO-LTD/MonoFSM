using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    /// <summary>
    /// 打開 target GameObject，等 _duration 秒後關掉。Local-only visual/SFX 用。
    /// 重複觸發時會取消上一次的關閉排程，重新計時。
    /// </summary>
    public class SetActiveWithDurationAction : AbstractStateAction
    {
        [Required]
        [SerializeField]
        private GameObject _target;

        [SerializeField]
        private float _duration = 1f;

        private Tween _pendingClose;

        protected override void OnActionExecuteImplement()
        {
            if (_target == null) return;

            _pendingClose.Stop();
            _target.SetActive(true);

            _pendingClose = Tween.Delay(this, _duration, t =>
            {
                if (t._target != null)
                    t._target.SetActive(false);
            });
        }
    }
}
