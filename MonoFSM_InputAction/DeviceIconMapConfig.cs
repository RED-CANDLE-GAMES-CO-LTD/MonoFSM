using System;
using System.Collections.Generic;
using System.Globalization;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCGInputAction
{
    //binding path -> icon 的對照表。一份 config 只服務一個 PromptDeviceFamily（由 PromptIconRegistry 決定要用哪份），
    //所以這裡不再需要 scheme 篩選。
    [CreateAssetMenu(
        menuName = "MonoFSM/Input/DeviceIconMapConfig",
        fileName = "DeviceIconMapConfig",
        order = 0
    )]
    [Searchable]
    public partial class DeviceIconMapConfig : ScriptableObject, IHintSpriteFinder
    {
        [Serializable]
        public class BindingIconEntry
        {
            //ex: <Gamepad>/buttonSouth, <Keyboard>/e, <Mouse>/leftButton
            //下拉選單來源＝專案裡所有 InputPromptUIData 用到的 binding path（依裝置分組），避免手打路徑打錯
            [ValueDropdown("GetBindingPathOptions", AppendNextDrawer = true)]
            public string _bindingPath;

            [PreviewField]
            public Sprite _icon;

            //該 sprite asset 內的 sprite 名稱（沿用 Kenney 原名），用來組 <sprite> tag inline 進文字流
            [ValueDropdown("GetSpriteNameOptions", AppendNextDrawer = true)]
            public string _spriteName;

            //單顆補償：圖案在 64px 格子裡偏矮/偏小時放大（ex: shift 的圖只有一般按鍵的一半高）。1 = 不調整。
            //最終大小 = registry 全域 × config 裝置級 × 這個值，數字不用手算，按 config 上的自動校正按鈕
            [TableColumnWidth(70, false)]
            public float _iconScale = 1f;

            //舊資料：sprite asset 名稱本來填在每一筆，現在提到 config 層級（見 DeviceIconMapConfig._spriteAssetName）
            [HideInInspector] [FormerlySerializedAs("_spriteAssetName")]
            public string _legacySpriteAssetName;

            //dropdown / 反查 sprite 需要 config 層級的 sprite asset 名稱；entry 拿不到 parent，由 config 回填
            [NonSerialized] public DeviceIconMapConfig _owner;

#if UNITY_EDITOR
            private static IEnumerable<ValueDropdownItem<string>> GetBindingPathOptions() =>
                PromptIconMapEditorUtility.GetBindingPathDropdown();

            //只在 Editor 填表時提供下拉選單，避免手動打字打錯 sprite 名稱；找不到對應 asset 就回空清單，交給使用者手動輸入
            private IEnumerable<string> GetSpriteNameOptions() =>
                PromptIconMapEditorUtility.GetSpriteNames(_owner?._spriteAssetName);

            //表裡沒填 _icon（只填了 TMP sprite 名稱）時，從 sprite sheet 反查同名 Sprite，
            //讓 Editor 預覽 / 一鍵補圖有東西可用（Kenney sheet 的切片名稱與 TMP sprite 名稱同名）
            public Sprite ResolveSpriteFromTmpAsset() =>
                PromptIconMapEditorUtility.FindSpriteInSheet(_owner?._spriteAssetName, _spriteName);
#endif
        }

        //一份 config 只服務一個機種，所以整份共用一個 TMP Sprite Asset
        [ValueDropdown("GetSpriteAssetNameOptions", AppendNextDrawer = true)]
        public string _spriteAssetName;

        //這台裝置所有 icon 的倍率。整批覺得太小就調這個；單顆圖比例不對調 entry 的 _iconScale；
        //所有裝置一起調則是 PromptIconRegistry._globalIconScale
        [BoxGroup("Icon 大小")]
        [LabelText("裝置級倍率")]
        [Tooltip("這份 config 的所有 icon 一起放大/縮小，1 = 不調整")]
        public float _deviceIconScale = 1f;

        //自動校正用：Kenney sheet 是 64px 一格，一般按鍵的圖佔 48px，shift / space 這種寬鍵只有 28~32px，
        //所以要把每顆圖的實際高度都補到同一個基準，看起來才一樣大
        [BoxGroup("Icon 大小")]
        [LabelText("自動校正基準高度 (px)")]
        [Tooltip("自動校正時把每顆圖的實際像素高度對齊到這個值；Kenney sheet 的一般按鍵是 48")]
        public float _iconAutoFitBaseHeight = 48f;

        [TableList]
        public List<BindingIconEntry> _entries = new();

#if UNITY_EDITOR
        private static IEnumerable<string> GetSpriteAssetNameOptions() =>
            PromptIconMapEditorUtility.GetSpriteAssetNames();
#endif

        [NonSerialized] private Dictionary<string, BindingIconEntry> _lookup;

        [NonSerialized]
        private int _lookupBuiltCount = -1;

        public Sprite GetIcon(InputActionData input)
        {
            if (!TryGetEntry(input, out var entry))
                return null;
#if UNITY_EDITOR
            //只填 sprite tag 沒填 _icon 也要能預覽（runtime 就靠下面的按鈕先補進 _icon）
            if (entry._icon == null)
                return entry.ResolveSpriteFromTmpAsset();
#endif
            return entry._icon;
        }

#if UNITY_EDITOR
        [Button("從 TMP Sprite Asset 補齊空的 _icon")]
        private void FillMissingIcons()
        {
            var filled = 0;
            foreach (var entry in _entries)
            {
                if (entry._icon != null)
                    continue;
                var sprite = entry.ResolveSpriteFromTmpAsset();
                if (sprite == null)
                    continue;
                entry._icon = sprite;
                filled++;
            }

            if (filled == 0)
                return;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            Debug.Log($"[DeviceIconMapConfig] 補齊 {filled} 個 _icon", this);
        }
#endif

        //組成 TMP 的 <sprite> tag，inline 進文字流用；表裡沒有對應資料就回 null，交給上層 fallback。
        //WASD 這種 composite 會串出多顆 tag（見 TryGetEntries）
        private static readonly List<BindingIconEntry> _tagEntryBuffer = new();
        private static readonly System.Text.StringBuilder _tagBuilder = new();

        public string GetSpriteTag(InputActionData input)
        {
            return GetSpriteTag(input, 1f);
        }

        //extraScale：上層（PromptIconRegistry）的全域倍率，跟裝置級 / 單顆倍率相乘後包成一層 <size>。
        //TMP 的 inline sprite 大小是跟著當下字級走的，所以放大 icon＝就地把字級撐大再還原
        public string GetSpriteTag(InputActionData input, float extraScale)
        {
            if (string.IsNullOrEmpty(_spriteAssetName))
                return null;
            if (!TryGetEntries(input, _tagEntryBuffer))
                return null;

            _tagBuilder.Clear();
            foreach (var entry in _tagEntryBuffer)
            {
                if (string.IsNullOrEmpty(entry._spriteName))
                    continue; //這顆還沒填 sprite 名稱，其他顆照樣顯示（缺哪顆在 Editor 的各機種對照表看得出來）

                var scale = SanitizeScale(extraScale)
                            * SanitizeScale(_deviceIconScale)
                            * SanitizeScale(entry._iconScale);
                var scaled = !Mathf.Approximately(scale, 1f);
                if (scaled)
                {
                    //一定要 InvariantCulture，不然 zh/de 這種 locale 會輸出 "150,5%" 讓 TMP 解析失敗
                    _tagBuilder.Append("<size=");
                    _tagBuilder.Append((scale * 100f).ToString("0.#", CultureInfo.InvariantCulture));
                    _tagBuilder.Append("%>");
                }

                _tagBuilder.Append("<sprite=\"");
                _tagBuilder.Append(_spriteAssetName);
                _tagBuilder.Append("\" name=\"");
                _tagBuilder.Append(entry._spriteName);
                _tagBuilder.Append("\">");

                if (scaled)
                    _tagBuilder.Append("</size>");
            }

            return _tagBuilder.Length == 0 ? null : _tagBuilder.ToString();
        }

        //舊資料沒有這個欄位（反序列化成 0）、或手動填了 0 / 負數，都當作不調整
        private static float SanitizeScale(float scale)
        {
            return scale > 0f ? scale : 1f;
        }

        //WASD 這種一個 action 對多顆鍵的情況：composite 的每個 part 各有 icon，要全部收集。
        //兩層去重：
        //1. 一個 action 常常有好幾組 binding（WASD composite、leftStick…），只取「第一組在這份 config
        //   裡查得到 icon」的那組。分組規則：composite 本體開一組（part 歸該組），非 composite 的單一
        //   binding 自己算一組。
        //2. 同一個 composite 裡，同一個 part（up/down/left/right）常綁了兩套鍵（W 和 ↑），只取先出現的
        //   那顆，不然 WASD 會跟方向鍵串成 8 顆。
        private static readonly HashSet<string> _seenPartNames = new(StringComparer.OrdinalIgnoreCase);

        public bool TryGetEntries(InputActionData input, List<BindingIconEntry> results)
        {
            results.Clear();
            _seenPartNames.Clear();
            var action = input != null && input._inputAction != null
                ? input._inputAction.action
                : null;
            if (action == null)
                return false;

            BuildLookupIfNeeded();

            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];

                if (binding.isComposite || !binding.isPartOfComposite)
                {
                    //換組了；前一組已經有 match 就用前一組，不要跟這組混在一起
                    if (results.Count > 0)
                        return true;
                    _seenPartNames.Clear();
                }

                if (binding.isComposite) //composite 本體沒有 path，路徑在 part 上
                    continue;

                var path = binding.effectivePath;
                if (string.IsNullOrEmpty(path))
                    continue;

                if (!_lookup.TryGetValue(path, out var entry))
                    continue;

                //同一個 part 的第二套鍵（WASD 之外還綁了方向鍵）不再收。
                //擺在查表之後：W 沒填 icon 但 ↑ 有填時，還能落到 ↑ 去
                if (binding.isPartOfComposite &&
                    !string.IsNullOrEmpty(binding.name) &&
                    !_seenPartNames.Add(binding.name))
                    continue;

                if (results.Contains(entry)) //同一顆鍵在同一組綁了兩次
                    continue;

                results.Add(entry);
            }

            return results.Count > 0;
        }

        public bool TryGetEntry(InputActionData input, out BindingIconEntry entry)
        {
            entry = null;
            var action = input != null && input._inputAction != null
                ? input._inputAction.action
                : null;
            if (action == null)
                return false;

            BuildLookupIfNeeded();

            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding.isComposite) //composite 本體沒有 path，取 part
                    continue;

                var path = binding.effectivePath;
                if (string.IsNullOrEmpty(path))
                    continue;

                if (_lookup.TryGetValue(path, out entry))
                    return true;
            }

            return false;
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null && _lookupBuiltCount == _entries.Count)
                return;

            _lookupBuiltCount = _entries.Count;
            _lookup = new Dictionary<string, BindingIconEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _entries)
            {
                entry._owner = this;
                if (string.IsNullOrEmpty(entry._bindingPath))
                    continue;
                _lookup[entry._bindingPath] = entry;
            }
        }

        private void OnEnable()
        {
            SyncOwners();
        }

        private void OnValidate()
        {
            _lookup = null; //編輯表格後重建
            SyncOwners();
        }

        //entry 的 dropdown / sprite 反查要回頭問 config 的 _spriteAssetName，順手把舊的 per-entry 名稱搬上來
        private void SyncOwners()
        {
            var migrated = false;
            foreach (var entry in _entries)
            {
                entry._owner = this;
                if (string.IsNullOrEmpty(entry._legacySpriteAssetName))
                    continue;
                if (string.IsNullOrEmpty(_spriteAssetName))
                    _spriteAssetName = entry._legacySpriteAssetName;
                entry._legacySpriteAssetName = null;
                migrated = true;
            }

#if UNITY_EDITOR
            //搬完要存回 asset，不然每次載入都得再搬一輪（OnEnable 當下不能直接動 asset，延到下一格）
            if (migrated && !Application.isPlaying)
                EditorApplication.delayCall += () =>
                {
                    if (this == null)
                        return;
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssetIfDirty(this);
                };
#endif
        }
    }
}
