using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif
using UnityEngine;

namespace MonoFSM.AnimatorUtility
{
    //FIXME: core module? animator asset creation
    public static class AnimatorAssetUtility
    {
#if UNITY_EDITOR
        public static void AddStateAndCreateClipToLayerIndex(AnimatorController animatorController, int layerIndex,
            string stateName)
        {
            if (animatorController == null)
                return;
            //get folder path of controller
            var path = AssetDatabase.GetAssetPath(animatorController);
            var stateMachine = animatorController.layers[layerIndex].stateMachine;
            // Create a new state
            var newState = stateMachine.AddState(stateName);
            var layer = animatorController.layers[layerIndex];
            // Create a new clip for the state
            var clip = CreateClip(animatorController, layer, newState, path);
            newState.motion = clip;
        }
        
        public static void AddStateToLayer(AnimatorController animatorController, AnimatorControllerLayer layer,
            string stateName)
        {
            if (animatorController == null)
                return;

            //get folder path of controller
            var path = AssetDatabase.GetAssetPath(animatorController);
            
            // 如果是同步層，使用 syncedLayerIndex，否則使用當前層的 stateMachine
            var stateMachine = layer.syncedLayerIndex >= 0 
                ? animatorController.layers[layer.syncedLayerIndex].stateMachine 
                : layer.stateMachine;

            // Create a new state
            var newState = stateMachine.AddState(stateName);

            // Create a new clip for the state
            var clip = CreateClip(animatorController, layer, newState, path);
            newState.motion = clip;
        }

        public static void CreateAnimatorController(Dictionary<string,object> data)
        {
            var path = data["assetPath"].ToString();
            // Check if the path is valid
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Invalid path for Animator Controller.");
                return;
            }

            // Check if the file already exists
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            {
                Debug.LogError($"Animator Controller already exists at {path}.");
                return;
            }
        
            // Create a new AnimatorController
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            // Set the name of the controller
            controller.name = data["name"].ToString();
            // Already has Base Layer
            //Create State
            var stateMachine = controller.layers[0].stateMachine;
            AddStateToLayer(controller, controller.layers[0], "Idle");
        }

        public static void CreateClipsForAllStatesOfLayer(AnimatorController animatorController, int layerIndex = 0)
        {
            if (animatorController == null)
                return;

            //get folder path of controller
            var path = AssetDatabase.GetAssetPath(animatorController);
            if (animatorController.layers.Length <= layerIndex)
                return;
            var layer = animatorController.layers[layerIndex];


            //synced
            if (layer.syncedLayerIndex != -1)
            {
                var syncedStates = animatorController.layers[layer.syncedLayerIndex].stateMachine.states;
                Debug.Log("Synced Layer:" + layer.name + syncedStates.Length);
                foreach (var childState in syncedStates)
                {
                    var state = childState.state;
                    //use GetOverride and SetOverride to get and set the override clip
                    var overrideMotion = layer.GetOverrideMotion(state);

                    var clip = animatorController.GetStateEffectiveMotion(state, layerIndex);
                    Debug.Log("GetOverrideMotion:" + overrideMotion);
                    Debug.Log("GetStateEffectiveMotion:" + clip);
                    if (clip == null)
                    {
                        clip = CreateClip(animatorController, layer, state, path);
                        animatorController.SetStateEffectiveMotion(state, clip, layerIndex);
                        // layer.SetOverrideMotion(state.state, clip);
                    }
                }
            }
            else
            {
                var states = animatorController.layers[layerIndex].stateMachine.states;
                Debug.Log("Layer:" + layer.name + states.Length);
                foreach (var childState in states)
                {
                    var state = childState.state;
                    var clip = state.motion as AnimationClip;
                    if (clip == null)
                    {
                        clip = CreateClip(animatorController, layer, state, path);
                        state.motion = clip;
                    }
                }
            }
        }

        public static AnimationClip CreateClip(AnimatorController animatorController, AnimatorControllerLayer layer,
            AnimatorState state, string path)
        {
            var clip = new AnimationClip();
            clip.name = state.name;
            //Create asset in the same folder as the controller
            AssetDatabase.CreateAsset(clip,
                path.Replace(animatorController.name + ".controller", "") +
                $"[{layer.name.Replace(" Layer", "")}] {clip.name}.anim");
            return clip;
        }
        
        public static AnimationClip CreateEmptyClip(string name, string path)
        {
            var clip = new AnimationClip();
            clip.name = name;
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }
#endif
    }

   
}
