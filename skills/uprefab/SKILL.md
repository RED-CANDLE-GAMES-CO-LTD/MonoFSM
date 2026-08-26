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
| prefab 階層、子樹 component 欄位細節、FSM 架構 | `prefab read`（hard `--budget` / `--fsm-only` / `--structure-only`） | ✅ | [read.md](references/read.md) |
| scene 上的階層 | `scene ls`（hard `--budget`，`0` 才不限） | ✅ | [read.md](references/read.md) |
| 貼了 **scene 物件連結**（`globalId=GlobalObjectId_V1-…`） | `obj` | ✅ | [read.md](references/read.md) |
| **改** prefab / scene 結構、開/複製/存 scene、建 variant | `prefab do` / `scene do` / `scene copy` / `prefab variant` | ✅ | [edit.md](references/edit.md) |
| **路徑失效、名字跟上次讀到的不一樣**、節點名含 `/` 或換行 | —— | | [naming.md](references/naming.md) |
| **建 / 改 ScriptableObject asset**（registry / config 類） | `asset create` / `set` / `set-ref` / `add-element` | ✅ | [asset.md](references/asset.md) |
| **加 / 改互動文字提示**（localized、按狀態切換） | `prompt` | ✅ | [prompt.md](references/prompt.md) |
| **只要 localization 條目**（文案持有者是 SO 不是節點） | `loc` | ✅ | [prompt.md](references/prompt.md) |
| 某個節點被誰指到 / 它指向誰 | `refs` | ✅ | [probe.md](references/probe.md) |
| **組 FSM 時要挑 Action / Condition**（有哪些可用、各自幹嘛、欄位填什麼） | `catalog` | ❌ | [catalog.md](references/catalog.md) |
| 某個型別叫什麼、有哪些欄位 | `types` / `fields`（Component）、`asset fields`（SO） | ✅ | [probe.md](references/probe.md) |
| 場上有幾個某某物件、某個 component 現在的值 | `scene count` / `peek` | ✅ | [probe.md](references/probe.md) |
| **prefab 上某顆 component 的某幾個欄位**（「這條 ref 接上了沒」） | `prefab peek`（**不要用 `read`**，貴 50 倍） | ✅ | [probe.md](references/probe.md) |
| 已知 prefab 內找合併後的 component / 節點路徑 | `prefab locate --comp/--name` | ✅ | [probe.md](references/probe.md) |
| 同一 prefab 一次查多顆 component 欄位 | `prefab peek-batch -f probes.txt` | ✅ | [probe.md](references/probe.md) |
| 命中/override 有幾千筆，想先知道集中在哪 | `find --by-asset` / `overrides --by-target` | ❌ | [offline-index.md](references/offline-index.md) |
| **Play Mode 下改一個 Var 的值**（自動測試撥旗標 / 給錢） | `poke` | ✅ | [probe.md](references/probe.md) |
| **EffectReceiver 沒觸發**，要一次看完整條鏈卡在哪 | `effect-trace` | ✅ | [probe.md](references/probe.md) |
| 按 asset 上的 Odin `[Button]`（無參數方法） | `asset invoke` | ✅ | [asset.md](references/asset.md) |
| 想知道「調查為什麼慢」的實際數據 | `usage` | ❌ | [offline-index.md](references/offline-index.md) |

一句話版本：**使用者貼連結走 `guid`（asset）/ `obj`（scene 物件），定位走 `find`（預設 full；
要 shallow 才 `--scope all`，要接著下鑽就加 `--resolve`），讀 prefab 結構走 `prefab read`
（hard budget，再用 `--node` 下鑽），讀 scene 結構走 `scene ls`，查引用走 `refs`，要改走 `prefab do` / `scene do`，建/改 ScriptableObject 走 `asset`，
加 localized 文字提示走 `prompt`，挑 Action / Condition 走 `catalog`。**

## 鐵則

開檔案之前就要做的判斷，只有五條（DSL 語法、失敗語意、Auto 綁定那些細節在對應的
reference 裡，真的要改的時候一定會讀到）：

