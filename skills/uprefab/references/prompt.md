# prompt —— 幫 VarString 掛一組有條件的 localized 文字提示（要 Unity 開著）

「加個互動文案」聽起來是一行的事，實際要跨四個系統：Localization 條目（key + IsSmart）、
`LocalizedStringValueSource` 節點、條件與 `InputPromptTokenBinding` 子節點、Auto 綁定與 Rename。
每一步都有自己的雷，所以包成一支：

```bash
up prompt "Assets/…/base 插座開關 Socket FSM.prefab" \
  --var "Modules/Player Selectable ModulePack Variant/[VarFolder] VariableFolder/[Getter] d_ Select Text Prompt 文字提示" \
  --case "broken|壞掉了請維修|if:Modules/Fixable ModulePack/[VarFolder] VariableFolder/[Getter] d_IsBroken=true" \
  --case "socket_no_power|沒有電力，無法充電|if:[VarFolder] VariableFolder/[Getter] d_HasPower 有電=false" \
  --case "socket_to_charge|{key} 充電 / 放置設備|prompt:key=RMB"
```

case 格式 `key|文案|spec;spec`：

| 欄位 | 說明 |
|---|---|
| `key` | string table 的 key。不存在就建 |
| `文案` | 留空 = 沿用 table 裡既有的。**含 `{` 會自動開 IsSmart**（沒開 `{token}` 不會展開，會原字輸出） |
| `if:<節點路徑>=true\|false` | 加一個 `VarBoolCompareCondition`。路徑相對於 prefab root |
| `prompt:<token>=<名稱或路徑>` | 加一個 `InputPromptTokenBinding`。token 可省（預設 `key`）；值吃 `InputPromptUIData` 的檔名或完整路徑 |

其他選項：`--locale`（預設 `zh-TW`）、`--table`（預設 `GameplayUI`）、
`--prune`（刪掉不在 case 清單裡的既有 value source）、`-f`（從檔案讀 case）。

**順序就是挑選優先序** —— value source 是「依 child 順序取第一個 `IsValid`」，所以
有條件的排前面、無條件的墊底。無條件的不是最後一條時會出 `[warn]`。

## 回傳自帶驗證，不用進 Play Mode

```
[loc] socket_no_power 新增：沒有電力，無法充電
[node] socket_no_power 新建  if [Getter] d_HasPower 有電==False
[值] locale = zh-TW
[值] 1. [Getter] d_IsBroken == True → 壞掉了請維修
[值] 3. 墊底 → <sprite="KeyboardMouse" name="mouse_right"> 充電 / 放置設備
```

`[值]` 是存檔後讀回來的真值，連 `{token}` 展開成 sprite tag 都看得到 ——
`LocalizedStringValueSource.RuntimeBindings` 在 Editor 非 Play 時會 fallback 直接抓子物件。
要進 Play Mode 的只剩「條件切換」是否如預期。

讀之前會把 `SelectedLocale` 切到 `--locale` 再還原：不切會拿到別的語言，
而且剛加的 key 因為 table 已載入會回 `No translation found`。

## 兩個踩過的坑（已在實作裡處理，改的時候別退回去）

- **不要 `AssetDatabase.SaveAssets()`** —— 它會把 Editor 記憶體裡所有 dirty 的 asset 一起落盤。
  實測連帶把使用者正在編輯、還沒存的兩個 prefab 寫進了磁碟。用 `SaveAssetIfDirty` 只存自己改的。
- **路徑先 probe 再寫 localization** —— localization 寫在 prefab 之前，
  路徑錯到那時才發現就會留下「條目建了但節點沒建」的半套狀態。

實作：`MonoFSM-Pro/Editor/PromptEdit.cs`（在 Pro 而不是 Core，因為
`LocalizedStringValueSource` / `InputPromptTokenBinding` 在 Pro，且要引用 `Unity.Localization.Editor`）。
