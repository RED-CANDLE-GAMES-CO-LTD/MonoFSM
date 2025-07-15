using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Runtime.Action;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;


//自動生成animator controller
//把Clip塞到state上
namespace MonoFSM.Core
{
    public class AnimationPlayAction : AbstractStateAction, IAnimatorStateProvider, IAnimatorPlayAction
    {
        public Animator BindAnimator => animator;
        int IAnimatorStateProvider.StateLayer => layer;

        public string StateName => Clip.name; //stateName;

        [TabGroup("Test")]
        [Button]
        private void TestPlay()
        {
            animator.Play(stateName, 0, 0);
        }

        [TabGroup("Animator")] public Animator animator;


        [InfoBox("Not Sync", InfoMessageType.Warning, "IsClipNotSynced")] [SerializeField]
        private AnimationClip _clip;

        public AnimationClip Clip => _clip;

        // [OnValueChanged("FetchClipCheck")] [ValueDropdown("GetAllStateNames")]

        //TODO: from parent?
        public string stateName;

        public int layer = 0; //FIXME: 會需要layer嗎？


        protected override void OnActionExecuteImplement()
        {
            animator.Play(StateName);
        }

        // [Button]
        // void ApplyClip()
        // {
        //     if (Controller == null)
        //         return;
        //
        //     var state = Controller.layers[layer].stateMachine.states.FirstOrDefault(x => x.state.name == stateName);
        //
        //     if (state.state != null)
        //         state.state.motion = Clip;
        // }


#if UNITY_EDITOR
        private IEnumerable<string> GetAllStateNames()
        {
            return animator.GetAnimatorStateNames(0);
        }

        private AnimatorController controller;

        [TabGroup("Animator")]
        [ShowInInspector]
        private AnimatorController Controller =>
            controller == null ? controller = animator.GetAnimatorController() : controller; // as AnimatorController;

        private bool IsClipNotSynced => !IsClipSynced;

        private bool IsClipSynced
        {
            get
            {
                if (Controller == null)
                    return false;

                var editorController = Controller as AnimatorController;

                var state = Controller.layers[0].stateMachine.states.FirstOrDefault(x => x.state.name == stateName);

                if (state.state != null)
                    return state.state.motion == Clip;
                else
                    return false;
            }
        }

        private void FetchClipCheck() //inspector code
        {
            //get clip from current state
            if (Controller == null)
                return;

            var editorController = Controller as AnimatorController;


            var state = editorController.layers[layer].stateMachine.states
                .FirstOrDefault(x => x.state.name == stateName);
            if (Clip == null || IsClipSynced)
                _clip = state.state.motion as AnimationClip;
        }


        [TabGroup("Animator")]
        [ShowIn(PrefabKind.PrefabAsset)]
        [Button("Generate Animation Controller")]
        private void GenerateAnimationController()
        {
            if (Controller == null)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                var prefabPath = stage.assetPath;
                //GetFolderFromPath(stage.assetPath);
                var folderPath = prefabPath.FolderPath();
                var ac = AnimatorController.CreateAnimatorControllerAtPath(
                    $"{folderPath}/{animator.name}.controller");
                // ac.AddLayer("");
                controller = ac;
                animator.runtimeAnimatorController = ac;
                UnityEditor.EditorUtility.SetDirty(animator);
            }
        }

        [Button]
        private void SaveClipToAnimatorController()
        {
            var animStateMachine = controller.layers[0].stateMachine;

            //find state with same name
            var state = animStateMachine.states.FirstOrDefault(x => x.state.name == stateName);
            if (state.state != null)
            {
                state.state.motion = Clip;
            }
            else
            {
                //create new state
                var newState = animStateMachine.AddState(stateName);
                newState.motion = Clip;
            }
        }


        private void CreateNewClip()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            var prefabPath = stage.assetPath;
            //GetFolderFromPath(stage.assetPath);
            var folderPath = prefabPath.FolderPath();
            var clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, $"{folderPath}/{stateName}.anim");
            _clip = clip;
        }

        [HideIf("Clip")]
        [Button("Create New Clip")]
        private void CreateClipAndSaveToAnimatorController()
        {
            CreateNewClip();
            SaveClipToAnimatorController();
        }

        [Button("Edit Clip")]
        public void EditClip()
        {
            AnimatorHelper.EditClip(animator, Clip);
        }
#endif
    }
}