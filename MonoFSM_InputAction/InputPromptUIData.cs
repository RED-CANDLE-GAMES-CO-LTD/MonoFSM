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

        //同一則提示要再串其他 action 時用（ex:「按住 Shift + W」），順序就是這裡的順序，接在 _input 後面。
        //單一 action 內的多顆鍵（WASD composite）不用填這裡，DeviceIconMapConfig 會自己把 part 串起來。
        public List<InputActionData> _extraInputs = new();

        //_input 與各個 _extraInputs 之間插的字（ex: "+"）；同一個 action 的多顆鍵之間不插
        public string _inputSeparator;

        //找不到對照 icon 時的替代圖（icon-only 場合用，走 GetIcon）
        [FormerlySerializedAs("placeHolderIcon")]
        public Sprite _placeHolderIcon;

        //圖文混排場合的最後保底：整則提示一顆 icon 都組不出來時（表裡完全沒這顆鍵、registry 沒載到）
        //改用這串文字，ex: "[互動鍵]"。單顆鍵缺圖請填 DeviceIconMapConfig entry 的 _fallbackText，
        //那層比較精細，也能反映裝置差異
        [Tooltip("整則提示都組不出 icon 時改顯示的文字，ex: [互動鍵]。留空＝token 會是空字串")]
        public string _fallbackText;

        //icon-only 場合用（ex: InputPromptUILabel 純圖示顯示）；多顆鍵的提示這裡只會拿到第一顆
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
            return BuildSpriteTag(null);
        }

        //_input + _extraInputs 依序串起來；family 有值＝指定機種查（Editor 的各機種對照預覽用）
        private string BuildSpriteTag(PromptDeviceFamily? family)
        {
            var finder = ResolveFinder();
            if (finder == null)
                return NullIfEmpty(_fallbackText);
            var registry = finder as PromptIconRegistry; //只有 registry 查得了指定機種

            _spriteTagBuilder.Clear();
            foreach (var input in EnumerateInputs())
            {
                var tag = family.HasValue && registry != null
                    ? registry.GetSpriteTag(input, family.Value)
                    : finder.GetSpriteTag(input);
                if (string.IsNullOrEmpty(tag))
                    continue; //這個 action 在這台裝置沒對照，其他的照樣串（漏填在各機種對照表看得出來）

                if (_spriteTagBuilder.Length > 0 && !string.IsNullOrEmpty(_inputSeparator))
                    _spriteTagBuilder.Append(_inputSeparator);
                _spriteTagBuilder.Append(tag);
            }

            return _spriteTagBuilder.Length == 0
                ? NullIfEmpty(_fallbackText)
                : _spriteTagBuilder.ToString();
        }

        //沒填 fallback 就維持回 null，讓 Editor 預覽照樣顯示「表裡沒有 sprite tag 資料」
        private static string NullIfEmpty(string text)
        {
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private static readonly System.Text.StringBuilder _spriteTagBuilder = new();

        //這則提示用到的所有 action（_input 在前）
        public IEnumerable<InputActionData> EnumerateInputs()
        {
            if (_input != null)
                yield return _input;
            if (_extraInputs == null)
                yield break;
            foreach (var extra in _extraInputs)
                if (extra != null)
                    yield return extra;
        }

        //放在 Resources 底下的那份 registry，是 build 出去唯一保證會被打包、且不靠場景擺設的來源
        private const string ResourcesRegistryName = "PromptIconRegistry";

        //查詢順序：installer 注入 -> Resources 的那份 -> (Editor) 專案裡任一份。
        //中間那條 Editor 也要走，不然「場景忘了放 installer」會被 EditorPreviewFinder 蓋掉、在編輯器完全測不出來
        //（踩過：build 出去所有圖文提示的 <sprite> 全變空字串，Editor 卻一切正常）
        private static IHintSpriteFinder ResolveFinder()
        {
            if (_spriteFinder != null)
                return _spriteFinder;

            var fromResources = ResourcesFinder;
            if (fromResources != null)
                return fromResources;

#if UNITY_EDITOR
            var editorFinder = EditorPreviewFinder;
            if (editorFinder != null)
            {
                LogFinderProblemOnce(
                    $"Resources 裡沒有 {ResourcesRegistryName}.asset，現在是靠 Editor 專用的全專案搜尋撐著；"
                        + "build 出去會抓不到 icon，請把 PromptIconRegistry 放進任一 Resources 資料夾");
                return editorFinder;
            }
#endif
            LogFinderProblemOnce(
                "找不到 IHintSpriteFinder：場景沒放 HintSpriteFinderInstaller，"
                    + $"Resources/{ResourcesRegistryName} 也不存在，所有圖文提示的 <sprite> 會是空字串");
            return null;
        }

        private static PromptIconRegistry _resourcesFinder;
        private static bool _resourcesFinderLoadAttempted;

        //ResolveFinder 是每幀被問的（HasValueChanged -> GetSpriteTag），所以載到就記住、載不到也不重試
        private static PromptIconRegistry ResourcesFinder
        {
            get
            {
                if (_resourcesFinder != null)
                    return _resourcesFinder;
                if (_resourcesFinderLoadAttempted)
                    return null;

                _resourcesFinderLoadAttempted = true;
                _resourcesFinder = Resources.Load<PromptIconRegistry>(ResourcesRegistryName);
                return _resourcesFinder;
            }
        }

        private static bool _finderProblemLogged;

        //每幀都會走到，同一個問題只印一次
        private static void LogFinderProblemOnce(string message)
        {
            if (_finderProblemLogged)
                return;
            _finderProblemLogged = true;
            Debug.LogError($"[InputPromptUIData] {message}");
        }

        //關掉 domain reload 時 static 會跨 Play Mode 殘留，錯誤變成只有第一次 Play 印得出來
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _spriteFinder = null;
            _resourcesFinder = null;
            _resourcesFinderLoadAttempted = false;
            _finderProblemLogged = false;
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
                    row._spriteTag = registry != null ? BuildSpriteTag(family) ?? "-" : "-";
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
