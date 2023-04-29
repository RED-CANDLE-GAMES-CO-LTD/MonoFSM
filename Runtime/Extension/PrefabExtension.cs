using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace Extension
{
    public static class PrefabExtension
    {
        //TODO: 還沒測試

        public static T GenerateScriptableObjectInPrefabFolder<T>(this GameObject gObj) where T : ScriptableObject
        {
            //動畫對應是clip？
#if UNITY_EDITOR
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                // prefabStage.IsPartOfPrefabContents()
                var prefabPath = prefabStage.assetPath;
                var folderPath = prefabPath[..prefabPath.LastIndexOf('/')];
                var asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, folderPath + "/0_" + gObj.name + ".anim");
                Debug.Log("生成 SO" + asset, asset);
                AssetDatabase.SaveAssets();
                return asset;
            }
            // var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m);
#endif
            return null;
        }


        //TODO: 還沒測試，monsterState 類似code, 但不是用 stateName做
        public static AnimationClip GenerateAnimationClipInPrefabFolder(this GameObject gObj, string stateName)
        {
#if UNITY_EDITOR
            var animator = gObj.GetComponentInParent<IAnimatorProvider>();
            var anim = animator.ChildAnimator;
            var overrideController = anim.runtimeAnimatorController as AnimatorOverrideController;
            if (overrideController == null) return null;

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                var prefabPath = prefabStage.assetPath;
                var folderPath = prefabPath[..prefabPath.LastIndexOf('/')];
                var overrideClip = new AnimationClip();

                var baseController = overrideController.runtimeAnimatorController as AnimatorController;
                if (baseController == null)
                    return null;

                var baseClip = baseController.layers[0].stateMachine.states
                    .FirstOrDefault((state) => state.state.name == stateName).state.motion as AnimationClip;

                overrideController[baseClip] = overrideClip;
                AssetDatabase.CreateAsset(overrideClip, folderPath + "/0_" + gObj.name + ".anim");
                Debug.Log("生成 clip" + overrideClip, overrideClip);
                AssetDatabase.SaveAssets();
                return overrideClip;
            }
#endif
            return null;
        }
    }
}