using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace RCGMaker.Core.Editor
{
#if UNITY_EDITOR
    public static class AnimatorControllerGenerator
    {
        [MenuItem("CONTEXT/Animator/Duplicate Animator Override Controller")]
        public static void GenerateAnimatorOverrideController(MenuCommand command)
        {
            var animator = command.context as Animator;
            if (animator == null)
            {
                Debug.LogError("Can't find Animator");
                return;
            }

            var prefabPath = "";
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage)
            {
                prefabPath = prefabStage.assetPath;
            }
            else
                prefabPath =
                    AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(animator.gameObject));

            Undo.RecordObject(animator, "Generate Variant");

            var folderPath = Path.GetDirectoryName(prefabPath);
            // var newAssetPath = Path.Combine(folderPath, animator.gameObject.name + ".overrideController");
            if (animator.runtimeAnimatorController != null)
            {
                var originalAssetPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
                var originalAssetName = Path.GetFileName(originalAssetPath);
                var newAssetPath = Path.Combine(folderPath, "Copied " + originalAssetName);
                Debug.Log(newAssetPath);
                AssetDatabase.CopyAsset(originalAssetPath, newAssetPath);
                var newOverrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(newAssetPath);
                CopyAllOverrideClipsToControllerFolder(newOverrideController);
                animator.runtimeAnimatorController = newOverrideController;
                animator.SetDirty();
                AssetDatabase.SaveAssets();
            }
            // Undo.FlushUndoRecordObjects();
        }

        //copy all override clips to the same folder of the override controller
        private static void CopyAllOverrideClipsToControllerFolder(AnimatorOverrideController overrideController)
        {
            var folderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(overrideController));
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
            overrideController.GetOverrides(overrides);
            for (var i = 0; i < overrides.Count; ++i)
            {
                if (!overrides[i].Value) continue; //有override的clip

                var clipPath = AssetDatabase.GetAssetPath(overrides[i].Value);
                var clipFolder = Path.GetDirectoryName(clipPath);
                if (folderPath != clipFolder)
                {
                    AssetDatabase.CopyAsset(clipPath, Path.Combine(folderPath, Path.GetFileName(clipPath)));
                    var newClip =
                        AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(folderPath,
                            Path.GetFileName(clipPath)));
                    overrideController[overrides[i].Key] = newClip;
                }
            }
        }


        //generate a new animator controller and assign it to the animator
        [MenuItem("CONTEXT/Animator/Create Or Copy AnimatorController")]
        public static void CreateOrCopyAnimatorController(MenuCommand command)
        {
            var animator = command.context as Animator;
            if (animator == null)
            {
                Debug.LogError("Can't find Animator");
                return;
            }

            var group = Undo.GetCurrentGroup();
            Undo.RecordObject(animator, "Override Animator Controller");
            // var folderPath = Path.GetDirectoryName(prefabPath);
            // var newAssetPath = Path.Combine(folderPath, animator.gameObject.name + ".controller");

            var newAsset = AssetDatabaseUtility.CopyAssetOrCreateToPrefabFolder(animator.runtimeAnimatorController,
                ".animator", (path) =>
                {
                    var newAsset = AnimatorController.CreateAnimatorControllerAtPath(path);
                    return newAsset;
                });
            animator.runtimeAnimatorController = newAsset;
            Undo.CollapseUndoOperations(group);
            // Undo.FlushUndoRecordObjects();
        }
    }
    #endif
}