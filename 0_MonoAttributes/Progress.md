# Progress

- **LayerMask 全域改用 Unity 原生 PropertyField**（`Editor/CustomDrawer/LayerMaskUnityDrawer.cs`）
  - 原因：Odin 內建 `LayerMaskDrawer` 不畫 prefab override 的粗體 label，只靠 GeneralDrawerConfig 裡那條「Show Blue Prefab
    Value Modified Bar」（可被關掉），實務上會看不出某個 mask 是 instance override。
  - 做法：`OdinValueDrawer<LayerMask>` + `DrawerPriority(0, 1000, 0)` 蓋掉內建 drawer，
    `Property.Tree.GetUnityPropertyForPath` 拿到 `SerializedProperty` 就交給 `EditorGUILayout.PropertyField`
    （粗體、右鍵 Revert/Apply 全都回來）。
  - 刻意不做：不用逐欄位加 `[DrawWithUnity]`（會漏，也髒）。拿不到 SerializedProperty 的情況（Odin 自行序列化、
    collection element）**不硬幹**，直接 `CallNextDrawer` fallback 回 Odin 原本的畫法。
  - `Initialize()` 只解析一次 path，不要每幀查字串。
