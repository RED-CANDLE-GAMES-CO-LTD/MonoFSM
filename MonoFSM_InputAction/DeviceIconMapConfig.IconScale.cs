#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace RCGInputAction
{
    //icon 大小自動校正：Kenney 的 sheet 是 64px 一格，但每顆圖在格子裡佔的高度不一樣
    //（一般按鍵 48px、shift 只有 32px、mouse 44px），直接 inline 進文字會看起來大小不一。
    //這裡量出每顆圖的實際高度，換算成補償倍率填進 entry._iconScale，讓所有 icon 視覺上等高。
    public partial class DeviceIconMapConfig
    {
        //倍率離譜通常代表量錯（整格透明、圖只有一條線），夾住避免把版面撐爛
        private const float MinAutoScale = 0.5f;
        private const float MaxAutoScale = 3f;

        [BoxGroup("Icon 大小")]
        [Button("依圖案高度自動校正每顆的 _iconScale", ButtonSizes.Medium)]
        private void AutoFitIconScales()
        {
            if (_iconAutoFitBaseHeight <= 0f)
            {
                Debug.LogError("[DeviceIconMapConfig] 基準高度要大於 0", this);
                return;
            }

            var changed = 0;
            var skipped = new List<string>();

            //先把所有會用到的 sheet 一次開好 readable 再讀像素：SaveAndReimport 會重建 Texture2D 實例，
            //邊 reimport 邊讀會拿到已經失效的參考
            var readableRestore = MakeSheetsReadable();

            try
            {
                foreach (var entry in _entries)
                {
                    var sprite = entry._icon != null ? entry._icon : entry.ResolveSpriteFromTmpAsset();
                    if (sprite == null)
                    {
                        skipped.Add($"{entry._bindingPath}（沒有 icon）");
                        continue;
                    }

                    if (sprite.texture == null || !sprite.texture.isReadable)
                    {
                        skipped.Add($"{entry._bindingPath}（texture 讀不到像素）");
                        continue;
                    }

                    var height = MeasureContentHeight(sprite);
                    if (height <= 0)
                    {
                        skipped.Add($"{entry._bindingPath}（整格都是透明的）");
                        continue;
                    }

                    var scale = Mathf.Clamp(_iconAutoFitBaseHeight / height, MinAutoScale, MaxAutoScale);
                    scale = Mathf.Round(scale * 100f) / 100f; //留兩位就好，數字太長難讀
                    if (Mathf.Approximately(entry._iconScale, scale))
                        continue;

                    entry._iconScale = scale;
                    changed++;
                }
            }
            finally
            {
                foreach (var importer in readableRestore)
                {
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }

            if (changed > 0)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssetIfDirty(this);
            }

            var message = $"[DeviceIconMapConfig] {name} 校正了 {changed} 筆 _iconScale" +
                          $"（基準高度 {_iconAutoFitBaseHeight}px）";
            if (skipped.Count > 0)
                message += $"\n跳過 {skipped.Count} 筆：\n  " + string.Join("\n  ", skipped);
            Debug.Log(message, this);
        }

        [BoxGroup("Icon 大小")]
        [Button("把所有 _iconScale 還原成 1")]
        private void ResetIconScales()
        {
            foreach (var entry in _entries)
                entry._iconScale = 1f;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        //量這顆 sprite 在自己那格裡「畫得到東西」的高度（單位：格內像素）
        private static int MeasureContentHeight(Sprite sprite)
        {
            var tex = sprite.texture;
            var rect = sprite.rect;
            var x = Mathf.Clamp((int)rect.x, 0, tex.width);
            var y = Mathf.Clamp((int)rect.y, 0, tex.height);
            var w = Mathf.Clamp((int)rect.width, 0, tex.width - x);
            var h = Mathf.Clamp((int)rect.height, 0, tex.height - y);
            if (w <= 0 || h <= 0)
                return 0;

            var pixels = tex.GetPixels(x, y, w, h);
            var top = -1;
            var bottom = -1;
            for (var row = 0; row < h; row++)
            {
                var rowStart = row * w;
                for (var col = 0; col < w; col++)
                {
                    if (pixels[rowStart + col].a <= 0.04f)
                        continue;
                    if (bottom < 0)
                        bottom = row;
                    top = row;
                    break;
                }
            }

            return top < 0 ? 0 : top - bottom + 1;
        }

        //GetPixels 需要 Read/Write Enabled；把這份 config 用到的 sheet 都暫時打開，回傳要還原的 importer
        private List<TextureImporter> MakeSheetsReadable()
        {
            var restore = new List<TextureImporter>();
            var seen = new HashSet<string>();

            foreach (var entry in _entries)
            {
                var sprite = entry._icon != null ? entry._icon : entry.ResolveSpriteFromTmpAsset();
                if (sprite == null || sprite.texture == null || sprite.texture.isReadable)
                    continue;

                var path = AssetDatabase.GetAssetPath(sprite.texture);
                if (string.IsNullOrEmpty(path) || !seen.Add(path))
                    continue;
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;

                importer.isReadable = true;
                importer.SaveAndReimport();
                restore.Add(importer);
            }

            return restore;
        }
    }
}
#endif
