using System;
using System.Collections.Generic;
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

        //組成 TMP 的 <sprite> tag，inline 進文字流用；表裡沒有對應資料就回 null，交給上層 fallback
        public string GetSpriteTag(InputActionData input)
        {
            if (!TryGetEntry(input, out var entry))
                return null;
            if (string.IsNullOrEmpty(_spriteAssetName) || string.IsNullOrEmpty(entry._spriteName))
                return null;
            return $"<sprite=\"{_spriteAssetName}\" name=\"{entry._spriteName}\">";
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
