# Progress

- GameData 加 `_titleLocalized`（Unity Localization）與 `TitleLocalized` property，給 VarString 的 value source 取 localize 過的品項名稱；沒設就退回舊的 `titleStr`（會印 warning）。
- GameData Config 供值 Phase 1：`GameData.Config.cs` 加 `_baseConfig` 疊層（TryGetConfig/HasConfig 查不到問 base，深度上限 8）；新增 `GameDataConfigInjector`（IResetStart 依 VariableTag 把 config 注入 folder 下的 VarFloat，支援 `_skipTags`）；刪除 0 使用的 `GameDataConfigValueSource`。
- GameData Config Phase 2（editor 工具）：GameData 加 `CollectConfigTags`（含疊層去重）；injector 加 `[Button] 依 GameData config 補齊 VarFloat`；新增 `Editor/FlagData/GameDataConfigValidator`（Assets 右鍵檢查死欄位）與 `Editor/FlagData/PrefabVarSheetWindow`（Tools/MonoFSM/Prefab Var Sheet，家族 VarFloat 預設值表格編輯）。
- GameData Variant 一鍵生成：新增 `GameData.Variant.cs`（`CreateVariantAsset`／`CreateVariantAndSelect`，CopyAsset 整份複製後清空 `_configs`/`_objConfigs`/`_bindPrefab`、`_baseConfig` 指回 base、Manual 型重寫 SaveID）；三個入口＝GameData Inspector 按鈕、VarGameData「從 _defaultValue 建立 Variant」按鈕、Assets 右鍵 `Editor/FlagData/GameDataVariantMenu`。
