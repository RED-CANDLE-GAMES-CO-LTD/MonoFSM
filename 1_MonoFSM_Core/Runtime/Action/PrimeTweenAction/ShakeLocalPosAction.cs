using PrimeTween;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.PrimeTweenAction
{
    public class ShakeLocalPosAction : AbstractStateAction
    {
        [SerializeField] private Transform _target;
        private Tween _shakeTween;

        protected override void OnActionExecuteImplement()
        {
            _shakeTween.Stop();
            _shakeTween = Tween.ShakeLocalPosition(_target,
                new Vector3(0.1f, 0.1f, 0.1f), 0.5f);
        }
    }
}