using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGInputAction
{
    //binding path -> icon 的對照表，依 InputSchemeWatcher.CurrentScheme 挑選對應裝置的 binding
    [CreateAssetMenu(
        menuName = "MonoFSM/Input/DeviceIconMapConfig",
        fileName = "DeviceIconMapConfig",
        order = 0
    )]
    public class DeviceIconMapConfig : ScriptableObject, IHintSpriteFinder
    {
        [Serializable]
        public class BindingIconEntry
        {
            //ex: <Gamepad>/buttonSouth, <Keyboard>/e, <Mouse>/leftButton
            public string _bindingPath;

            [PreviewField]
            public Sprite _icon;
        }

        [TableList]
        public List<BindingIconEntry> _entries = new();

        [NonSerialized]
        private Dictionary<string, Sprite> _lookup;

        [NonSerialized]
        private int _lookupBuiltCount = -1;

        public Sprite GetIcon(InputActionData input)
        {
            return GetIcon(input, InputSchemeWatcher.CurrentScheme);
        }

        public Sprite GetIcon(InputActionData input, InputSchemeType scheme)
        {
            var action = input != null && input._inputAction != null
                ? input._inputAction.action
                : null;
            if (action == null)
                return null;

            BuildLookupIfNeeded();

            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding.isComposite) //composite 本體沒有 path，取 part
                    continue;

                var path = binding.effectivePath;
                if (string.IsNullOrEmpty(path) || !MatchesScheme(path, scheme))
                    continue;

                if (_lookup.TryGetValue(path, out var sprite))
                    return sprite;
            }

            return null;
        }

        private static bool MatchesScheme(string path, InputSchemeType scheme)
        {
            return scheme switch
            {
                InputSchemeType.Gamepad => path.StartsWith("<Gamepad>")
                    || path.StartsWith("<Joystick>"),
                InputSchemeType.KeyboardMouse => path.StartsWith("<Keyboard>")
                    || path.StartsWith("<Mouse>"),
                _ => false,
            };
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null && _lookupBuiltCount == _entries.Count)
                return;

            _lookupBuiltCount = _entries.Count;
            _lookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry._bindingPath))
                    continue;
                _lookup[entry._bindingPath] = entry._icon;
            }
        }

        private void OnValidate()
        {
            _lookup = null; //編輯表格後重建
        }
    }
}
