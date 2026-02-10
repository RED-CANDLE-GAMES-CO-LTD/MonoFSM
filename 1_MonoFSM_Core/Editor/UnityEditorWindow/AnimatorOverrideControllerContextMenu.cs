using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AnimatorOverrideControllerContextMenu
{
    [MenuItem("CONTEXT/AnimatorOverrideController/Generate Empty Clips For Empty Slots")]
    private static void GenerateEmptyClipsForEmptySlots(MenuCommand command)
    {
        var overrideController = command.context as AnimatorOverrideController;
        if (overrideController == null || overrideController.runtimeAnimatorController == null)
        {
            Debug.LogError("AnimatorOverrideController or its source controller is null");
            return;
        }

        var controllerPath = AssetDatabase.GetAssetPath(overrideController);
        var folderPath = Path.GetDirectoryName(controllerPath);
        var controllerName = Path.GetFileNameWithoutExtension(controllerPath);

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(overrides);

        Undo.RecordObject(overrideController, "Generate Override Clips");

        var createdCount = 0;
        for (var i = 0; i < overrides.Count; i++)
        {
            if (overrides[i].Value != null) continue;

            var originalClip = overrides[i].Key;
            var clipName = $"{controllerName}_{originalClip.name}";
            var clipPath = Path.Combine(folderPath, clipName + ".anim");
            clipPath = AssetDatabase.GenerateUniqueAssetPath(clipPath);

            var newClip = new AnimationClip { name = Path.GetFileNameWithoutExtension(clipPath) };
            AssetDatabase.CreateAsset(newClip, clipPath);

            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, newClip);
            createdCount++;
        }

        overrideController.ApplyOverrides(overrides);
        EditorUtility.SetDirty(overrideController);
        AssetDatabase.SaveAssets();

        Debug.Log($"Generated {createdCount} empty clips for {controllerName}");
    }
}
