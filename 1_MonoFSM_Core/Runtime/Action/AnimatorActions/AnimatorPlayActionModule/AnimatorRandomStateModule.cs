using System.Linq;
using MonoFSM.Core;
using Sirenix.OdinInspector;
using Random = UnityEngine.Random;

namespace MonoFSM.AnimatorControl
{
    //想要隨機抽一個動畫來播，用位置來決定random seed
    //Config Setter...
    //Animator誰觸發啊？
    public class AnimatorRandomStateModule : AnimatorPlayActionModule, ISceneSavingCallbackReceiver
    {
        private void OnValidate()
        {
            RandomAssignStateFromPosition();
        }

        [InfoBox("Assign state name from position hash code")]
        [Button]
        private void RandomAssignStateFromPosition()
        {
#if UNITY_EDITOR
            var names = _animatorPlayAction.GetAnimatorStateNames();
            if (names == null)
                return;

            Random.InitState(transform.position.x.GetHashCode() + transform.position.y.GetHashCode() +
                             transform.position.z.GetHashCode());

            var enumerable = names.ToList();
            var index = Random.Range(0, enumerable.Count());
            _animatorPlayAction.stateName = enumerable.ElementAt(index);
#endif
        }

        public void OnBeforeSceneSave()
        {
            RandomAssignStateFromPosition();
        }
    }
}