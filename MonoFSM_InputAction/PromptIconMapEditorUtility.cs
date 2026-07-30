#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace RCGInputAction
{
    //DeviceIconMapConfig 填表用的 Editor 工具：從專案裡的 InputPromptUIData 反查有哪些 binding path 要填。
    //只有 Editor 需要，所以整包包在 UNITY_EDITOR 裡（放在 runtime asmdef 是因為 DeviceIconMapConfig 的
    //[ValueDropdown] / [Button] 得直接呼叫，跨 asmdef 反向依賴不成立）
    public static class PromptIconMapEditorUtility
    {
        //這裡每個查詢都要 FindAssets + LoadAssetAtPath（TMP Sprite Asset 還會連 material/texture 一起載），
        //但呼叫端是 [ValueDropdown] / [InfoBox] 的 resolver —— Odin 每次 repaint、每個 entry 都會問一次，
        //不 cache 的話 entry 一多 Inspector 就卡到要等好幾秒。資產增刪（projectChanged）或 TTL 到才重算。
        private const double CacheTtlSeconds = 30;
        private static double _cacheStamp = -1;

        private static List<BindingUsage> _bindingUsages;
        private static List<ValueDropdownItem<string>> _bindingDropdown;
        private static List<string> _spriteAssetNames;
        private static readonly Dictionary<string, Object> _spriteAssetByName = new();
        private static readonly Dictionary<string, List<string>> _spriteNamesByAsset = new();
        private static readonly Dictionary<string, Sprite> _spriteBySheetKey = new();
        private static readonly Dictionary<DeviceIconMapConfig, PromptDeviceFamily?> _ownerFamily =
            new();

        [InitializeOnLoadMethod]
        private static void HookInvalidation()
        {
            EditorApplication.projectChanged -= InvalidateCache;
            EditorApplication.projectChanged += InvalidateCache;
        }

        public static void InvalidateCache()
        {
            _cacheStamp = -1;
            _bindingUsages = null;
            _bindingDropdown = null;
            _spriteAssetNames = null;
            _spriteAssetByName.Clear();
            _spriteNamesByAsset.Clear();
            _spriteBySheetKey.Clear();
            _ownerFamily.Clear();
        }

        private static void EnsureFresh()
        {
            var now = EditorApplication.timeSinceStartup;
            if (_cacheStamp >= 0 && now - _cacheStamp < CacheTtlSeconds)
                return;
            InvalidateCache();
            _cacheStamp = now;
        }

        //一筆「專案裡實際用到的 binding」：哪個 prompt / action 用到、路徑是什麼
        public struct BindingUsage
        {
            public string _path; //ex: <Gamepad>/buttonSouth
            public string _layout; //ex: Gamepad（路徑 <> 內的裝置 layout 名）
            public string _actionName;
            public string _promptName;
        }

        //Unity 端會出現的手把 layout 名稱；binding path 直接寫特定機種（ex: <SwitchProControllerHID>/dpad/up）時也要算手把
        private static readonly HashSet<string> GamepadLayouts = new()
        {
            "Gamepad",
            "DualShockGamepad",
            "DualSenseGamepadHID",
            "DualShock4GamepadHID",
            "SwitchProControllerHID",
            "XInputController",
            "XboxOneGamepad",
        };

        private static readonly HashSet<string> KeyboardMouseLayouts = new()
        {
            "Keyboard",
            "Mouse",
            "Pointer",
        };

        public static bool IsLayoutOfFamily(string layout, PromptDeviceFamily family)
        {
            if (string.IsNullOrEmpty(layout))
                return false;
            return family == PromptDeviceFamily.KeyboardMouse
                ? KeyboardMouseLayouts.Contains(layout)
                : GamepadLayouts.Contains(layout);
        }

        //掃專案裡所有 InputPromptUIData -> _input -> action 的 bindings，回傳去重後的路徑清單
        public static List<BindingUsage> CollectBindingUsages()
        {
            EnsureFresh();
            return _bindingUsages ??= CollectBindingUsagesUncached();
        }

        private static List<BindingUsage> CollectBindingUsagesUncached()
        {
            var result = new List<BindingUsage>();
            var seen = new HashSet<string>();

            foreach (var guid in AssetDatabase.FindAssets("t:InputPromptUIData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prompt = AssetDatabase.LoadAssetAtPath<InputPromptUIData>(path);
                if (prompt == null || prompt._input == null)
                    continue;

                var action = prompt._input._inputAction != null
                    ? prompt._input._inputAction.action
                    : null;
                if (action == null)
                    continue;

                var bindings = action.bindings;
                for (var i = 0; i < bindings.Count; i++)
                {
                    var binding = bindings[i];
                    if (binding.isComposite) //composite 本體沒有 path，路徑在 part 上（跟 runtime 查表一致）
                        continue;

                    var bindingPath = binding.effectivePath;
                    if (string.IsNullOrEmpty(bindingPath) || !seen.Add(bindingPath))
                        continue;

                    result.Add(new BindingUsage
                    {
                        _path = bindingPath,
                        _layout = ExtractLayout(bindingPath),
                        _actionName = action.name,
                        _promptName = prompt.name,
                    });
                }
            }

            return result;
        }

        //<Gamepad>/buttonSouth -> Gamepad；*/{Submit} 這種沒有具體 layout 的回空字串
        public static string ExtractLayout(string bindingPath)
        {
            if (string.IsNullOrEmpty(bindingPath) || bindingPath[0] != '<')
                return string.Empty;
            var close = bindingPath.IndexOf('>');
            return close <= 1 ? string.Empty : bindingPath.Substring(1, close - 1);
        }

        //<Gamepad>/dpad/up -> dpad/up；<Keyboard>/e -> e
        public static string ExtractControl(string bindingPath)
        {
            if (string.IsNullOrEmpty(bindingPath))
                return string.Empty;
            var slash = bindingPath.IndexOf('/');
            return slash < 0 ? string.Empty : bindingPath.Substring(slash + 1);
        }

        //依裝置分組的下拉選單，label 帶上是哪個 action / prompt 在用，填表時不用回頭查 inputactions
        public static IEnumerable<ValueDropdownItem<string>> GetBindingPathDropdown()
        {
            EnsureFresh();
            return _bindingDropdown ??= BuildBindingPathDropdown();
        }

        private static List<ValueDropdownItem<string>> BuildBindingPathDropdown()
        {
            var result = new List<ValueDropdownItem<string>>();
            foreach (var usage in CollectBindingUsages())
            {
                var group = string.IsNullOrEmpty(usage._layout) ? "Other" : usage._layout;
                var control = ExtractControl(usage._path);
                result.Add(new ValueDropdownItem<string>(
                    $"{group}/{usage._actionName} － {control}",
                    usage._path));
            }

            return result;
        }

        public static IEnumerable<string> GetSpriteAssetNames()
        {
            EnsureFresh();
            if (_spriteAssetNames != null)
                return _spriteAssetNames;

            _spriteAssetNames = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:TMP_SpriteAsset"))
            {
                //只要名字，走 path 的檔名就夠，不必 LoadMainAssetAtPath 把 sprite asset 連 material/texture 載進來
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                    _spriteAssetNames.Add(System.IO.Path.GetFileNameWithoutExtension(path));
            }

            _spriteAssetNames.Sort();
            return _spriteAssetNames;
        }

        //name -> TMP_SpriteAsset（不引用 TMPro 型別，回 Object 讓呼叫端用 SerializedObject 讀）
        public static Object FindSpriteAsset(string spriteAssetName)
        {
            if (string.IsNullOrEmpty(spriteAssetName))
                return null;

            EnsureFresh();
            if (_spriteAssetByName.TryGetValue(spriteAssetName, out var cached))
                return cached;

            Object found = null;
            foreach (var guid in AssetDatabase.FindAssets($"t:TMP_SpriteAsset {spriteAssetName}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != spriteAssetName)
                    continue; //名字不合就別載，FindAssets 是模糊比對
                found = AssetDatabase.LoadMainAssetAtPath(path);
                if (found != null)
                    break;
            }

            _spriteAssetByName[spriteAssetName] = found;
            return found;
        }

        //sprite asset 裡有哪些 sprite 名稱（讀 m_SpriteCharacterTable）
        public static List<string> GetSpriteNames(string spriteAssetName)
        {
            EnsureFresh();
            if (string.IsNullOrEmpty(spriteAssetName))
                return EmptyNames;
            if (_spriteNamesByAsset.TryGetValue(spriteAssetName, out var cached))
                return cached;

            var result = new List<string>();
            var asset = FindSpriteAsset(spriteAssetName);
            if (asset != null)
            {
                var table = new SerializedObject(asset).FindProperty("m_SpriteCharacterTable");
                if (table != null && table.isArray)
                    for (var i = 0; i < table.arraySize; i++)
                    {
                        var nameProp = table
                            .GetArrayElementAtIndex(i)
                            .FindPropertyRelative("m_Name");
                        if (nameProp != null)
                            result.Add(nameProp.stringValue);
                    }
            }

            _spriteNamesByAsset[spriteAssetName] = result;
            return result;
        }

        private static readonly List<string> EmptyNames = new();

        //從 sprite asset 的 sprite sheet 反查同名 Sprite（Kenney sheet 的切片名稱與 TMP sprite 名同名）
        public static Sprite FindSpriteInSheet(string spriteAssetName, string spriteName)
        {
            if (string.IsNullOrEmpty(spriteAssetName) || string.IsNullOrEmpty(spriteName))
                return null;

            EnsureFresh();
            var key = spriteAssetName + "/" + spriteName;
            if (_spriteBySheetKey.TryGetValue(key, out var cached))
                return cached;

            Sprite found = null;
            var asset = FindSpriteAsset(spriteAssetName);
            var sheet = asset != null
                ? new SerializedObject(asset).FindProperty("spriteSheet")?.objectReferenceValue
                : null;
            var sheetPath = sheet != null ? AssetDatabase.GetAssetPath(sheet) : null;
            if (!string.IsNullOrEmpty(sheetPath))
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
                    if (sub is Sprite sprite && sprite.name == spriteName)
                    {
                        found = sprite;
                        break;
                    }

            _spriteBySheetKey[key] = found;
            return found;
        }

        //config 沒有自己記 family（由 PromptIconRegistry 分派），要自動填表就從 registry 反查
        public static bool TryFindOwnerFamily(DeviceIconMapConfig config, out PromptDeviceFamily family)
        {
            family = default;
            if (config == null)
                return false;

            EnsureFresh();
            if (_ownerFamily.TryGetValue(config, out var cached))
            {
                if (cached == null)
                    return false;
                family = cached.Value;
                return true;
            }

            var found = FindOwnerFamilyUncached(config, out family);
            _ownerFamily[config] = found ? family : null;
            return found;
        }

        private static bool FindOwnerFamilyUncached(
            DeviceIconMapConfig config,
            out PromptDeviceFamily family
        )
        {
            family = default;
            foreach (var guid in AssetDatabase.FindAssets("t:PromptIconRegistry"))
            {
                var registry = AssetDatabase.LoadAssetAtPath<PromptIconRegistry>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (registry == null)
                    continue;

                foreach (var entry in registry._entries)
                    if (entry._config == config)
                    {
                        family = entry._family;
                        return true;
                    }
            }

            return false;
        }
    }
}
#endif
