using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class AnimatorOverrideControllerContextMenu
{
    private static string CleanClipName(string originalName, string controllerName)
    {
        // 移除 [...] 內容（含中括弧本身）
        var cleaned = Regex.Replace(originalName, @"\s*\[.*?\]", "").Trim();
        return $"{cleaned} {controllerName}";
    }

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
        var folderPath = Path.GetDirectoryName(controllerPath) ?? "Assets";
        var controllerName = Path.GetFileNameWithoutExtension(controllerPath);

        var overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(overrides);

        Undo.RecordObject(overrideController, "Generate Override Clips");

        var createdCount = 0;
        for (var i = 0; i < overrides.Count; i++)
        {
            if (overrides[i].Value != null) continue;

            var originalClip = overrides[i].Key;
            var clipName = CleanClipName(originalClip.name, controllerName);
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

    [MenuItem("CONTEXT/AnimatorOverrideController/Rename Override Clips")]
    private static void RenameOverrideClips(MenuCommand command)
    {
        var overrideController = command.context as AnimatorOverrideController;
        if (overrideController == null || overrideController.runtimeAnimatorController == null)
        {
            Debug.LogError("AnimatorOverrideController or its source controller is null");
            return;
        }

        var controllerPath = AssetDatabase.GetAssetPath(overrideController);
        var controllerName = Path.GetFileNameWithoutExtension(controllerPath);

        var overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(overrides);

        var renamedCount = 0;
        for (var i = 0; i < overrides.Count; i++)
        {
            var clip = overrides[i].Value;
            if (clip == null) continue;

            var originalClip = overrides[i].Key;
            var newName = CleanClipName(originalClip.name, controllerName);

            if (clip.name == newName) continue;

            var clipPath = AssetDatabase.GetAssetPath(clip);

            var result = AssetDatabase.RenameAsset(clipPath, newName);
            if (string.IsNullOrEmpty(result))
            {
                renamedCount++;
            }
            else
            {
                Debug.LogWarning($"Failed to rename {clip.name}: {result}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Renamed {renamedCount} override clips for {controllerName}");
    }
}
