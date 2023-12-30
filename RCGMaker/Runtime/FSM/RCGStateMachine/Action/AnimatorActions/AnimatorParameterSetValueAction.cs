using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGFSM.Animation
{
    public class AnimatorParameterSetValueAction : AbstractStateAction
    {
        public Animator animator;

        [ValueDropdown(nameof(GetParameterNames))]
        public string ParameterName;

        public bool value;

        private IEnumerable<string> GetParameterNames()
        {
            var parameters = animator.parameters;
            foreach (var parameter in parameters)
            {
                yield return parameter.name;
            }
        }

        protected override void OnStateEnterImplement()
        {
            animator.SetBool(ParameterName, value);
        }
    }
}