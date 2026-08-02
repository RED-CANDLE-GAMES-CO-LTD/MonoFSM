using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGInputAction
{
    //依 PromptDeviceFamily 分派到對應的 DeviceIconMapConfig。
    //查詢 fallback 鏈：目前 family 的 config -> GamepadGeneric 的 config（僅當目前 family 屬於手把類）-> null。
    //這樣 Switch/PS/Xbox 只需要填「跟 generic 不一樣」的少數項目，其餘自動落回 generic。
    [CreateAssetMenu(
        menuName = "MonoFSM/Input/PromptIconRegistry",
        fileName = "PromptIconRegistry",
        order = 0
    )]
    public class PromptIconRegistry : ScriptableObject, IHintSpriteFinder
    {
        [Serializable]
        public class FamilyEntry
        {
            public PromptDeviceFamily _family;

            [Required]
            public DeviceIconMapConfig _config;
        }

        //所有裝置、所有 icon 的總倍率。覺得整體提示的 icon 偏小就調這一個，
        //單一裝置調 DeviceIconMapConfig._deviceIconScale，單顆鍵調該 entry 的 _iconScale（三層相乘）
        [LabelText("全域 icon 倍率")]
        [Tooltip("1 = 跟文字同高（TMP 預設）。放大會連帶撐開該行行高")]
        [PropertyRange(0.5f, 3f)]
        public float _globalIconScale = 1f;

        [TableList]
        public List<FamilyEntry> _entries = new();

        [NonSerialized]
        private Dictionary<PromptDeviceFamily, DeviceIconMapConfig> _lookup;

        [NonSerialized]
        private int _lookupBuiltCount = -1;

        public Sprite GetIcon(InputActionData input)
        {
            return GetIcon(input, InputSchemeWatcher.CurrentDeviceFamily);
        }

        //指定機種查詢：Editor 的多平台對照預覽用（runtime 走上面那個，跟著目前裝置）
        public Sprite GetIcon(InputActionData input, PromptDeviceFamily family)
        {
            ResolveConfigChain(family, out var primary, out var fallback);

            if (primary != null)
            {
                var icon = primary.GetIcon(input);
                if (icon != null)
                    return icon;
            }

            return fallback != null ? fallback.GetIcon(input) : null;
        }

        public string GetSpriteTag(InputActionData input)
        {
            return GetSpriteTag(input, InputSchemeWatcher.CurrentDeviceFamily);
        }

        public string GetSpriteTag(InputActionData input, PromptDeviceFamily family)
        {
            ResolveConfigChain(family, out var primary, out var fallback);

            if (primary != null)
            {
                var tag = primary.GetSpriteTag(input, _globalIconScale);
                if (tag != null)
                    return tag;
            }

            return fallback != null ? fallback.GetSpriteTag(input, _globalIconScale) : null;
        }

        //依指定 family 解出要依序嘗試的 config（跳過 null / 重複）。
        //鏈最多兩層，所以直接用 out 回傳，不走 iterator —— 這條路每幀會被提示 UI 呼叫，
        //yield 版本每次呼叫都會 new 一個 enumerator 物件造成 GC
        private void ResolveConfigChain(
            PromptDeviceFamily family,
            out DeviceIconMapConfig primary,
            out DeviceIconMapConfig fallback
        )
        {
            BuildLookupIfNeeded();

            primary = _lookup.TryGetValue(family, out var config) ? config : null;
            fallback = null;

            if (family == PromptDeviceFamily.KeyboardMouse)
                return; //鍵鼠沒有 generic fallback
            if (family == PromptDeviceFamily.GamepadGeneric)
                return; //自己就是 generic，避免重複查

            if (_lookup.TryGetValue(PromptDeviceFamily.GamepadGeneric, out var generic))
                fallback = generic;
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null && _lookupBuiltCount == _entries.Count)
                return;

            _lookupBuiltCount = _entries.Count;
            _lookup = new Dictionary<PromptDeviceFamily, DeviceIconMapConfig>();
            foreach (var entry in _entries)
            {
                if (entry._config == null)
                    continue;
                _lookup[entry._family] = entry._config;
            }
        }

        private void OnValidate()
        {
            _lookup = null; //編輯表格後重建
        }
    }
}
