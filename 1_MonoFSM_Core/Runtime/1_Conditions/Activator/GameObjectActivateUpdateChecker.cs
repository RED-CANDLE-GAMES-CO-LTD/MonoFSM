using MonoFSM.Core.Condition;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._1_Conditions.Activator
{
    /// <summary>
    /// 可以關掉自己嗎？
    /// </summary>
    public class GameObjectActivateUpdateChecker : AbstractConditionUpdateChecker
    {
        [PropertyOrder(-1)]
        [Required]
        public GameObject _target;

        public GameObject[] _additionals;

        protected override void ActivateCheckImplement(bool isValid)
        {
            if (_target == null)
            {
                Debug.LogError("GameObjectActivateChecker: Target is null", this);
                return;
            }

            if (_target.activeSelf != isValid)
                _target.SetActive(isValid);
            foreach (var additional in _additionals)
            {
                if (additional.activeSelf != isValid)
                    additional.SetActive(isValid);
            }
        }
    }
}
