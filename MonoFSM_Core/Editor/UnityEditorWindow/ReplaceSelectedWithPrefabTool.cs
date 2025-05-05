using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class ReplaceSelectedWithPrefabTool
{
    [MenuItem("GameObject/Prefab/關聯為 Prefab（儲存到 Prefabs 資料夾）", false, 10)]
    static void ConvertSelectedToPrefab()
    {
        var selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogError("請先選取一個場景中的 GameObject");
            return;
        }

        string targetName = selected.name;

        // 嘗試找出原始 FBX 的資源路徑
        var prefabSource = PrefabUtility.GetCorrespondingObjectFromOriginalSource(selected);
        string sourcePath = AssetDatabase.GetAssetPath(prefabSource);

        if (string.IsNullOrEmpty(sourcePath))
        {
            Debug.LogError("找不到原始資料夾");
            return;
        }

        // 存放 prefab 在 FBX 同資料夾
        string folder = Path.GetDirectoryName(sourcePath);
        string prefabPath = Path.Combine(folder, $"{targetName}_Prefab.prefab").Replace("\\", "/");

        // --- 關鍵步驟：複製並 Unpack 為新的 GameObject ---
        GameObject unpacked = GameObject.Instantiate(selected);
        unpacked.name = targetName;
        // 儲存成 prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(unpacked, prefabPath);
        GameObject.DestroyImmediate(unpacked); // 清理臨時物件

        Debug.Log($"Prefab 儲存至: {prefabPath}");

        // 找出所有同名物件
        var allObjects = GameObject.FindObjectsOfType<GameObject>();
        var targets = allObjects.Where(obj =>
            obj.name == targetName || obj.name.StartsWith(targetName + " (")).ToList();

        foreach (var go in targets)
        {

            Transform t = go.transform;
            Vector3 pos = t.position;
            Quaternion rot = t.rotation;
            Vector3 scale = t.localScale;
            Transform parent = t.parent;

            GameObject.DestroyImmediate(go);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent);
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            instance.transform.localScale = scale;
        }

        Debug.Log($"已替換場景中 {targets.Count} 個 [{targetName}] 為新 prefab");
    }
}