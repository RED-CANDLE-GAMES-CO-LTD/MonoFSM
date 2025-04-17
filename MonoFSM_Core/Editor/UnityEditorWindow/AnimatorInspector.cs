using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomEditor(typeof(AnimatorController))]
public class AnimatorInspector : OdinEditor
{
    private AnimatorController currentAnimatorController;

    public override void OnInspectorGUI()
    {
        AnimationClip clip = null;
        EditorGUI.indentLevel = 1;
        // DragAndDropUtilities.DrawDropZone(new Rect(100, 100, 100, 100), clip, null, 1);
        // clip = DragAndDropUtilities.DropZone(new Rect(200, 200, 100, 100), null, typeof(AnimationClip)) as AnimationClip;
        currentAnimatorController = target as AnimatorController;
        // target.name = EditorGUILayout.TextField("Name", oldName);
        // AssetDatabase.SaveAssetIfDirty(target);
        clip =
            EditorGUILayout.ObjectField("Drop Clip To Add", (AnimationClip)null, typeof(AnimationClip), false) as
                AnimationClip;
        if (clip != null)
        {
            var new_ac = Instantiate(clip) as AnimationClip;
            new_ac.name = clip.name;


            //自動增加State
            var state = currentAnimatorController.layers[0].stateMachine.AddState(new_ac.name);
            state.motion = new_ac;

            AssetDatabase.AddObjectToAsset(new_ac, target);
            AssetDatabase.SaveAssetIfDirty(target);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(clip));
        }

