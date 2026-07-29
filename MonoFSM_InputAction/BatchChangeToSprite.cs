using UnityEditor;
using UnityEngine;

public class BatchChangeToSprite
{
    [MenuItem("Assets/Batch Change Texture To Sprite", false, 100)]
    static void ChangeTexturesToSprite()
    {
        // 取得目前選取的所有物件（包含子資料夾內的圖片）
        Object[] selectedObjects =
            Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);

        int count = 0;
        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            // 如果原本不是 Sprite，才進行轉換
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                AssetDatabase.ImportAsset(path);
                count++;
            }
        }

        Debug.Log($"批次轉換完成！共處理了 {count} 張圖片。");
    }
}
