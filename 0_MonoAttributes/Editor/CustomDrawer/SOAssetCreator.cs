#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// ScriptableObject asset 一鍵生成的共用實作。
    /// 給 SOConfigAttributeDrawer / DropDownRefDrawer 等需要「依型別建立 SO asset」的 drawer 共用，
    /// 統一走 SOPathSettingConfig 的路徑設定 + CreateScriptableObjectAt 擴充方法。
    /// </summary>
    public static class SOAssetCreator
    {
        /// <summary>
        /// 產生統一格式的檔案名稱（scriptableObject 前綴 d_）
        /// </summary>
        public static string GenerateFileName(string postfix)
        {
            return $"d_{postfix}";
        }

        /// <summary>
        /// 依 SOPathSettingConfig 的路徑設定，為指定型別建立 ScriptableObject asset。
        /// </summary>
        /// <param name="configType">要建立的 ScriptableObject 型別（必須是具體、非抽象）</param>
        /// <param name="fileName">檔名（不含副檔名與前綴）</param>
        /// <param name="subFolderPath">SOConfig 上事先定義的子資料夾路徑</param>
        public static ScriptableObject Create(
            Type configType,
            string fileName,
            string subFolderPath = ""
        )
        {
            if (configType == null || !typeof(ScriptableObject).IsAssignableFrom(configType))
            {
                Debug.LogError($"[SOAssetCreator] 型別不是 ScriptableObject: {configType}");
                return null;
            }

            if (configType.IsAbstract)
            {
                Debug.LogError($"[SOAssetCreator] 無法建立抽象型別的 asset: {configType.Name}");
                return null;
            }

            var config = SOPathSettingConfig.Instance;
            var basePath = config.GetBasePathForType(configType);
            var relativePath = config.GetRelativePathForType(configType, subFolderPath);
            return configType.CreateScriptableObjectAt(basePath, relativePath, fileName);
        }
    }
}
#endif
