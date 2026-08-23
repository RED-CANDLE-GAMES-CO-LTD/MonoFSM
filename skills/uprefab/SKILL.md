---
name: uprefab
description: 能讀懂並改動 Unity serialized data（prefab / scene / ScriptableObject）。當需要：(1) 找某個 component / 節點在哪些 prefab 或 scene 裡 (2) 讀某個 prefab 的階層結構或 FSM 狀態機架構 (3) 看某個子樹的 component 欄位細節 (4) prefab override 稽核 (5) 用 API 改 prefab / scene 結構、建 prefab variant、複製場景模板、組 FSM、建立或編輯 ScriptableObject asset（registry / config 類資料） (6) 查某個型別有哪些 serialized 欄位、讀 Play Mode 下的 runtime 值、數場上物件驗證生成邏輯 (7) 查某個節點被誰引用 / 它指向誰 (8) 使用者貼了 asset guid 或 Editor webhook 連結（`?asset_guid=…`）需要換成資產路徑 (9) 理解或修改 uprefab 離線索引（MonoFSM/Tools~/uprefab/*.py）、PrefabTextReader 或 PrefabEdit / SceneEdit 時使用此 skill。
---

# uprefab

讀 / 改 Unity serialized data 的工具組。**先看決策表決定用哪一條路** —— 選錯會白跑一趟
或讀到不完整的資料。

範例裡的 `up` 是這個 shell function（**zsh 不會對 `$VAR` 斷詞，所以不要用
`UP="python3 …"`**，會被當成一個檔名而找不到）：

```bash
up() { python3 "MonoFSM/Tools~/uprefab/uprefab.py" "$@"; }
```

## 決策表

| 你要做什麼 | 用什麼 | Unity | 細節 |
|---|---|---|---|
| 這個 component / 名稱在哪些檔案裡 | `find` | ❌ | [offline-index.md](references/offline-index.md) |
| 找到之後要**能直接下鑽**（拿可餵給 `--node` 的完整路徑） | `find --resolve` | ✅ | [offline-index.md](references/offline-index.md) |
| 貼了 asset guid / webhook 連結（`?asset_guid=`）要換成路徑 | `guid` | ❌ | [offline-index.md](references/offline-index.md) |
| prefab override 稽核、索引範圍調整 | `overrides` / `scope stats` | ❌ | [offline-index.md](references/offline-index.md) |
| prefab 階層、子樹 component 欄位細節、FSM 架構 | `prefab read`（`--fsm` / `--budget`） | ✅ | [read.md](references/read.md) |
| scene 上的階層（**沒有 budget 保護，要自己給 `--depth`**） | `scene ls` | ✅ | [read.md](references/read.md) |
| 貼了 **scene 物件連結**（`globalId=GlobalObjectId_V1-…`） | `obj` | ✅ | [read.md](references/read.md) |
| **改** prefab / scene 結構、開/複製/存 scene、建 variant | `prefab do` / `scene do` / `scene copy` / `prefab variant` | ✅ | [edit.md](references/edit.md) |
| **建 / 改 ScriptableObject asset**（registry / config 類） | `asset create` / `set` / `set-ref` / `add-element` | ✅ | [asset.md](references/asset.md) |
| **加 / 改互動文字提示**（localized、按狀態切換） | `prompt` | ✅ | [prompt.md](references/prompt.md) |
| **只要 localization 條目**（文案持有者是 SO 不是節點） | `loc` | ✅ | [prompt.md](references/prompt.md) |
| 某個節點被誰指到 / 它指向誰 | `refs` | ✅ | [probe.md](references/probe.md) |
| **組 FSM 時要挑 Action / Condition**（有哪些可用、各自幹嘛、欄位填什麼） | `catalog` | ❌ | [catalog.md](references/catalog.md) |
| 某個型別叫什麼、有哪些欄位 | `types` / `fields`（Component）、`asset fields`（SO） | ✅ | [probe.md](references/probe.md) |
| 場上有幾個某某物件、某個 component 現在的值 | `scene count` / `peek` | ✅ | [probe.md](references/probe.md) |
| **prefab 上某顆 component 的某幾個欄位**（「這條 ref 接上了沒」） | `prefab peek`（**不要用 `read`**，貴 50 倍） | ✅ | [probe.md](references/probe.md) |
| 命中/override 有幾千筆，想先知道集中在哪 | `find --by-asset` / `overrides --by-target` | ❌ | [offline-index.md](references/offline-index.md) |
| **Play Mode 下改一個 Var 的值**（自動測試撥旗標 / 給錢） | `poke` | ✅ | [probe.md](references/probe.md) |
| **EffectReceiver 沒觸發**，要一次看完整條鏈卡在哪 | `effect-trace` | ✅ | [probe.md](references/probe.md) |
| 按 asset 上的 Odin `[Button]`（無參數方法） | `asset invoke` | ✅ | [asset.md](references/asset.md) |
| 想知道「調查為什麼慢」的實際數據 | `usage` | ❌ | [offline-index.md](references/offline-index.md) |

一句話版本：**使用者貼連結走 `guid`（asset）/ `obj`（scene 物件），定位走 `find`（要接著
下鑽就加 `--resolve`），讀 prefab 結構走 `prefab read`（預設就分層摺疊，再用 `--node`
下鑽），讀 scene 結構走 `scene ls`（要自己控 `--depth`），查引用走 `refs`，要改走 `prefab do` / `scene do`，建/改 ScriptableObject 走 `asset`，
加 localized 文字提示走 `prompt`，挑 Action / Condition 走 `catalog`。**

## 鐵則

- **所有需要 Unity 的操作都有 CLI 入口 —— 不要直接寫 `uloop execute-dynamic-code`**，
  它每次回傳 15 行 JSON envelope（Logs / SecurityLevel / Diagnostics…），CLI 只回結果那一行。
- **離線索引還是唯一的跨資產定位手段** —— Unity 端沒有全專案搜尋（`refs` 只掃單一
  prefab / scene，`types` 只查型別名），所以「這個 component 在哪些檔案裡」只有 `find`
  答得出來，而且快兩個數量級（find 0.1s vs Unity 一次來回含 domain reload 十幾秒）。
  離線的就只有 `index` / `scope` / `find` / `guid` / `overrides` 這幾條。
- **離線索引只回答「在哪個檔案」，內容一律走 Unity 匯出。** 離線 YAML 讀不到 variant
  繼承來的東西（stripped 佔位 document 沒有名稱、component、真值），連 `find` 印的節點
  路徑都是局部的、不能直接餵給 `--node`（要完整路徑就 `--resolve`）。原因與實測數據見
  [internals.md](references/internals.md)。
- **挑 component 之前先 `up catalog`，不要 grep 或 Read .cs** —— 108 個 Action、87 個
  Condition 的用途與欄位一次列完（離線、0.1s），比逐檔讀便宜兩個數量級。
  讀到 `⚠無說明` 而你為了工作實際去讀了那份原始碼，**順手補一段 `/// <summary>` 再走** ——
  這是目錄唯一的補齊來源，補完下一個人就不用再讀一次。
- **`find` 不會自己更新索引** —— 改過 prefab 就先 `up index`（增量，實測一天的變更量
  234 個資產 2.3 秒）。`(no match)` 或路徑對不上時，第一個嫌疑就是索引過期。
- **DSL 欄位用 `|` 分隔，不用空白** —— 節點名帶空白、`[Tag] ` 前綴與中文，空白分隔一定炸。
- **結構改完一定要下 `auto|`** —— MonoFSM 大量欄位靠 `[Auto*]` attribute 填，不補這步會
  存出「看起來對、欄位全是 null」的資料，只有進 Play Mode 才發現。
- **`add` condition 到別人的節點下之前，先確認那個節點上沒有別的 `[AutoChildren]` 使用者** ——
  子節點是整個 GameObject 共用的，多掛一個 condition 可能默默把同節點上其他 component 的行為
  也一起關掉（無錯誤訊息）。要只影響單一 component 就走 `VarBool` + `[DropDownRef]` 引用，
  見 [edit.md](references/edit.md)。
- **建新東西一律開 variant / 複製模板，不要從零建** —— prefab 帶著大量共用底盤
  （MonoEntity / MonoObj / NetworkObject / ModulePack），scene 需要 WorldUpdateSimulator /
  SpawnProcessor / PoolManager / AutoAttributeManager。
- **路徑或欄位打錯不會白跑** —— 錯誤訊息會列出走到哪一層、那層有什麼候選，照著修就好。
  同名節點（一排 `[Case] SwitchCase`）用 `[n]` 後綴指定第幾個。
- **改完直接 `prefab read` / `scene ls` 驗證** —— 讀到的一定是當下真值，沒有落檔 cache。

## References

| 檔案 | 內容 |
|---|---|
| [offline-index.md](references/offline-index.md) | `index` / `find`（含 `--resolve`）/ `guid` / `overrides` / `scope`、`.uprefab.json` 設定、中文名稱 escape |
| [read.md](references/read.md) | `prefab read` / `scene ls` 參數與 `--budget` 分層下鑽、`obj`（GlobalObjectId 連結） |
| [edit.md](references/edit.md) | 批次 DSL 全部操作、失敗語意、`[n]` 後綴、`auto`、variant / 模板 |
| [asset.md](references/asset.md) | ScriptableObject asset 的 create / set / set-ref / add-element / fields |
| [prompt.md](references/prompt.md) | localized 文字提示：case 格式、優先序、自帶驗證輸出 |
| [catalog.md](references/catalog.md) | `catalog`：Action / Condition 目錄、`--type` 細查、`--missing` 待補清單、`/// summary` 撰寫規範 |
| [probe.md](references/probe.md) | `types` / `fields` / `peek` / `refs` / `scene count` 與 Play Mode 驗證流程 |
| [example-fsm.md](references/example-fsm.md) | 完整實例：從零組「定時生資源」FSM 並在 Play Mode 驗證速率 |
| [internals.md](references/internals.md) | 設計取捨（為何不用離線讀內容 / 為何拆掉 cache）、已知限制、模組結構 —— **改 uprefab 本身前先讀** |

格式規則（node 行、component 區塊、值格式化、摺疊摘要）的真相來源是
`monofsm:hierarchy-text-exporter` skill，這裡不重複。
