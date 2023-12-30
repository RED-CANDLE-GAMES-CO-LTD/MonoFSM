using System;
using System.Linq;
using RCGMaker.Core;
using Sirenix.OdinInspector;
using Random = UnityEngine.Random;

namespace RCGFSM.AnimatorControl
{
    //想要隨機抽一個動畫來播，用位置來決定random seed
    public class AnimatorRandomStateModule : AnimatorPlayActionModule, ISceneSavingCallbackReceiver
    {

        private void OnValidate()
        {
            AssignFromPosition();
        }

        [InfoBox("Assign state name from position hash code")]
        [Button]
        private void AssignFromPosition()
        {
#if UNITY_EDITOR
            var names = animatorPlayAction.GetAnimatorStateNames();
            if (names == null)
                return;

            Random.InitState(transform.position.x.GetHashCode() + transform.position.y.GetHashCode() +
                             transform.position.z.GetHashCode());

            var enumerable = names.ToList();
            var index = Random.Range(0, enumerable.Count());
            animatorPlayAction.stateName = enumerable.ElementAt(index);
            #endif
        }

        public void OnBeforeSceneSave()
        {
            AssignFromPosition();
        }

    }

}