using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime._3_FlagData;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGInputAction
{
    //一則操作提示：對應的 input action + 依裝置切換的 icon。
    //語序（例如「按住 X 再按 Y 丟擲」）交給 Localization String Table 處理，這裡只負責提供 icon / <sprite> tag。
    [CreateAssetMenu(
        menuName = "MonoFSM/Input/InputPromptUIData",
        fileName = "InputPromptUIData",
        order = 0
    )]
    public partial class InputPromptUIData : AbstractSOConfig
    {
        private static IHintSpriteFinder _spriteFinder;

        //看專案定義，ex: DeviceIconMapConfig / PromptIconRegistry（由 HintSpriteFinderInstaller 注入）
        public static void SetSpriteFinder(IHintSpriteFinder finder)
        {
            _spriteFinder = finder;
        }

        [FormerlySerializedAs("input")]
        [Required]
        public InputActionData _input;

        //找不到對照 icon 時的替代圖
        [FormerlySerializedAs("placeHolderIcon")]
        public Sprite _placeHolderIcon;

        //icon-only 場合用（ex: InputPromptUILabel 純圖示顯示）
        public Sprite GetIcon()
        {
            var finder = ResolveFinder();
            if (finder != null)
            {
                var icon = finder.GetIcon(_input);
                if (icon != null)
                    return icon;
            }

            return _placeHolderIcon;
        }

        //圖文混排場合用：組成 TMP 的 <sprite> tag，inline 進 Smart String 的 token 裡
        public string GetSpriteTag()
        {
            var finder = ResolveFinder();
            return finder?.GetSpriteTag(_input);
        }

        //Play Mode 由 HintSpriteFinderInstaller 注入；Editor 沒跑過 installer，就自己去專案裡找一份來 preview
        private static IHintSpriteFinder ResolveFinder()
        {
            if (_spriteFinder != null)
                return _spriteFinder;
#if UNITY_EDITOR
            return EditorPreviewFinder;
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        private static IHintSpriteFinder _editorPreviewFinder;

        //專案裡通常只有一份 PromptIconRegistry，直接抓第一份當 preview 用的 finder
        private static IHintSpriteFinder EditorPreviewFinder
        {
            get
            {
                if (_editorPreviewFinder is ScriptableObject so && so != null)
                    return _editorPreviewFinder;

                _editorPreviewFinder = null;
                var guids = UnityEditor.AssetDatabase.FindAssets("t:PromptIconRegistry");
                if (guids.Length == 0)
                    return null;

                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                _editorPreviewFinder =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<PromptIconRegistry>(path);
                return _editorPreviewFinder;
            }
        }

        [PropertyOrder(100)]
        [BoxGroup("Editor Preview")]
        [ShowInInspector]
        [InfoBox("Play Mode 外的預覽用機種，會影響所有 InputPromptUIData 的 Editor 預覽")]
        private PromptDeviceFamily PreviewDeviceFamily
        {
            get => InputSchemeWatcher.EditorPreviewFamily;
            set => InputSchemeWatcher.EditorPreviewFamily = value;
        }

        [PropertyOrder(101)]
        [BoxGroup("Editor Preview")]
        [ShowInInspector]
        [PreviewField(64, ObjectFieldAlignment.Left)]
        [ReadOnly]
        private Sprite PreviewIcon => GetIcon();

        [PropertyOrder(102)]
        [BoxGroup("Editor Preview")]
        [ShowInInspector]
        [ReadOnly]
        private string PreviewSpriteTag => GetSpriteTag() ?? "(表裡沒有 sprite tag 資料)";

        //各機種一起看，才知道哪台裝置漏填了
        public class FamilyPreviewRow
        {
            [TableColumnWidth(110, false)]
            [ReadOnly]
            public string _family;

            [TableColumnWidth(60, false)]
            [PreviewField(48, ObjectFieldAlignment.Center)]
            [ReadOnly]
            public Sprite _icon;

            [ReadOnly]
            public string _spriteTag;
        }

        //每次 repaint 都會問，所以重用同一批 row 物件，別讓 Odin 重建 drawer
        [System.NonSerialized]
        private List<FamilyPreviewRow> _previewRows;

        [PropertyOrder(103)]
        [BoxGroup("Editor Preview")]
        [ShowInInspector]
        [TableList(IsReadOnly = true, AlwaysExpanded = true)]
        [LabelText("各機種對照")]
        private List<FamilyPreviewRow> AllFamilyPreview
        {
            get
            {
                var families = (PromptDeviceFamily[])
                    System.Enum.GetValues(typeof(PromptDeviceFamily));
                _previewRows ??= new List<FamilyPreviewRow>();
                while (_previewRows.Count < families.Length)
                    _previewRows.Add(new FamilyPreviewRow());

                //只有 registry 才能指定機種查；直接掛單一 DeviceIconMapConfig 的專案就沒得對照
                var registry = ResolveFinder() as PromptIconRegistry;
                for (var i = 0; i < families.Length; i++)
                {
                    var row = _previewRows[i];
                    var family = families[i];
                    row._family = family.ToString();
                    row._icon = registry != null ? registry.GetIcon(_input, family) : null;
                    row._spriteTag = registry != null
                        ? registry.GetSpriteTag(_input, family) ?? "-"
                        : "-";
                }

                return _previewRows;
            }
        }

        [PropertyOrder(104)]
        [BoxGroup("Editor Preview")]
        [ShowInInspector]
        [ReadOnly]
        private string PreviewFinderSource
        {
            get
            {
                var finder = ResolveFinder();
                if (finder == null)
                    return "找不到 PromptIconRegistry";
                return finder is ScriptableObject so
                    ? $"{so.name} ({so.GetType().Name})"
                    : finder.GetType().Name;
            }
        }
#endif
    }

    public interface IHintSpriteFinder
    {
        public Sprite GetIcon(InputActionData input);
        public string GetSpriteTag(InputActionData input);
    }
}
