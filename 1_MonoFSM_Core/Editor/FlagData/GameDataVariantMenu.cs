using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.FlagData
{
    /// <summary>
    ///     Project 視窗右鍵一鍵建立 GameData Variant。
    ///     原 asset 不動當 base，新 asset 的 _baseConfig 指回原 asset、delta 欄位清空。
    ///     實作在 Runtime 的 GameData.Variant.cs（#if UNITY_EDITOR），因為 Odin [Button] 需要在 Runtime assembly。
    /// </summary>
    public static class GameDataVariantMenu
    {
        private const string LogTag = "[GameDataVariant]";
        private const string MenuPath = "Assets/MonoFSM/建立 GameData Variant";

        [MenuItem(MenuPath, false, 2001)]
        private static void CreateVariantForSelected()
        {
            var objects = Selection.objects;
            GameData lastVariant = null;
            var count = 0;

            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] is not GameData data)
                    continue;

                var variant = GameData.CreateVariantAsset(data);
                if (variant == null)
                    continue;

                lastVariant = variant;
                count++;
            }

            if (count == 0)
            {
                Debug.LogWarning($"{LogTag} 沒有選到任何 GameData asset");
                return;
            }

            AssetDatabase.Refresh();
            if (lastVariant != null)
            {
                Selection.activeObject = lastVariant;
                EditorGUIUtility.PingObject(lastVariant);
            }

            Debug.Log($"{LogTag} 共建立 {count} 顆 variant");
        }

        [MenuItem(MenuPath, true)]
        private static bool CreateVariantForSelectedEnabled()
        {
            var objects = Selection.objects;
            for (var i = 0; i < objects.Length; i++)
                if (objects[i] is GameData)
                    return true;
            return false;
        }
    }
}
