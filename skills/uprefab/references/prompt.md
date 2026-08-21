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

**在 variant 上加 case 會插到最前面** —— base 繼承來的 value source 排在後面，
新加的節點會變成第 0 個，於是無條件的新 case 蓋掉 base 有條件的那些。
加完用 `up prefab do "$P" "idx|<新節點路徑>|-1"` 壓到最後，再讀一次確認順序
（節點名含 `/`，路徑要寫 `\/`）。**不要再跑一次 `up prompt`**，它會把節點移回最前面。

**順序就是挑選優先序** —— value source 是「依 child 順序取第一個 `IsValid`」，所以
有條件的排前面、無條件的墊底。無條件的不是最後一條時會出 `[warn]`。

## `--check` —— 只驗不改

`ConditionRef`（proxy 到別處的條件）、`SmartStringTokenBinding`（token 指向另一顆 VarString）
這些 `--case` 語法蓋不到，只能用 `prefab do` 手工組。組完要驗收就用 `--check`：

```bash
up prompt "$P" --var "…/[Getter] d_Prompt_String 提示說明" --check
```

```
[值] locale = zh-TW
[值] [Getter] d_Prompt_String 提示說明 =            ← VarString.CurrentValue 在非 Play 時是空的，正常
[值] 1. 墊底 → 耐久度 40
[token] 1. ✓ {durability} ← durability = Durability → 40
```

`[值]` 每行一顆 value source（順序＝優先序），條件欄印的是節點名而不是結果 ——
`AbstractConditionBehaviour.FinalResult` 在非 Play 時一律回 false，印結果只會誤導。
`[token]` 是 inspector 那個「Token 檢查」的同一份報告，有 `✗` 就是模板 token 與 binding 對不上。

**多行組合（外層模板 + token 指向巢狀 VarString）只有分支自己的文字驗得起來** ——
token 讀的是巢狀 VarString 的 `CurrentValue`，非 Play 時解析不出來，所以外層會看到 token 位置是空的。
最終三行長怎樣要進 Play Mode 看。

## 節點名含換行要寫成 `\n`

`Localized: <譯文> (Table/key)` 這種自動命名會把含換行的譯文塞進 GameObject 名字，
於是路徑同時含 `/` 和換行。`\/` 之外再加一條：**換行寫成 `\n`**，`--var` 與 `prefab do` 的
`<node>` 都吃得下（`prefab read` 印出來的候選已經逃逸好了，照抄就對）。

順帶兩個踩過的：`Transform.Find` 對名稱含換行的節點**一律找不到**（要自己逐一比 `child.name`）；
`m_KeyId` 是 long，用 `SerializedProperty.intValue` 讀寫會溢位／截斷成負數。

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

## `up loc` —— 只要條目，不要節點

文案的持有者不是節點而是 ScriptableObject（例如 `GameEventTag` 只存 table + key）時，
`prompt` 那一整套節點操作用不上，只需要 string table 條目本身：

```bash
up loc ev_amulet_blocked "護身符擋下了落雷！"          # 預設 table=GameplayUI locale=zh-TW
up loc ev_hit_by_bolt "{player} 被落雷擊中" --table GameplayUI
up loc ev_amulet_blocked                              # 文案留空 = 只讀出既有的
up loc eff_normal $'\n能源效率 正常' --locale zh-TW --smart   # 沒有 token 但要當 Smart String 分支
```

key 不存在就建，含 `{` 自動開 IsSmart，一樣只 `SaveAssetIfDirty` 自己那兩個 asset。
文案沒有 `{` 但會被別的模板當 `{token}` 串進去時（多行組合的分支）要自己加 `--smart`。
回傳含 `id=<m_KeyId>`，那個 long 就是節點上 `TableEntryReference` 要填的值。

**換行直接用真實換行字元**（shell 用 `$'\n…'`）—— `\n` 兩個字面字元 SmartFormat 不會轉義。
`unity.lit` 對含換行的字串會改用跳脫字串而非 verbatim：verbatim 的換行會把
execute-dynamic-code 的程式碼縮排一起吃進文案（實測會多出 12 個空白）。
實作：`MonoFSM-Pro/Editor/LocEdit.cs`。

**SO 上不要存 `LocalizedString`** —— 它序列化的是 `m_KeyId`(long)，CLI / 腳本很難填對；
改存 `table` + `key` 兩個字串，runtime 再 `new LocalizedString(table, key)`
（`TableEntryReference` 吃得下 key 名稱）。`GameEventTag` 就是這樣做的。