- **所有需要 Unity 的操作都有 CLI 入口 —— 不要直接寫 `uloop execute-dynamic-code`**，
  它每次回傳 15 行 JSON envelope（Logs / SecurityLevel / Diagnostics…），CLI 只回結果那一行。
- **離線索引還是唯一的跨資產定位手段** —— Unity 端沒有全專案搜尋（`refs` 只掃單一
  prefab / scene，`types` 只查型別名），所以「這個 component 在哪些檔案裡」只有 `find`
  答得出來，而且快兩個數量級（find 0.1s vs Unity 一次來回含 domain reload 十幾秒）。
  離線的就只有 `index` / `scope` / `find` / `guid` / `overrides` / `catalog` 這幾條。
- **離線索引只回答「在哪個檔案」，內容一律走 Unity 匯出。** 離線 YAML 讀不到 variant
  繼承來的東西（stripped 佔位 document 沒有名稱、component、真值），連 `find` 印的節點
  路徑都是局部的、不能直接餵給 `--node`（要完整路徑就 `--resolve`）。原因與實測數據見
  [internals.md](references/internals.md)。**`find` 也不會自己更新索引** —— 改過 prefab
  先 `up index`，`(no match)` 的第一個嫌疑就是索引過期。
- **`find` 預設只查 full tier。** shallow 是 override target 解析層，第三方 Example 命中常比
  gameplay 多兩個數量級；表尾若提示有 shallow 命中，真的需要時才加 `--scope all`。
- **read 的 `--budget` 是 hierarchy + FSM hard cap，`--depth` 不能繞過。** `--budget 0` 才是
  明確允許無上限；只看狀態機用 `--fsm-only`，只導航用 `--structure-only`。磁碟 cache 預設關閉，
  只有確定 prefab 已存且會反覆讀時才明確加 `--cache`。
- **挑 component 之前先 `up catalog`，不要 grep 或 Read .cs** —— 近 400 個 Action /
  Condition / Getter 的用途與欄位一次列完（離線、0.1s）。讀到 `⚠無說明` 而你為了工作
  實際去讀了那份原始碼，**順手補一段 `/// <summary>` 再走**，見 [catalog.md](references/catalog.md)。
- **節點名是框架自動命名的，路徑寫死一定會過期** —— 同批 ops 內用 `mark` + `$label`，
  跨批次 / 寫進計畫 md 時描述結構位置而不是抄名字。見 [naming.md](references/naming.md)。

## References

| 檔案 | 內容 |
|---|---|
| [offline-index.md](references/offline-index.md) | `index` / `find`（含 `--resolve`）/ `guid` / `overrides` / `scope`、`.uprefab.json` 設定、中文名稱 escape |
| [read.md](references/read.md) | `prefab read` / `scene ls` 參數與 `--budget` 分層下鑽、`obj`（GlobalObjectId 連結） |
| [edit.md](references/edit.md) | 批次 DSL 全部操作、`$` 代換、FSM 複合操作、`[n]` 後綴、失敗語意、`auto` 與 AutoChildren 陷阱、存檔 callback、variant / 模板 |
| [naming.md](references/naming.md) | 自動命名：為什麼路徑會過期、三道防線、`\/` 與 `\n` 逃逸 |
| [asset.md](references/asset.md) | ScriptableObject asset 的 create / set / set-ref / add-element / fields |
| [prompt.md](references/prompt.md) | localized 文字提示：case 格式、優先序、自帶驗證輸出 |
| [catalog.md](references/catalog.md) | `catalog`：Action / Condition 目錄、`--type` 細查、`--missing` 待補清單、`/// summary` 撰寫規範 |
| [probe.md](references/probe.md) | `types` / `fields` / `peek` / `refs` / `scene count` 與 Play Mode 驗證流程 |
| [example-fsm.md](references/example-fsm.md) | 完整實例：從零組「定時生資源」FSM 並在 Play Mode 驗證速率 |
| [internals.md](references/internals.md) | 設計取捨（為何不用離線讀內容 / 為何拆掉 cache）、已知限制、模組結構 —— **改 uprefab 本身前先讀** |

格式規則（node 行、component 區塊、值格式化、摺疊摘要）的真相來源是
`monofsm:hierarchy-text-exporter` skill，這裡不重複。
