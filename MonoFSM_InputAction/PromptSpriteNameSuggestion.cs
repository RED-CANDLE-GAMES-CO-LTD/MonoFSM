#if UNITY_EDITOR
using System.Collections.Generic;

namespace RCGInputAction
{
    //binding path -> Kenney sprite 名稱的建議值，給 DeviceIconMapConfig 自動填表用。
    //只是「建議」：填完仍可在 Inspector 的下拉選單改掉，而且填之前會先確認該 sprite 真的存在於對應的 TMP Sprite Asset。
    public static class PromptSpriteNameSuggestion
    {
        //手把類：Unity 的 control 名 -> 各機種 sprite 名。
        //Nintendo 的 A/B、X/Y 位置與 Xbox 相反，所以 buttonSouth 對 switch_button_b（照實體位置對，不是照字母）
        private static readonly Dictionary<string, string> Xbox = new()
        {
            ["buttonSouth"] = "xbox_button_a",
            ["buttonEast"] = "xbox_button_b",
            ["buttonWest"] = "xbox_button_x",
            ["buttonNorth"] = "xbox_button_y",
            ["leftShoulder"] = "xbox_lb",
            ["rightShoulder"] = "xbox_rb",
            ["leftTrigger"] = "xbox_lt",
            ["rightTrigger"] = "xbox_rt",
            ["start"] = "xbox_button_menu",
            ["select"] = "xbox_button_view",
            ["leftStick"] = "xbox_stick_l",
            ["rightStick"] = "xbox_stick_r",
            ["leftStickPress"] = "xbox_stick_l_press",
            ["rightStickPress"] = "xbox_stick_r_press",
            ["leftStick/up"] = "xbox_stick_l_up",
            ["leftStick/down"] = "xbox_stick_l_down",
            ["leftStick/left"] = "xbox_stick_l_left",
            ["leftStick/right"] = "xbox_stick_l_right",
            ["rightStick/up"] = "xbox_stick_r_up",
            ["rightStick/down"] = "xbox_stick_r_down",
            ["rightStick/left"] = "xbox_stick_r_left",
            ["rightStick/right"] = "xbox_stick_r_right",
            ["dpad"] = "xbox_dpad_all",
            ["dpad/up"] = "xbox_dpad_up",
            ["dpad/down"] = "xbox_dpad_down",
            ["dpad/left"] = "xbox_dpad_left",
            ["dpad/right"] = "xbox_dpad_right",
        };

        private static readonly Dictionary<string, string> PlayStation = new()
        {
            ["buttonSouth"] = "playstation_button_cross",
            ["buttonEast"] = "playstation_button_circle",
            ["buttonWest"] = "playstation_button_square",
            ["buttonNorth"] = "playstation_button_triangle",
            ["leftShoulder"] = "playstation_trigger_l1",
            ["rightShoulder"] = "playstation_trigger_r1",
            ["leftTrigger"] = "playstation_trigger_l2",
            ["rightTrigger"] = "playstation_trigger_r2",
            ["start"] = "playstation5_button_options",
            ["select"] = "playstation5_button_create",
            ["leftStick"] = "playstation_stick_l",
            ["rightStick"] = "playstation_stick_r",
            ["leftStickPress"] = "playstation_button_l3",
            ["rightStickPress"] = "playstation_button_r3",
            ["leftStick/up"] = "playstation_stick_l_up",
            ["leftStick/down"] = "playstation_stick_l_down",
            ["leftStick/left"] = "playstation_stick_l_left",
            ["leftStick/right"] = "playstation_stick_l_right",
            ["rightStick/up"] = "playstation_stick_r_up",
            ["rightStick/down"] = "playstation_stick_r_down",
            ["rightStick/left"] = "playstation_stick_r_left",
            ["rightStick/right"] = "playstation_stick_r_right",
            ["dpad"] = "playstation_dpad_all",
            ["dpad/up"] = "playstation_dpad_up",
            ["dpad/down"] = "playstation_dpad_down",
            ["dpad/left"] = "playstation_dpad_left",
            ["dpad/right"] = "playstation_dpad_right",
        };

        private static readonly Dictionary<string, string> Switch = new()
        {
            ["buttonSouth"] = "switch_button_b",
            ["buttonEast"] = "switch_button_a",
            ["buttonWest"] = "switch_button_y",
            ["buttonNorth"] = "switch_button_x",
            ["leftShoulder"] = "switch_button_l",
            ["rightShoulder"] = "switch_button_r",
            ["leftTrigger"] = "switch_button_zl",
            ["rightTrigger"] = "switch_button_zr",
            ["start"] = "switch_button_plus",
            ["select"] = "switch_button_minus",
            ["leftStick"] = "switch_stick_l",
            ["rightStick"] = "switch_stick_r",
            ["leftStickPress"] = "switch_stick_l_press",
            ["rightStickPress"] = "switch_stick_r_press",
            ["leftStick/up"] = "switch_stick_l_up",
            ["leftStick/down"] = "switch_stick_l_down",
            ["leftStick/left"] = "switch_stick_l_left",
            ["leftStick/right"] = "switch_stick_l_right",
            ["rightStick/up"] = "switch_stick_r_up",
            ["rightStick/down"] = "switch_stick_r_down",
            ["rightStick/left"] = "switch_stick_r_left",
            ["rightStick/right"] = "switch_stick_r_right",
            ["dpad"] = "switch_dpad_all",
            ["dpad/up"] = "switch_dpad_up",
            ["dpad/down"] = "switch_dpad_down",
            ["dpad/left"] = "switch_dpad_left",
            ["dpad/right"] = "switch_dpad_right",
        };

