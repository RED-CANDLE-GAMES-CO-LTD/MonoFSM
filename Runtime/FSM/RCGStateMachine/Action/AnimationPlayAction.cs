using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor.Animations;
using UnityEngine;

namespace RCGMaker.Core
{
    public class AnimationPlayAction : AbstractStateAction
    {
        public Animator animator;
        private AnimatorController controller;

        private AnimatorController Controller =>
            controller ??= animator.runtimeAnimatorController as AnimatorController;

        public AnimationClip clip;
        public string stateName;

        protected override void OnStateEnterImplement()
        {
        }

        [Button]
        void ApplyClip()
        {
            if (Controller == null)
                return;

            var state = Controller.layers[0].stateMachine.states.FirstOrDefault(x => x.state.name == stateName);

            if (state.state != null)
                state.state.motion = clip;
        }

        bool IsClipSynced
        {
            get
            {
                if (Controller == null)
                    return false;

                var state = Controller.layers[0].stateMachine.states.FirstOrDefault(x => x.state.name == stateName);

                if (state.state != null)
                    return state.state.motion == clip;
                else
                    return false;
            }
        }
    }
}