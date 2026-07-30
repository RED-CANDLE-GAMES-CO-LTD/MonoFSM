using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore;

namespace RCGInputAction
{
    //Kenney Input Prompts 的 sheet(png+xml) 一鍵轉出 TMP Sprite Asset 的 build config。
    //Editor-only：只在開發時手動按 Button 重建，不進 runtime。
    [CreateAssetMenu(
        menuName = "MonoFSM/Input/SpriteAssetBuildConfig",
        fileName = "InputPromptSpriteAssetBuildConfig",
        order = 0
    )]
    public class InputPromptSpriteAssetBuildConfig : ScriptableObject
    {
        [Serializable]
        public class SheetEntry
        {
            public PromptDeviceFamily _family;

            [Required]
            [PreviewField]
            public Texture2D _sheet;

            //留空時自動用 _family 的名字當 TMP Sprite Asset 名稱
            public string _outputSpriteAssetName;
        }

        [TableList]
        public List<SheetEntry> _sheets = new();

        //TMP 要靠 TMP Settings 裡設定的 Default Sprite Asset Path 才能用 <sprite="AssetName"> 依名稱定址
        [FolderPath]
        public string _outputFolder = "Assets/TextMesh Pro/Resources/Sprite Assets";

        [InfoBox("baseline/尺寸只調一次，之後 String Table 裡就永遠不用寫 <voffset>")]
        public float _baselineOffsetRatio = 0.8f;

        public float _advanceRatio = 1f;

        public float _glyphScale = 1f;

        [Button("重建全部 Sprite Asset")]
        public void RebuildAll()
        {
            foreach (var entry in _sheets)
            {
                try
                {
                    RebuildEntry(entry);
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[InputPromptSpriteAssetBuildConfig] {entry._family} 重建失敗：{e}",
                        this
                    );
                }
            }
        }