        //Kenney 的 generic sheet 很小：四顆面鍵沒有分別的圖，只能都給同一顆通用鍵；dpad 沒有圖，留空讓上層用 placeholder
        private static readonly Dictionary<string, string> GamepadGeneric = new()
        {
            ["buttonSouth"] = "generic_button_circle",
            ["buttonEast"] = "generic_button_circle",
            ["buttonWest"] = "generic_button_circle",
            ["buttonNorth"] = "generic_button_circle",
            ["leftShoulder"] = "generic_button_trigger_a",
            ["rightShoulder"] = "generic_button_trigger_b",
            ["leftTrigger"] = "generic_button_trigger_c",
            ["rightTrigger"] = "generic_button_trigger_c",
            ["leftStick"] = "generic_stick",
            ["rightStick"] = "generic_stick",
            ["leftStickPress"] = "generic_stick_press",
            ["rightStickPress"] = "generic_stick_press",
            ["leftStick/up"] = "generic_stick_up",
            ["leftStick/down"] = "generic_stick_down",
            ["leftStick/left"] = "generic_stick_left",
            ["leftStick/right"] = "generic_stick_right",
            ["rightStick/up"] = "generic_stick_up",
            ["rightStick/down"] = "generic_stick_down",
            ["rightStick/left"] = "generic_stick_left",
            ["rightStick/right"] = "generic_stick_right",
        };

        //鍵盤上有專屬圖的按鍵；單一字母 / 數字走下面的規則直接組名字
        private static readonly Dictionary<string, string> KeyboardSpecial = new()
        {
            ["space"] = "keyboard_space",
            ["enter"] = "keyboard_enter",
            ["numpadEnter"] = "keyboard_numpad_enter",
            ["escape"] = "keyboard_escape",
            ["tab"] = "keyboard_tab",
            ["backspace"] = "keyboard_backspace",
            ["delete"] = "keyboard_delete",
            ["insert"] = "keyboard_insert",
            ["home"] = "keyboard_home",
            ["end"] = "keyboard_end",
            ["pageUp"] = "keyboard_page_up",
            ["pageDown"] = "keyboard_page_down",
            ["capsLock"] = "keyboard_capslock",
            ["leftShift"] = "keyboard_shift",
            ["rightShift"] = "keyboard_shift",
            ["shift"] = "keyboard_shift",
            ["leftCtrl"] = "keyboard_ctrl",
            ["rightCtrl"] = "keyboard_ctrl",
            ["ctrl"] = "keyboard_ctrl",
            ["leftAlt"] = "keyboard_alt",
            ["rightAlt"] = "keyboard_alt",
            ["alt"] = "keyboard_alt",
            ["leftCommand"] = "keyboard_command",
            ["rightCommand"] = "keyboard_command",
            ["upArrow"] = "keyboard_arrow_up",
            ["downArrow"] = "keyboard_arrow_down",
            ["leftArrow"] = "keyboard_arrow_left",
            ["rightArrow"] = "keyboard_arrow_right",
            ["comma"] = "keyboard_comma",
            ["period"] = "keyboard_period",
            ["semicolon"] = "keyboard_semicolon",
            ["quote"] = "keyboard_quote",
            ["slash"] = "keyboard_slash_forward",
            ["backslash"] = "keyboard_slash_back",
            ["minus"] = "keyboard_minus",
            ["equals"] = "keyboard_equals",
            ["leftBracket"] = "keyboard_bracket_open",
            ["rightBracket"] = "keyboard_bracket_close",
            ["backquote"] = "keyboard_tilde",
        };

        private static readonly Dictionary<string, string> MouseControls = new()
        {
            ["leftButton"] = "mouse_left",
            ["rightButton"] = "mouse_right",
            ["middleButton"] = "mouse_scroll",
            ["forwardButton"] = "mouse_side_forward",
            ["backButton"] = "mouse_side_back",
            ["scroll"] = "mouse_scroll_vertical",
            ["scroll/y"] = "mouse_scroll_vertical",
            ["scroll/up"] = "mouse_scroll_up",
            ["scroll/down"] = "mouse_scroll_down",
            ["scroll/x"] = "mouse_horizontal",
            ["delta"] = "mouse_move",
            ["position"] = "mouse_move",
        };

        //回傳建議的 sprite 名稱；沒把握就回 null（讓呼叫端留空並列進 log，人工補）
        public static string Suggest(PromptDeviceFamily family, string bindingPath)
        {
            var layout = PromptIconMapEditorUtility.ExtractLayout(bindingPath);
            var control = PromptIconMapEditorUtility.ExtractControl(bindingPath);
            if (string.IsNullOrEmpty(control))
                return null;

            if (family == PromptDeviceFamily.KeyboardMouse)
                return layout == "Keyboard" ? SuggestKeyboard(control) : Lookup(MouseControls, control);

            var table = family switch
            {
                PromptDeviceFamily.Xbox => Xbox,
                PromptDeviceFamily.PlayStation => PlayStation,
                PromptDeviceFamily.Switch => Switch,
                _ => GamepadGeneric,
            };
            return Lookup(table, control);
        }

        private static string SuggestKeyboard(string control)
        {
            if (control.Length == 1 && (char.IsLetterOrDigit(control[0])))
                return "keyboard_" + char.ToLowerInvariant(control[0]);

            //f1 ~ f12
            if (control.Length is 2 or 3 && (control[0] == 'f' || control[0] == 'F')
                && int.TryParse(control.Substring(1), out var fn) && fn is >= 1 and <= 12)
                return "keyboard_f" + fn;

            return Lookup(KeyboardSpecial, control);
        }

        private static string Lookup(Dictionary<string, string> table, string control) =>
            table.TryGetValue(control, out var name) ? name : null;
    }
}
#endif