        if (GUILayout.Button("Create Empty Clip")) AddClip();
        GUILayout.Label("1. 先在 Base(Logic) Layer 建立需要的State");
        if (GUILayout.Button("2. Create View Layer and Sync with Logic Layer")) CreateViewSyncAnimatorController();
        if (GUILayout.Button("3. 幫所有State生成Clip Create Clip For All States")) CreateClipForAllStates();
        base.OnInspectorGUI();
    }

    [Button()]
    private void AddClip()
    {
        var animationClip = new AnimationClip
        {
            name = "New Clip"
        };
        // Undo.RecordObject(target, "Add Clip To Animator");
        AssetDatabase.AddObjectToAsset(animationClip, target);
        AssetDatabase.SaveAssetIfDirty(target);
        Selection.activeObject = target;
    }

    [TabGroup("Animator Controller")]
    [Button()]
    private void CreateClipsForAllStatesOfLayer(int layerIndex = 0)
    {
        if (currentAnimatorController == null)
            return;

        //get folder path of controller
        var path = AssetDatabase.GetAssetPath(currentAnimatorController);
        if (currentAnimatorController.layers.Length <= layerIndex)
            return;
        var layer = currentAnimatorController.layers[layerIndex];


        //synced
        if (layer.syncedLayerIndex != -1)
        {
            var syncedStates = currentAnimatorController.layers[layer.syncedLayerIndex].stateMachine.states;
            Debug.Log("Synced Layer:" + layer.name + syncedStates.Length);
            foreach (var state in syncedStates)
            {
                //use GetOverride and SetOverride to get and set the override clip
                var overrideMotion = layer.GetOverrideMotion(state.state);

                var clip = currentAnimatorController.GetStateEffectiveMotion(state.state, layerIndex);
                Debug.Log("GetOverrideMotion:" + overrideMotion);
                Debug.Log("GetStateEffectiveMotion:" + clip);
                if (clip == null)
                {
                    clip = CreateClip(layer, state, path);
                    currentAnimatorController.SetStateEffectiveMotion(state.state, clip, layerIndex);
                    // layer.SetOverrideMotion(state.state, clip);
                }
            }
        }
        else
        {
            //
            var states = currentAnimatorController.layers[layerIndex].stateMachine.states;
            Debug.Log("Layer:" + layer.name + states.Length);
            foreach (var state in states)
            {
                var clip = state.state.motion as AnimationClip;
                if (clip == null)
                {
                    clip = CreateClip(layer, state, path);
                    state.state.motion = clip;
                }
            }
        }
    }

    private AnimationClip CreateClip(AnimatorControllerLayer layer, ChildAnimatorState state, string path)
    {
        var clip = new AnimationClip();
        clip.name = state.state.name;
        //Create asset in the same folder as the controller
        AssetDatabase.CreateAsset(clip,
            path.Replace(currentAnimatorController.name + ".controller", "") +
            $"[{layer.name.Replace(" Layer", "")}] {clip.name}.anim");
        return clip;
    }

    [TabGroup("Animator Controller")]
    [Button("建立 Logic View雙層結構 AnimatorController")]
    private void CreateViewSyncAnimatorController()
    {
        // CreateNewLayerAndCopyStatesFromBaseLayerAndRename("Logic Layer");
        // currentAnimatorController.RemoveLayer(0);

        //layer 拿出來
        var baseLayer = currentAnimatorController.layers[0];
        baseLayer.name = currentAnimatorController.MakeUniqueLayerName("Logic Layer");
        baseLayer.stateMachine.name = "Logic Layer";
        //layers 拿出來，換掉再塞回去
        var layers = currentAnimatorController.layers;
        layers[0] = baseLayer;
        currentAnimatorController.layers = layers;

        // var layers = ;
        // ArrayUtility.Remove<AnimatorControllerLayer>(ref layers, layers[0]);
        // currentAnimatorController.layers = layers;
        //
        // currentAnimatorController.AddLayer(baseLayer);

        Debug.Log("Logic Layer?" + baseLayer.name + baseLayer.stateMachine.name);
        EditorUtility.SetDirty(currentAnimatorController.layers[0].stateMachine);
        // Debug.Log("Set Layer 0 Name" + currentAnimatorController.layers[0].name);
        
        
        if (currentAnimatorController.layers.Length == 1)
        {
            currentAnimatorController.AddLayer("View Layer");

            var layer = currentAnimatorController.layers[^1];
            Debug.Log("Add Layer " + layer.name);
            //add a layer called View Layer

            layer.defaultWeight = 1;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.syncedLayerIndex = 0;
            layer.syncedLayerAffectsTiming = true;

            //拿出來再塞回去
            layers = currentAnimatorController.layers;
            layers[1] = layer;
            currentAnimatorController.layers = layers;

            // layers = currentAnimatorController.layers;
            // ArrayUtility.Remove<AnimatorControllerLayer>(ref layers, layers[1]);
            // currentAnimatorController.layers = layers;
            //
            // currentAnimatorController.AddLayer(layer);
        }

        EditorUtility.SetDirty(currentAnimatorController);
        AssetDatabase.SaveAssetIfDirty(currentAnimatorController);
        AssetDatabase.Refresh();
    }


    //不能直接改layer名字...只好刪掉再新增
    private void CreateNewLayerAndCopyStatesFromBaseLayerAndRename(string name)
    {
        currentAnimatorController.AddLayer(name);
        
        
        var layer = new AnimatorControllerLayer
        {
            name = name,
            defaultWeight = 1,
            blendingMode = AnimatorLayerBlendingMode.Override,
            
        };
        var stateMachine = new AnimatorStateMachine
        {
            name = name
        };
        layer.stateMachine = stateMachine;
        //copy all states from base layer to the new layer
        var baseLayer = currentAnimatorController.layers[0];
        var baseStates = baseLayer.stateMachine.states;
        foreach (var state in baseStates)
        {
            var newState = layer.stateMachine.AddState(state.state.name);
            newState.motion = state.state.motion;
            //copy the position
            layer.stateMachine.states[^1].position = state.position;
        }
        

        //add the new layer to the controller
        currentAnimatorController.AddLayer(layer);
    }


    [TabGroup("Animator Controller")]
    [Button()]
    private void CreateClipForAllStates()
    {
        CreateClipsForAllStatesOfLayer(0);
        CreateClipsForAllStatesOfLayer(1);
    }
}