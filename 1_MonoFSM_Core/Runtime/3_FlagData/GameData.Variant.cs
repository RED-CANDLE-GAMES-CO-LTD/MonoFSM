#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

/// <summary>
///     GameData Variant 一鍵生成。
///     語意：原 asset 完全不動當 base，新建一顆 variant，_baseConfig 指回原 asset，
///     delta 欄位（_configs / _objConfigs / _bindPrefab）清空，靠疊層繼承。
///     用 AssetDatabase.CopyAsset 做整份複製，才能保留子類別型別與 [SerializeReference] 的 _dataFunctions。
/// </summary>
public partial class GameData
{
    private const string VariantLogTag = "[GameDataVariant]";

    [Button("建立 Variant（本體當 base）", ButtonSizes.Medium)]
    [PropertyOrder(101)]
    private void CreateVariant()
    {
        CreateVariantAndSelect(this);
    }

    /// <summary>
    ///     對 source 建立一顆 variant asset，回傳新建的 variant（失敗回 null）。
    /// </summary>
    /// <param name="nameHint">
    ///     指定新 asset 的檔名（不含副檔名），通常是呼叫端所在的 prefab 名。
    ///     null / 空字串時用 "{base 名} Variant"。存放位置一律是 base asset 同資料夾。
    /// </param>
    public static GameData CreateVariantAsset(GameData source, string nameHint = null)
    {
        if (source == null)
        {
            Debug.LogError($"{VariantLogTag} source 是 null，無法建立 variant");
            return null;
        }

        var path = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError($"{VariantLogTag} {source.name} 不是專案裡的 asset，無法建立 variant", source);
            return null;
        }

        var dir = System.IO.Path.GetDirectoryName(path);
        var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        var newName = SanitizeFileName(nameHint);
        if (string.IsNullOrEmpty(newName))
            newName = $"{fileName} Variant";
        var newPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{newName}.asset");

        if (!AssetDatabase.CopyAsset(path, newPath))
        {
            Debug.LogError($"{VariantLogTag} CopyAsset 失敗：{path} → {newPath}", source);
            return null;
        }

        AssetDatabase.ImportAsset(newPath);
        var variant = AssetDatabase.LoadAssetAtPath<GameData>(newPath);
        if (variant == null)
        {
            Debug.LogError($"{VariantLogTag} 複製後讀不回 GameData：{newPath}", source);
            return null;
        }

        //欄位都是 private，走 SerializedObject 不用改 runtime 可及性
        var so = new SerializedObject(variant);
        var baseConfigProp = so.FindProperty(nameof(_baseConfig));
        if (baseConfigProp != null)
            baseConfigProp.objectReferenceValue = source;
        else
            Debug.LogError($"{VariantLogTag} 找不到 _baseConfig 欄位，variant 沒接上 base", variant);

        so.FindProperty(nameof(_configs))?.ClearArray();
        so.FindProperty(nameof(_objConfigs))?.ClearArray();

        var bindPrefabProp = so.FindProperty(nameof(_bindPrefab));
        if (bindPrefabProp != null)
            bindPrefabProp.objectReferenceValue = null; //靠疊層繼承 base 的

        so.ApplyModifiedPropertiesWithoutUndo();

        //CopyAsset 會把 base 的 SaveID 一起複製過來，會撞號
        if (source.gameStateType == GameStateType.Manual)
        {
            variant.SetSaveID(AssetDatabase.AssetPathToGUID(newPath));
        }
        else
        {
            Debug.LogWarning(
                $"{VariantLogTag} {variant.name} 的 gameStateType 是 {source.gameStateType}，" +
                "SaveID 沒有自動改寫，請人工確認是否會與 base 撞號", variant);
        }

        EditorUtility.SetDirty(variant);
        AssetDatabase.SaveAssetIfDirty(variant);
        Debug.Log($"{VariantLogTag} 建立 {newPath}，base = {source.name}", variant);
        return variant;
    }

    /// <summary>
    ///     把 nameHint 洗成合法檔名（濾掉 OS 不允許的字元），全空回 null 讓呼叫端 fallback。
    /// </summary>
    private static string SanitizeFileName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (var c in raw.Trim())
            if (System.Array.IndexOf(invalid, c) < 0)
                sb.Append(c);

        var result = sb.ToString().Trim();
        return string.IsNullOrEmpty(result) ? null : result;
    }

    /// <summary>
    ///     建立 variant 並在 Project 視窗選取 / ping 它。
    /// </summary>
    public static void CreateVariantAndSelect(GameData source)
    {
        var variant = CreateVariantAsset(source);
        if (variant == null)
            return;

        Selection.activeObject = variant;
        EditorGUIUtility.PingObject(variant);
    }
}
#endif
