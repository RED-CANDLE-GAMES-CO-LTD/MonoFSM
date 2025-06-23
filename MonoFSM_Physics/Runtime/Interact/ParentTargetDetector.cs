using System;
using Cysharp.Threading.Tasks;
using MonoFSM.Core.Detection;
using MonoFSM.Runtime.Interact.EffectHit;
using UnityEngine;

namespace MonoFSM.Core
{
    public class ParentTargetDetector : AbstractDetector
    {
        private EffectDetectable _parentDetectable;

        private void OnEnable()
        {
            OnEnableNextFrame().Forget();
        }

        private async UniTaskVoid OnEnableNextFrame()
        {
            //FIXME: 應該要等多久？等到LateUpdate？還是等到下一幀？
            await UniTask.WaitForEndOfFrame();
            _parentDetectable = GetComponentInParent<EffectDetectable>();
            if (_parentDetectable == null)
                Debug.LogError("ParentTargetDetector requires an EffectDetectable component in the parent hierarchy.",
                    this);

            OnDetectEnter(_parentDetectable.gameObject);
        }


        protected override void OnDisableImplement()
        {
            if (_parentDetectable != null)
                OnDetectExit(_parentDetectable.gameObject);
        }

        protected override void SetLayerOverride()
        {
        }
    }
}