        //只重建單一 family，方便調整某個機種時不用整批跑
        [Button("重建單一 Sprite Asset")]
        public void RebuildSingle(PromptDeviceFamily family)
        {
            var entry = _sheets.FirstOrDefault(e => e._family == family);
            if (entry == null)
            {
                Debug.LogError(
                    $"[InputPromptSpriteAssetBuildConfig] 找不到 family={family} 對應的 entry",
                    this
                );
                return;
            }

            try
            {
                RebuildEntry(entry);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[InputPromptSpriteAssetBuildConfig] {entry._family} 重建失敗：{e}",
                    this
                );
            }
        }

        private void RebuildEntry(SheetEntry entry)
        {
            if (entry._sheet == null)
                throw new Exception("_sheet 未指定");

            var sheetPath = AssetDatabase.GetAssetPath(entry._sheet);
            if (string.IsNullOrEmpty(sheetPath))
                throw new Exception("_sheet 不是有效的 project asset");

            //xml 自動找：跟 sheet 同路徑同檔名，副檔名換成 .xml
            var xmlPath = Path.ChangeExtension(sheetPath, ".xml");
            if (!File.Exists(xmlPath))
                throw new Exception($"找不到對應的 xml：{xmlPath}");

            var regions = ParseXml(xmlPath);
            if (regions.Count == 0)
                throw new Exception($"xml 內沒有解析到任何 SubTexture：{xmlPath}");

            var slicedCount = SliceSpriteSheet(sheetPath, regions);
            //切片階段就先自我檢查，數量不對要在這裡爆炸，不要讓錯誤流到下面逐個 region 對不到 sprite 才報「找不到切好的 sprite」，
            //那種訊息看起來像是資料對不上，實際上是切片沒切完。
            if (slicedCount < regions.Count)
                Debug.LogError(
                    $"[InputPromptSpriteAssetBuildConfig] {entry._family} 切片沒有完全生效：xml 有 {regions.Count} 個 region，實際切出 {slicedCount} 個 sprite，缺 {regions.Count - slicedCount} 個。（{sheetPath}）",
                    this
                );

            var outputName = string.IsNullOrEmpty(entry._outputSpriteAssetName)
                ? entry._family.ToString()
                : entry._outputSpriteAssetName;

            EnsureFolderExists(_outputFolder);
            var outputPath = $"{_outputFolder}/{outputName}.asset";

            var spriteCount = BuildOrUpdateSpriteAsset(
                outputPath,
                entry._sheet,
                sheetPath,
                regions
            );

            Debug.Log(
                $"[InputPromptSpriteAssetBuildConfig] {entry._family} 重建完成，共 {spriteCount} 個 sprite，輸出到 {outputPath}",
                this
            );
        }

        private readonly struct SubTextureRegion
        {
            public readonly string Name;
            public readonly int X;
            public readonly int Y;
            public readonly int Width;
            public readonly int Height;

            public SubTextureRegion(string name, int x, int y, int width, int height)
            {
                Name = name;
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
        }

        //解析 Kenney 的 TextureAtlas xml。這份 sheet 的排列是左下往右上，
        //xml 的 y 已經是左下為原點，跟 Unity sprite rect / TMP glyphRect 同一套座標系，直接用即可。
        //別自作聰明加 y = textureHeight - xmlY - height 的翻轉：那會讓所有圖上下鏡射錯位
        //（抓到「同一 x、對稱那一列」的圖），而且切片與 TMP 兩邊都不報錯，只能靠肉眼發現。
        private List<SubTextureRegion> ParseXml(string xmlPath)
        {
            var doc = XDocument.Load(xmlPath);
            var result = new List<SubTextureRegion>();

            if (doc.Root == null)
                return result;

            foreach (var sub in doc.Root.Elements("SubTexture"))
            {
                var name = sub.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name))
                    continue;

                var x = (int)sub.Attribute("x");
                var y = (int)sub.Attribute("y");
                var w = (int)sub.Attribute("width");
                var h = (int)sub.Attribute("height");

                result.Add(new SubTextureRegion(name, x, y, w, h));
            }

            return result;
        }

        //切 sprite：不用 TextureImporter.spritesheet，改走 SpriteDataProviderFactories，
        //目的是讓既有 sprite 的 spriteID 在重切時保持穩定，不然每次重建都換 GUID，已經指向這些 sprite 的引用會全斷。
        //回傳實際切出來的 sprite 數量，讓呼叫端可以驗證有沒有切完整。
        private int SliceSpriteSheet(string path, List<SubTextureRegion> regions)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new Exception($"取得 TextureImporter 失敗：{path}");

            //對「全新未切過」的貼圖來說，textureType/spriteImportMode 這些改動只存在 importer 的記憶體物件上，
            //不會馬上寫進 .meta；如果沒先落地就直接拿 provider 來切，provider 對應的 sprite 編輯資料
            //可能還是照著舊的（非 Sprite/Multiple）狀態在跑，切出來的 sprite 子資產就會不齊全。
            //這裡先 SaveAndReimport() 把型別改動確實落地、reimport 一次，之後才用乾淨的 Sprite/Multiple 狀態去拿 provider。
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var existingRects = provider.GetSpriteRects();
            var existingByName = new Dictionary<string, SpriteRect>();
            foreach (var rect in existingRects)
            {
                if (!string.IsNullOrEmpty(rect.name))
                    existingByName[rect.name] = rect;
            }

            var newRects = new SpriteRect[regions.Count];
            for (var i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                SpriteRect spriteRect;
                if (existingByName.TryGetValue(region.Name, out var existing))
                {
                    //既有的沿用 spriteID，只更新 rect
                    spriteRect = existing;
                }
                else
                {
                    //新增的才給新 GUID
                    spriteRect = new SpriteRect { spriteID = GUID.Generate() };
                }

                spriteRect.name = region.Name;
                spriteRect.rect = new Rect(region.X, region.Y, region.Width, region.Height);
                spriteRect.alignment = SpriteAlignment.Center;
                spriteRect.pivot = new Vector2(0.5f, 0.5f);

                newRects[i] = spriteRect;
            }

            //xml 裡已不存在的名字不放進 newRects，等同移除
            provider.SetSpriteRects(newRects);
            provider.Apply();

            //provider.Apply() 一樣只是把資料寫進 importer 的記憶體物件，要靠 WriteImportSettingsIfDirty 落地到 .meta，
            //再 ImportAsset 重新產生 sprite 子資產，兩步都做才保證這次的 rect 改動真的生效。
            EditorUtility.SetDirty(importer);
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var actualCount = AssetDatabase
                .LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .Count();
            return actualCount;
        }

        //建立或就地更新 TMP_SpriteAsset：已存在時絕不能 delete + recreate，要保留 asset GUID 讓既有引用不斷。
        private int BuildOrUpdateSpriteAsset(
            string outputPath,
            Texture2D sheetTexture,
            string sheetPath,
            List<SubTextureRegion> regions
        )
        {
            //重新載入切完的 sprite 子資產，依名字對應回 region
            var spritesByName = AssetDatabase
                .LoadAllAssetsAtPath(sheetPath)
                .OfType<Sprite>()
                .ToDictionary(s => s.name);

            var spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(outputPath);
            var isNew = spriteAsset == null;
            if (isNew)
            {
                spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, outputPath);

                //version 的 setter 是 internal（package 外部存取不到），改用 SerializedObject 直接寫入 backing field
                var so = new SerializedObject(spriteAsset);
                so.FindProperty("m_Version").stringValue = "1.1.0";
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            spriteAsset.spriteSheet = sheetTexture;

            var characterTable = new List<TMP_SpriteCharacter>();
            var glyphTable = new List<TMP_SpriteGlyph>();

            uint glyphIndex = 0;
            foreach (var region in regions)
            {
                if (!spritesByName.TryGetValue(region.Name, out var sprite))
                {
                    Debug.LogError(
                        $"[InputPromptSpriteAssetBuildConfig] 找不到切好的 sprite：{region.Name}（{sheetPath}）",
                        this
                    );
                    continue;
                }

                var glyph = new TMP_SpriteGlyph
                {
                    index = glyphIndex,
                    glyphRect = new GlyphRect(region.X, region.Y, region.Width, region.Height),
                    metrics = new GlyphMetrics(
                        region.Width,
                        region.Height,
                        0,
                        region.Height * _baselineOffsetRatio,
                        region.Width * _advanceRatio
                    ),
                    scale = _glyphScale,
                    sprite = sprite,
                };
                glyphTable.Add(glyph);

                //unicode 用不到（只靠 name 定址），固定填 0xFFFE
                var character = new TMP_SpriteCharacter(0xFFFE, glyph)
                {
                    name = region.Name,
                    scale = 1f,
                };
                characterTable.Add(character);

                glyphIndex++;
            }

            //spriteCharacterTable / spriteGlyphTable 的 setter 是 internal，改成直接清空、重灌既有 List
            spriteAsset.spriteCharacterTable.Clear();
            spriteAsset.spriteCharacterTable.AddRange(characterTable);
            spriteAsset.spriteGlyphTable.Clear();
            spriteAsset.spriteGlyphTable.AddRange(glyphTable);

            //Material：沿用既有 sub-asset，不要每次重建新建一顆
            if (spriteAsset.material == null)
            {
                ShaderUtilities.GetShaderPropertyIDs();
                var shader = Shader.Find("TextMeshPro/Sprite");
                var material = new Material(shader) { name = spriteAsset.name + " Material" };
                material.SetTexture(ShaderUtilities.ID_MainTex, sheetTexture);
                spriteAsset.material = material;
                AssetDatabase.AddObjectToAsset(material, spriteAsset);
            }
            else
            {
                spriteAsset.material.SetTexture(ShaderUtilities.ID_MainTex, sheetTexture);
            }

            spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(spriteAsset.name);
            spriteAsset.materialHashCode = TMP_TextUtilities.GetSimpleHashCode(
                spriteAsset.material.name
            );
            spriteAsset.UpdateLookupTables();

            EditorUtility.SetDirty(spriteAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(outputPath);

            return characterTable.Count;
        }

        private static void EnsureFolderExists(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parts = folder.Split('/');
            var current = parts[0]; //預期是 "Assets"
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
