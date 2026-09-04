# uprefab 開發進度

> 工具本身的使用說明在 [README.md](README.md)。這份是**開發進度與待辦**，用來接續工作。

## Resume

```bash
claude --resume e978684e-7697-4a61-b6b8-a018fe03c42e
```

- 最後更新：2026-07-27
- 分支：`develop`
- 狀態：Phase 1 完成並實測通過；**Phase 5（scene 寫入 + CLI 一行入口）已完成**，
  正在做「自己開 scene 組一個定時生資源 FSM 並驗證數量」的端到端實測

---

## Phase 5 —— scene 寫入、批次 DSL、CLI 一行入口（2026-07-27）

驅動這一輪的驗收條件：**自己開一個 scene、組一個定時生資源的 FSM、自己確認場上物件
數量對，而且整個過程要省 token。** 做不到的地方就是工具的缺口。

### 補了什麼

C#（`MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/`）：

| 檔案 | 內容 |
|---|---|
| `EditResolve.cs` | 從 `PrefabEdit` 抽出的共用解析：路徑 → 節點 → component → 欄位、型別解析、值套用、**錯誤訊息**。prefab 與 scene 只差在 root 怎麼來，錯誤訊息尤其不該有兩份 |
| `SceneEdit.cs` | scene 版原語：`NewScene` / `OpenScene` / `Save` / `AddNode` / `AddPrefab` / `AddComponent` / `SetField` / `SetRef` / `SetAssetRef` / `SetPos` / `Move` / `DeleteNode` / `Auto` / `Export` / `Count` / `Batch` |
| `EditBatch.cs` | 一行一操作的迷你 DSL（`|` 分隔），第一個失敗就停並回報停在哪 |
| `EditProbe.cs` | `Types`（找型別名）/ `Fields`（列 serialize 欄位）/ `Peek`（讀 runtime 值） |
| `AssetRef.cs` | asset path → 該塞進 ObjectReference 的物件（prefab 要按欄位型別取 component，硬塞 GameObject 會被靜默存成 null） |
| `PrefabEdit.CreateVariant` | 建 variant。**不要從零建 prefab** —— 專案的 prefab 帶著大量共用底盤 |
| `PrefabEdit.Batch` | 整批共用一次 `LoadPrefabContents` / `SaveAsPrefabAsset` |

Python（`MonoFSM/Tools~/uprefab/`）：

| 檔案 | 內容 |
|---|---|
| `unity.py` | uloop 橋接：只回 `Result`，Domain Reload / server starting 時自己等再重試 |
| `uprefab.py` | 新子指令 `scene` / `prefab` / `types` / `fields` / `peek` / `logs` / `clear` / `play` |

### 省 token 的三個真正來源

實測下來，省的不是「呼叫次數」而是這三件事：

1. **濾掉 JSON envelope。** `uloop execute-dynamic-code` 每次回 15 行 JSON
   （Logs / CompilationErrors / SecurityLevel / Diagnostics…），有用的只有 `Result`。
   `uprefab scene …` 一行進、一行出。`logs` 也是同一個道理。
2. **批次 DSL。** 建一個 FSM 是 30 幾個原語。逐次呼叫的雜訊會比內容多一個數量級；
   `scene do -f ops` 一次做完，回 30 行結果。
   分隔用 `|` 不用空白 —— MonoFSM 節點名帶空白與 `[Tag]` 前綴，空白分隔一定炸。
3. **`fields` / `types` / `peek` 取代讀 .cs。** 要知道欄位叫 `_timeMax` 還是 `_maxTime`，
   替代方案是把幾百行 .cs 讀進 context。而且回的是反射真值，不會被註解掉的舊欄位誤導。

### 實測過程中被錯誤訊息救回來的幾次

錯誤訊息是這套工具最有價值的部分 —— 每次失敗都要能直接推出下一步：

- `set|…|_timeMax._constValue|1` → 原本只列頂層欄位（沒用，因為 `_timeMax` 是對的）。
  改成「走到 `_timeMax`（VarFloatWrapper）為止，這層底下有：`_tempValue: float`, …」
  一次就修好。**巢狀路徑的錯誤要列走得通的那一層底下有什麼，不是列頂層。**
- `add` 重複 → 原本 abort 導致整批停。改成回「（跳過）已存在」。
  批次的實際用法是「修一行再整份重跑」，重複建立是預期狀況不是錯誤。
- `scene do "…"` 吃不到參數 → `path`（`new`/`open` 用的位置參數）先吃掉了。
- 編譯完緊接著呼叫必定撞 Domain Reload → `unity.py` 自己等。

### 結構改完一定要 `auto`

`EditResolve.RunAuto`（DSL 的 `auto|<node>`）。MonoFSM 大量欄位靠 Auto 系列 attribute 填
（`TransitionBehaviour._conditions` 是 `[AutoChildren]`、Action 的 `_parentObj` 是
`[AutoParent]`），平常是 Inspector 畫到時順手綁的。用 API 建節點不經過 Inspector，
不補這步會存出一份「看起來對、欄位全是 null」的資料。

### 順手修掉的兩個模組 bug（不是工具問題）

用 API 建出「乾淨的」新物件才會踩到 —— 既有 prefab 都已經被 Inspector 摸過所以看不出來：

1. `StateMachineLogic.cs:173` `if (_owners.Length == 0)` —— 剛 `AddComponent` 出來的
   `_owners` 是 null，這裡 NRE。同檔 67 / 75 / 84 行都有 null check，只有這行漏了。
2. `LocalSimulatorRunner.Awake` 沒 push `ShouldSimulte`。單機沒有
   `ISimulateAuthorityProvider`，`ShouldSimulte` 落到 `_shouldSimulateFlag`（預設 false），
   所以 **scene 上的物件在純 local 路徑下永遠不會被 Simulate**；spawn 出來的走
   `LocalSpawnManager` 有 push，scene 物件原本漏了。

### 組 FSM 場景的正確做法（使用者指定，2026-07-27）

第一輪實測走的是「開空 scene + 放 `(local) SinglePlayer World Simulator`」，那條 local
路徑模組有缺漏（見上面兩個 bug）。**正確做法是複製既有模板**：

- **scene**：`SceneEdit.CopyScene` 複製 `Assets/1_Prototype/Module Test/Network FSM Template.unity`
- **FSM 物件**：`PrefabEdit.CreateVariant` 從
  `Packages/com.monofsm.fusion/MonoFSM_Fusion/Network FSM.prefab` 開 variant
  —— 它已經是乾淨的 `init` / `idle` 兩狀態骨架，接著往上加狀態就好

`CopyScene` 為此補上（`NewScene` 保留，但組 gameplay 場景不要用它 —— 空 scene 缺的底盤
只會在 Play Mode 才炸）。`AssetDatabase.Refresh()` 也一起補進去：外部 `rm` 過檔案時
AssetDatabase 還握著舊狀態，「已存在」判斷會誤判。

### 端到端實測結果（通過）

`Assets/1_Prototype/uprefab Test/` 底下三個產物，全部由 CLI 建出來，沒手動開過 Inspector：

| 產物 | 來源 |
|---|---|
| `定時生資源 Test.unity` | `scene copy` ← Network FSM Template |
| `資源生成器 FSM.prefab` | `prefab variant` ← Network FSM.prefab，再 `prefab do` 加邏輯 |
| `測試資源 Rock Variant.prefab` | `prefab variant` ← `[Base] Carriable Mineral (Rock)` |

FSM 結構（`idle` 等計時器 → `spawn` 生一顆 → 回 `idle`）：

```
Timer <VarFloatCountDownTimer>            _timeMax = 1
[StateFolder] StateFolder
  [State] init   → [Transition] => idle
  [State] idle   [Event] OnStateEnter → [Action] Reset Timer (timer → Timer)
                 [Transition] => spawn   [If] Timer Up (_timer → Timer)
  [State] spawn  [Event] OnStateEnter → [Action] Spawn 資源
                 [Transition] => idle
```

Play Mode 分段量測（`scene count --name 測試資源`，每 4 秒取樣一次）：

| 時間 | count |
|---|---|
| t≈4s | 2 |
| t≈8s | 6 |
| t≈12s | 10 |

每 4 秒 +4，速率 **1.0 顆/秒**，與 `_timeMax = 1` 一致（首次取樣少 2 顆是
Fusion / scene 啟動的暖機時間）。

### `Count` 原本有個會誤判成「完全沒生成」的盲點

第一次量到 `count=0`，但 Console 顯示 pool 正在長（`Update max count … 3 → 7`）——
物件確實生出來了。原因：`Count` 只掃 active scene 的 root，而**借出中的 pool 物件掛在
`DontDestroyOnLoad`**。

已改成 `FindObjectsByType<Transform>` 掃全部已載入物件，並在輸出附 scene 分佈：

```
count=10 activeInHierarchy=10  [PlayMode]  filter: comp=* name=測試資源
  scenes: DontDestroyOnLoad=10
```

那行 `scenes:` 就是為了這件事加的 —— 數字對不上時，第一個要問的是「東西在哪個 scene」。

---

## 這個工具要解決什麼

專案規模變大後，大量 gameplay 資料放在 Unity serialize data（scene / prefab）上，
但 LLM 不擅長直接讀這些檔案

目標：**像讀程式碼一樣「跳著讀」Unity 資料** —— 先精準定位，再只讀需要的那一小塊。

### 現況盤點（開工前的判斷）

專案裡**已經有很好的 renderer**：`HierarchyTextExporter`
（`MonoFSM/1_MonoFSM_Core/Editor/PrefabExporter/HierarchyText/`），
有折疊摘要、預設值過濾、`res:` / `@相對路徑` reference 格式、`_expandPaths`。
**格式這層不用重做。**

缺的是三件事：

1. **定址（anchor）** —— 沒有 `file:line` 的對應物
2. **索引** —— 找東西只能整棵 export 再自己看
3. **部分讀取入口** —— `Export()` 一定從 root 開始，`_expandPaths` 是
   「在整棵裡挖洞」，不是「從中間讀起」

### 架構決策：兩層

| 層 | 做什麼 | 需要 Unity？ |
|---|---|---|
| **Layer 1** 離線索引 | 掃 YAML 建 SQLite，負責 find / refs / overrides | 否 |
| **Layer 2** Unity 精讀 | 沿用 `HierarchyTextExporter`，負責 read / fsm | 是（走 uloop） |

離線 YAML 拿不到「合併後的真值」（variant 鏈、型別預設值），所以精讀必須回 Unity。
但**定位**與**override 稽核**離線做反而更快 —— scene 檔裡的 `PrefabInstance`
document 帶完整 `m_Modifications`，那就是 override list 本身。

### 四個原語（對應熟悉的程式碼工具）

| 讀程式碼 | uprefab | 資料來源 | 狀態 |
|---|---|---|---|
| Glob | `find --comp X --name Y` | 索引 | ✅ Phase 1（2026-07-28 修效能，見下） |
| Grep | `grep`（欄位值搜尋） | 索引 | ⬜ 未做 |
| Read(offset,limit) | `prefab read --node <path> --budget N` | Unity | ✅ 已做 |
| （無對應） | `refs --node <path>` 反查引用 | **Unity** | ✅ 已做（單一資產內） |

---

## 已完成：Phase 1

`MonoFSM/Tools~/uprefab/`，約 1200 行 Python，零外部依賴（SQLite / re 都是標準庫）。

```
uyaml.py    Unity YAML 的 streaming document scanner
scripts.py  .cs.meta → guid/class/namespace 對照表
config.py   .uprefab.json 讀取與路徑比對
indexer.py  SQLite schema 與索引建置
query.py    find / overrides / scope stats
uprefab.py  CLI 進入點
```

### 實測數字（5323 個資產）

| 項目 | 結果 |
|---|---|
| 全量索引 | 26 秒 |
| 增量索引 | 3 秒 |
| `find` 查詢 | 0.12 秒（**修過查詢計劃之後**，見下） |
| DB | 207MB（已 gitignore） |

#### `find` 的查詢計劃修正（2026-07-28）

`--comp` 查詢一度退化到 **4 分 50 秒**（nodes 長到 12.7 萬列之後）。原本的寫法是

```sql
FROM nodes n WHERE EXISTS (SELECT 1 FROM comps c WHERE … AND c.type LIKE ?)
```

`EXPLAIN QUERY PLAN` 顯示 `SCAN n` —— 全掃 nodes、每列跑一次 EXISTS，
**`ix_comps_type` 完全沒被用上**，外加一個 correlated scalar subquery 對每列組
`group_concat`，最後才 `USE TEMP B-TREE FOR ORDER BY` 排序全部命中列再 LIMIT。

改成兩階段（`query.find`）：以 `comps` 當驅動表讓 `ix_comps_type` 生效、join `nodes` 走
PRIMARY KEY，`group_concat` 只對 LIMIT 之後的那幾列補。**4:49.91 → 0.118 秒**，
結果一字不差。純 SQL 重寫，沒動 schema、不必 rebuild。

### 大檔怎麼變得可索引

三道濾網，缺一不可：

1. **script guid 對照表** —— 掃 `.cs.meta` 建 `guid → class name`，
   不開 Unity 就能從 `m_Script` 判斷型別
2. **`scriptOnly`** —— 只索引「自己或後代掛有自家 script」的節點。
   Terrain / 植被 / 靜態裝飾（大部分體積）連進索引都不用
3. **`sceneRootFilter`** —— 特定 scene 指定整棵跳過的 root

補充：`m_EditorClassIdentifier` 直接帶完整 class name，但只有 **68%** 的
MonoBehaviour 有填，所以 guid 對照表這條 fallback 路徑是必要的。

### 開發過程修掉的 5 個真 bug

都是實測發現，不是預先設想的：

1. **`m_Modifications` 抓不到** —— 它在 `m_Modification:` 底下（縮排 4）不是頂層，
   而且 YAML 序列項（`- target:`）與 key 同縮排，原本的 `block()` 會提早 break
2. **stripped MonoBehaviour 灌水** —— comps 從 229k 降到 19k。stripped 是
   prefab instance 佔位，沒有實際欄位資料
3. **refs 爆到 3M** —— MonoFSM 的路徑解析結構（`fieldName` / `TargetMb` / `value`）
   在 scene 裡序列化出上百萬筆重複邊。去重 + 每 document 上限 64 後剩 20k
4. **Transform 從沒進 comps** —— override 最常改的就是 `m_LocalPosition`，
   不進 comps 就解不出 target
5. **中文名稱搜尋整個失效** —— Unity 把非 ASCII 的 `m_Name` 逃逸成 `\uXXXX`
   （蒸汽壓力 → `蒸汽壓力`）。修好前搜 `火車` 只有 1 筆，修好後 11 筆

### 兩個設計取捨（已定案）

- **`exclude` 是降級成 shallow，不是完全丟棄**（`config.py` `Config.tier`）。
  第三方 Demo 資料夾不是自己的 gameplay 內容，但別人的 prefab 引用進去時
  還是要查得到節點名，否則 override 稽核會顯示 `(source 未索引)`
- **shallow 層不存自己的 override**（`indexer.py`，PrefabInstance 分支）。
  shallow 只是「被查詢的對象」。加這條後 DB 從 456MB 降到 206MB

### override target 解析率

| 結果 | 佔比 |
|---|---|
| 解析到具體物件 | 66% |
| 只知道來源資產 | 30% |
| 完全未解析 | 2% |

未解析的主因：多層 variant 的**合成 fileID 在任何單一檔案裡都不存在**，
要完整還原得實作 Unity 的 prefab 實例化演算法。

---

## PrefabLens 評估（已完成，結論：不採用）

<https://github.com/hashiiiii/PrefabLens>（Apache 2.0）

**不採用的理由**：它是 **diff** 工具不是 **query** 工具。公開 API 只有
`diffBytes` / `diffToJson`（`core/src/root.zig`），連 `tree.build()` 都吃
`DiffResult` 而非單檔。沒有專案掃描、沒有 reference 索引。加上 Zig 技術棧
（團隊沒有）、沒有 script guid → class name。

**但值得另外裝來用** —— CLI + Editor window 能把 `.prefab` / `.mat` 的
git diff 變成可讀的語意 diff。跟 uprefab 不衝突。

**可以參考的部分**（真的要做完整 variant 解析時）：
- `core/src/instantiate.zig`（579 行）—— 離線展開 prefab variant chain
- `core/src/diff_overrides.zig`（531 行）—— `m_Modifications` 解析

---

## 實測發現：階層在 variant 邊界斷開（2026-07-27）

拿 PPlayer 做「讀狀態機架構」實測時發現的 **第 6 個真 bug**，比 override
解析率更影響可用性。

`PPlayer.prefab` 694 個節點裡有 **263 個 parent=0**。原因不是索引漏掉，
而是它們的 `m_Father` 指向 **stripped Transform** —— stripped document
沒有 `m_GameObject` 欄位，所以本檔內算不出 parent GameObject。

已加 `pending_parent` 表與 `_resolve_stripped_parents()` post-pass：
用 `m_CorrespondingSourceObject` 往來源 prefab 問「這個 Transform 屬於
哪個 GameObject」，拿到之後回頭找本檔 src 指向它的 stripped 節點。

**但只解出 153/2414（PPlayer 4/235）。** 卡在一個結構性事實：

> Unity 只在「本檔有東西引用到」時才寫出 stripped 佔位 document。
> 中間層常常只有 stripped **Transform**（因為有子物件的 `m_Father` 指它），
> 沒有對應的 stripped **GameObject**，鏈就在那層斷掉。

PPlayer 的鏈有四層：
`PPlayer` → `Base Character` → `CharacterModules` → `General FSM`。
最上層 `General FSM` 的 comps 表答得出 Transform→GameObject，
但回程在 `CharacterModules` 找不到對應的 stripped GO 節點。

### 建議的解法（未做）

不要繼續追 parent fileID —— 改成給孤兒節點一個 **來源路徑前綴**：

```
@General FSM.prefab:[StateFolder] StateFolder/[Var] AirDashCount
```

`_go_of_transform()` 失敗前的「最後一次命中」已經知道是哪個資產的哪個
GameObject，把它的 `path` 當前綴寫進 `nodes.path` 即可。parent_file_id
仍是 0，但 path 變得可讀、可分群 —— 對「讀架構」這個用途就夠了。
要放在 `_resolve_cross_asset_names()` **之後**，否則會被 path 重算蓋掉。

### 順帶：`fsm` 指令的原型已經跑過了

即使階層有斷，靠 `[State] X` / `[Transition] => Y` 的命名慣例 + 一層
parent 關係，已經能匯出可讀的狀態機清單（state、型別、transition、條件）。
PPlayer 那條鏈實測結果：

| 檔案 | states |
|---|---|
| `PPlayer.prefab` | 21（Gravity Gun 相關 + Install/Plugged/Reload） |
| `CharacterModules.prefab` | 43（idle/Walk/Jump/Dash/Bow/Climb 主體） |
| `Gravity Gun.prefab` | 5（idle/TryGrab/Grabbing/TryRelease/Flying） |
| `Base Character.prefab` | 4 |

這份查詢邏輯值得直接收進 `query.py` 當 `fsm <asset>` 指令，
當成離線的粗查（Unity 精讀走 `prefab read --fsm`）。

---

## 待辦

### 兩個等使用者決定的事

1. **override target 66% 夠不夠？** 要拉高就得實作 Unity 的 prefab
   實例化演算法（參考 `instantiate.zig`）。我判斷 66% 對稽核夠用，但這是我的判斷。
2. **要不要砍掉兩個第三方大檔？** `scope stats` 指出：
   ```
   52178 nodes  161M  Assets/Example/03_Interactions/06_Environments/Environments _flat.unity
   13456 nodes   26M  Assets/Example/00_Showcase/Showcase.unity
   ```
   從 `includeShallow` 拿掉可省約 1/3 的 DB。看會不會查到它們。

### ~~Phase 2~~ —— `refs` 反查：**單一資產內已做，走 Unity**

`EditRefs.cs`（`PrefabRefs` / `SceneRefs`）+ CLI `up refs`。已實測。

**原本的計畫是走離線 `refs` 表，實測後放棄了 —— 別再往那條路投資：**

| 問題 | 實測數據 |
|---|---|
| `refs` 表只收本檔直接寫出的引用邊 | 對 override 型引用 **0 命中**（`refs` 34,556 筆全無） |
| override 的目標在 `mods` 表但沒獨立欄位 | 被格式化成 `→{fileID: …}` 塞進 `value`，32 萬筆要 LIKE 全表掃，且**不完整**（`_targetVar` 那筆查不到） |
| 就算查到也只有裸 fileID | 翻成可讀路徑會撞上 variant 階層斷裂（那條已明確標「不要投資」） |

同一個目標（`[Var] Durability`）的實測對比：離線 grep + SQLite 探測數輪只湊出 4 筆，
`up refs` 一次回 14 筆且帶可讀節點路徑 + `型別.欄位`。**省 token 的是 Unity 那條。**

`SerializedObject.NextVisible(true)` 會走進巢狀 serialized 欄位，所以
`AbstractVarWrapper` / `ValueProvider` 的間接引用**天生涵蓋**（實測抓到
`VarFloatEffectApplyAction._targetValue._var`、`HittableSchema._durability._var`），
不需要重做 `ComponentReferenceScanner` 的那套反射特例。

**還沒做的是跨資產全庫粗查**（「哪些 prefab 引用到這個 SO / 這顆 prefab」）。
那個目標是 asset 而不是節點，離線索引的 `refs.to_guid` 就夠用，不會碰到上面的 override 問題。
分工：全庫粗查（離線，找「在哪個資產」）→ `up refs`（Unity，該資產內精查）。

### ~~Phase 3~~ —— `read` / `fsm`（跳著讀）：**已完成**

`PrefabTextReader.cs` + `up prefab read`：

- **`charBudget`（`--budget`，預設 20000）** —— 由淺往深試，取「塞得進預算的最深一層」，
  檔頭寫下摺在第幾層、下一層要多少字元。實測 PPlayer 全展開 122KB → 17KB。
  由淺往深而不是先全展開再退：全展開一份 PPlayer 是 120KB 字串，淺層那幾次都很便宜。
- **`--fsm`** —— 轉呼叫既有的 `FsmTextExporter`（markdown 輸出）。
- 從中間節點展開走 `--node <路徑>`，不需要 `ExportAt(fileID)` —— 路徑語彙已經夠用，
  而且跟 `prefab do` / `refs` 共用同一套（打錯會列出該層子節點）。

**同時把落檔 cache 整套拆了**（marker / writer / config / `Tools/uprefab/cache/`）。
理由：實測 5 份 cache 有 2 份比來源舊（差 80～135 秒），照過期 cache 做的分析會給出
「看起來合理但已經不成立」的結論；而且要靠人記得掛 marker、記得掃新舊。
`--budget` 拿到同樣的省 context 效果，讀到的一定是當下真值。

### Phase 4 —— 寫入：**已完成**，但不在 CLI 裡

原本規劃是 CLI `set --dry-run`，結論是**寫入不該進 CLI**：離線 YAML 寫入沒辦法保證 prefab
override 語意與序列化正確性（這也是「不要碰 YAML」原本的理由）。改成走 Unity 側的
`MonoFSM.Editor.PrefabEditing.PrefabEdit`，由 `uloop execute-dynamic-code` 一行呼叫。

四個原語 `AddNode` / `SetField` / `SetRef` / `DeleteNode`，吃**節點路徑**（跟
`ExportSubtree` 同一套語彙），不需要先查 anchor 或 fileID —— 原本設想的
「anchor → 人工編輯指示」這一步因此整個省掉了。

實作要點：`LoadPrefabContents` + `SerializedObject` + `SaveAsPrefabAsset`，不碰 YAML；
路徑 / 型別 / 欄位解析失敗就 abort 不存檔；錯誤訊息帶下一步線索（列出該層子節點、
候選 FullName、可用欄位名）。

用法見 `MonoFSM/skills/uprefab/SKILL.md` 的「三、寫入 —— 批次 DSL」。

---

## 檔案清單

| 路徑 | 狀態 |
|---|---|
| `MonoFSM/Tools~/uprefab/*.py`（6 檔） | 新增 |
| `MonoFSM/Tools~/uprefab/README.md` | 新增 |
| `MonoFSM/Tools~/uprefab/PROGRESS.md` | 本檔 |
| `.uprefab.json` | 新增（repo root，設定檔） |
| `.gitignore` | 修改（加 `/.uprefab.db`） |
| `MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/PrefabEdit.cs` | 新增（寫入原語 + batch dispatch） |
| `MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/PrefabTextReader.cs` | 新增（匯出 + charBudget 分層 + `--fsm`） |
| `MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/EditRefs.cs` | 新增（引用反查） |
| `Assets/0_Gameplay/Editor/PrefabTextReaderConfig.cs` | 新增（專案端視覺 component 注入） |
| ~~`PrefabTextCacheWriter.cs` / `PrefabTextCacheMarker.cs` / `PrefabTextCacheConfig.cs`~~ | **已刪**（落檔 cache 機制拆除，理由見 Phase 3） |

尚未 commit。MonoFSM 是 submodule，那幾個檔要在 submodule 裡另外 commit。

## 2026-07-29

- 新增 `up asset` 子命令，對應 `AssetEdit.cs`：讓 CLI 也能建立/編輯 ScriptableObject asset。

## 2026-07-30 —— `up prompt`

新增 `up prompt` 子命令，對應 `MonoFSM-Pro/Editor/PromptEdit.cs`：一行完成「幫某個 VarString
掛一組有條件的 localized 文字提示」。

**驅動它的實例**：幫插座 prefab 加「充電 / 沒電 / 壞掉」三種提示。手工做要跨四個系統
（Localization 條目、`LocalizedStringValueSource` 節點、條件與 token 子節點、Auto 綁定與
Rename），每次都得臨時寫 `execute-dynamic-code`，而且每次重踩同一批雷：

- `m_KeyId` 是 long，`prefab do` 的 `set` 只吃 int32 → 改用官方的 `new LocalizedString(guid, entryId)`
- value source 的節點名含 `/`（`=> Localized: GameplayUI/broken`），`Transform.Find` 會當成路徑分隔
  → 改用 keyId 比對既有節點，不靠名字
- 文案含 `{token}` 但沒開 `IsSmart` → SmartFormat 不展開，原字輸出。現在含 `{` 自動開
- dynamic code 裡 `Object` 一定和 `UnityEngine.Object` 歧義

**放在 Pro 而不是 Core**：`LocalizedStringValueSource` / `InputPromptTokenBinding` 在
`MonoFSMPro`，而且要引用 `Unity.Localization.Editor`（已加進 `MonoFSMPro.Editor.asmdef`）。
`EditResolve` 是 `internal`，跨 assembly 用不到，所以路徑解析與錯誤訊息在 `PromptEdit` 裡另寫了一份精簡版。

### 兩個實作上的坑（別退回去）

- **不要 `AssetDatabase.SaveAssets()`** —— 它會把 Editor 記憶體裡所有 dirty 的 asset 一起落盤。
  第一版用了它，實測連帶把使用者當時正在編輯、還沒存的兩個 prefab 寫進磁碟。改成 `SaveAssetIfDirty`。
- **先 probe 路徑再寫 localization** —— localization 在 prefab 之前跑，路徑錯到那時才發現
  會留下「條目建了但節點沒建」的半套狀態。現在先對唯讀的 prefab asset 把 `--var` 與所有
  `if:` 路徑走一遍。

### 回傳自帶驗證，不用進 Play Mode

存檔後把每條 value source 的 `Value` 讀回來印出，連 `{token}` 展開成 sprite tag 都看得到 ——
`LocalizedStringValueSource.RuntimeBindings` 在 Editor 非 Play 時會 fallback
`GetComponentsInChildren<ISmartStringTokenBinding>`。讀之前把 `SelectedLocale` 切到 `--locale`
再還原（不切會拿到別的語言，且剛加的 key 因為 table 已載入會回 `No translation found`）。
要進 Play Mode 的只剩「條件切換」是否如預期。

## 2026-07-30 —— `up obj`（GlobalObjectId 連結）

新增 `up obj`（別名 `up gid`），對應 `MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/EditGid.cs`：
吃使用者從 Editor 貼來的 scene 物件連結，回傳節點路徑或整棵子樹。

**為什麼要它**：專案裡「指某個 scene 節點」的通用交換格式是 `BugReportUtility` 產的
`[名稱](http://localhost:8888/webhook?globalId=GlobalObjectId_V1-2-<sceneGuid>-<objId>-<prefabId>)`。
人貼給 Unity 就能跳過去，但那串 id **不含節點路徑**，所以拿到連結的一方原本什麼都做不了：
拿去 `up guid` 只會得到「所在的那個 scene」（32 位 hex 是 scene 的 guid，不是節點）。

- `--locate` 只回 `owner / 節點路徑 / component 清單 / (+N nodes)` —— 那行路徑可以直接
  接給 `up scene ls --node`、`up refs --node`。
- 不帶 `--locate` 就走 `PrefabTextReader` 同一個 renderer（`--node` / `--depth` /
  `--budget` / `--fold` / `--fsm` 同一套參數）。為此把分層邏輯抽成
  `PrefabTextReader.ExportNode(GameObject, …)`，`Export(assetPath, …)` 改成呼叫它。
- 解析用 regex 撈 `GlobalObjectId_V1-…`，所以 markdown 連結 / 裸 URL / 只有 id 都吃，
  整段貼進去就行；`-` 讀 stdin。

**解不開時把「為什麼」講完**，這是實測後補的重點。GlobalObjectId 只在物件所在 scene
開著時解得開（Unity 限制），所以失敗時它把 guid 翻成 scene 路徑、印 `identifierType`
的意思，並區分三種情況：scene 沒開（給 `up scene open` 指令，或加 `--open`）／scene
開著但物件不見（連結過期）／guid 對不到資產。`--open` 在有未存檔 scene 時一律拒絕 ——
換 scene 會丟掉編輯，不猜使用者想不想留。

**實測**（`0_下山逃脫_July_lake`）：使用者提供的那條 `[Render] VerletRope` 連結
`targetPrefabId=0`，現場對同名節點重新產出來是 `…-1351641103` —— 該物件後來被打包進
`safe light bulb 燈泡.prefab`，舊連結的 id 已對不上。用現產的 id round-trip 則路徑、
component 清單、`--budget` 分層、`--node` 下鑽（含路徑打錯時列出該層子節點）都正確。

## SerializeReference / Play Mode 寫入 / Odin Button / transform / 存檔 callback

做「訂購終端機」機台時一路補的五個缺口 —— 判準都是「不補就得繞去
`uloop execute-dynamic-code`」：

- **`asset add-element --type <T>`**：`[SerializeReference]` 陣列（`GameData._dataFunctions`）
  單純 `arraySize++` 只會得到 `rid: -2` 的 null 元素。加了 `managedReferenceValue` 設定，
  型別池是欄位宣告型別的非抽象衍生型別（`EditResolve.ManagedRefType` /
  `ManagedRefFieldType`，後者拆 `managedReferenceFieldTypename` 的 `"組件名 FullName"` 格式）。
  不給 `--type` 時會在 log 明講「這是 null，要加 typeName」，免得以為加成功了。
- **`poke <node> <comp> <value>`**：Play Mode 下走 `AbstractMonoVariable.SetValue`
  設 runtime 值，peek 的寫入面。沒有它就沒辦法自動驗「按鍵 → FSM → 扣錢」這條鏈
  （要嘛去驅動玩家角色互動，要嘛不驗）。
- **`asset invoke <path> <method>`**：反射按 Odin `[Button]`。起因是新建的 GameData
  沒被收進 `AllFlagCollection`，Play Mode 下 `FlagAwake` 不跑、`_dataFunctionDict` 空的、
  `Price` 靜默回 0 —— 而修法是按一顆 `FindAllFlagsInProject` button，agent 按不到。
- **prefab batch 的 `pos` / `scale` / `rot`**：本來只有 scene 有 `pos`。三顆 nested prefab
  按鍵不能擺位置，等於機台組不起來。三個分量少一個就停，不猜 0（`EditBatch.Vec3`）。
- **`aref` 的 `builtin:` 前綴**：`builtin:Cube` / `Quad` / … 走
  `Resources.GetBuiltinResource`。內建 primitive 住在 `Library/unity default resources`，
  `AssetDatabase.LoadMainAssetAtPath` 讀不到，組 placeholder 幾何一定會撞到。
- **存檔前跑 `IBeforePrefabSaveCallbackReceiver`**：這個最重要。Unity 只在 PrefabStage
  觸發它，`LoadPrefabContents` + `SaveAsPrefabAsset` 不會。而
  `NetworkAutoSuggestVarSyncComp` 靠它把 `NetworkedVarTag` 配成實際的 sync 元件 ——
  不跑的話用 API 加的 networked var **靜默沒有同步**，單機測完全正常。
  專案幾乎每個 MonoBehaviour 都實作這介面（一顆機台 920 個），所以只報數量、失敗才點名。

順帶修正文件兩處舊敘述：`prefab|` 放 nested prefab 實例其實 prefab batch 一直支援
（`PrefabEdit.cs` 的 `case "prefab"`），文件卻寫「只有 scene」。

## `effect-trace` —— EffectHit 鏈路診斷

`EffectTrace.Trace(nodePath, effectTypeFilter)`（`Editor/PrefabEditing/EffectTrace.cs`）。
起因是查一顆 `Zone Arrive` receiver 為什麼沒觸發花了十幾次 peek：這條鏈有六段
（detector 偵測 → detectable dict → dealer 有效 → 配對 → enterNode 四道 gate → action），
每段都靜默 return，只能逐段 peek 二分。現在一次攤開，並在有問題的那段標 `←`。

沒有 dealer 打進來時，會反查場上同 effectType 的 dealer（附距離、它們掛在哪顆 detector 下）。
全程反射 + 型別名比對，不對 runtime assembly 產生編譯期依賴。

兩個踩到的實作細節：`_enterNode` / `_parentObj` 是 `[AutoChildren]` / `[Auto]` 填的，EditMode 下
是 null，會誤報成「沒有 enterNode」→ 退回用階層找，且結論箭頭只在 Play Mode 印；
另外拿來比對的值要用原始 object（`Prop`）而不是印給人看的字串（`Call`）——
`string` 也是 `IEnumerable`，錯用會讓 `registered` 永遠是 NO。

---

## `prefab read` 加一層磁碟快取（readcache.py）

`up prefab read` 是唯一「純讀、輸出到 budget 上限、同一份東西會被反覆問」的指令
（variant 的 base 在一次調查裡常被讀好幾次），所以只有它值得快取。
key = 指令參數 + 依賴集合的 (相對路徑, mtime_ns, size)：從目標 .prefab 出發離線掃
YAML 的 `guid:`，用既有的 `query.asset_by_guid` 翻成路徑、只留 .prefab，遞迴三層 ——
這樣 variant base（`m_SourcePrefab`）與 nested prefab 會一起被納入，改了 base 就自動失效。

正確性優先於命中率：guid 解不開、檔案讀不到、任何例外一律當 miss 直接走 Unity。
命中時在輸出最前面印一行提醒（Inspector 改了沒存檔的話請加 `--no-cache`）；
`--no-cache` 跳過讀取但仍寫入。快取放 `.uprefab-cache/read/`（已進 .gitignore），
超過 200 檔依 mtime 刪到剩 150。usage 記錄多一個 `cache` 欄位（hit / miss / bypass / off）。

---

## 三項省 token 的改動：欄位級讀取、命中聚合、批次 DSL 路徑代換

依 `up usage`（1125 次呼叫、3.27M 字元輸出）挑的三個最大來源：
`prefab read` 佔 62%、`find` 佔 20%、`overrides` 佔 10%，而寫入的 `prefab do` 只佔 2.2%
—— 所以寫入的 DSL 本身沒有換掉的理由（直接寫 execute-dynamic-code 還要多付 envelope，
直接改 YAML 讀不到 variant 繼承），要省的是讀取量與重複的路徑字串。

1. **`up prefab peek`**（`EditProbe.PeekAsset`）—— 讀 prefab asset 上一顆 component 的
   幾個欄位。原本「那條 ref 接上了沒」的最小單位是 `prefab read` 的整顆子樹（平均
   6.4KB），現在是 ~150 字元。`--members` 留空時列 serialize 欄位而不是 public 屬性
   （asset 上沒跑過 runtime 邏輯，屬性大半空的或會炸），所以 `Peek` 的傾印邏輯抽成
   `Dump(comp, header, members, serializedByDefault)` 共用。

2. **命中聚合** —— `find --by-asset`、`overrides --by-target` 只回「集中在哪」的分佈；
   並且被 `-n` 切掉時表尾一定講出「50 / 共 2809」。原本只印「50 match(es)」，會被讀成
   「總共就這些」，後續「這個 component 只有這幾處用到」的結論整個是錯的。
   `find` / `find_count` / `find_by_asset` / `find_totals` 共用 `_find_where()`，避免
   「列出的那幾筆」跟「總共幾筆」用到不同條件。

3. **批次 DSL 的 `$` 代換與 FSM 複合操作** —— `$` = 上一個建立節點的操作碰到的節點、
   `mark|<label>[|<node>]` + `$label` = 命名代換（`EditBatch`，prefab / scene 共用）；
   `state` / `trans` / `if` / `act` 四個複合操作（`EditFsm`，兩邊 Dispatch 的 default
   接進去）。實測同一份 FSM 從 1.5KB 降到 0.8KB，差的全是重複的長路徑。
   代換只認 `^\$([A-Za-z_]\w*)?(/.*)?$`，所以 prompt 的 `${token}` 不受影響（`$$` 是跳脫）。
   只做「一定會這樣做」的部分：`[State]` / `[Transition] =>` / `[If]` / `[Action]` /
   `[Event]` 命名慣例、phase → handler 型別對照、transition 的 `_target`。
   已知限制：`[Action]` / `[If]` 節點存檔後會被 `AbstractDescriptionBehaviour` 的自動命名
   蓋掉，整份重跑前要先 `read` 看實際名稱，否則會建出重複節點（原有 `add` 也一樣）。

## catalog：組 FSM 時挑 component 的離線目錄

`up catalog [action|condition|render|handler|getter|var|so|all] [keyword]`。
資料在 `catalog.py`（純字串比對抽 .cs）→ `.uprefab.db` 的 `catalog` 表，跟著
`up index` 全庫重建（約 2 秒）。每列給「class ─ 用途第一句」＋壓縮欄位行，
`--type` 看單一型別完整說明與 tooltip，`--missing` 是缺 `/// summary` 的待補清單。
`up fields` 也會在 Unity 欄位真值前補上這裡的說明。

抽取上踩到的三個雷（都已修，改的時候別改回去）：
1. class 宣告的 regex 會把上方 `[Attr]` 行一起吃進 match，所以 match 起始行不一定是
   `public class` 那行 —— 要先 `_skip_attrs_up` 才判斷得出 summary 與 `[Obsolete]`。
2. 泛型參數與 base list 之間常常換行（`class Foo<T>\n    : Bar`），base 的 `:` 前要允許空白，
   不然整條繼承鏈斷掉，底下幾十個 class 全部歸不了類。
3. kind 靠繼承鏈遞移，但鏈上的中繼 class 不一定自己一個檔案（`AbstractGetter` 寫在
   `AbstractValueSource.cs` 裡），所以 bases 對照表要收「檔案裡每一個 class 宣告」，
   catalog 條目本身才只認檔名 stem。

`[Obsolete]` 沿繼承鏈遞移並預設隱藏（整批 `VarXxxProviderRef` 都是），避免挑到廢棄型別。

## 2026-08-25 —— hard budget、merged locate、批次 probe、寫後驗證

依累積 2421 次 usage（6.72M 字元）重排優先度：`prefab read` 占 68%，而一般 `find`
對常用 component 的 2807 筆命中有 2677 筆來自只供 override 解析的 shallow tier。

- `find` 預設改成 `--scope full`；`--scope all` / `shallow` 才擴大，表尾會講被隱藏數。
- prefab / scene / obj 共用 hierarchy + FSM 總輸出的 hard budget；`depth` 只是最大深度，
  不能繞過。新增 `--fsm-only` / `--structure-only`。
- `prefab locate` 在 Unity 合併後真值裡一次按 component / name 定位；`peek-batch` 用
  `node|comp|members` 清單一次讀多顆欄位。
- prefab batch 檢查 SaveAsPrefabAsset、reload 驗證 touched 值；`active` 支援 inherited nested
  override。`--quiet` 只壓縮成功 log，錯誤不裁。
- read cache 改成 `--cache` opt-in；預設與 `--no-cache` 都完全不讀不寫。key 額外含直接影響
  匯出格式的 Python/C# tool hash。這層只省 Unity latency，不宣稱省 context。
- `usage --since <hours>` 可只看新資料；報告新增 avg/p95、cache hit ratio 與 budget 超量數。

舊章節的「拆 cache」指人工 marker `.md` 機制；後來加回的 CLI cache 已依上面改成 opt-in，
不再與「預設讀當下真值」衝突。

## catalog 自動增量刷新（2026-09-02）

**問題**：catalog 只在 `up index` 時整批重建，改了 .cs 卻沒重跑索引時，
`up catalog` 會回舊的 summary（實際案例：`VarFloatIsBoundCondition` 已有完整
`/// <summary>` 卻仍顯示 `⚠ 沒有 summary`）。靠人記得重建不可靠。

**做法**：查詢時自動對齊。`cmd_catalog` / `cmd_fields` 進來先跑
`indexer.refresh_catalog()`，走 mtime/size 增量：
- 全庫 .cs walk 只要 0.16 秒，那 4.5 秒的成本全在 parse —— 所以沒改檔時刷新幾乎免費
- 新增 `cs_files(path, mtime, size, bases)` 表。存 bases 是因為 kind / obsolete
  要沿**全庫**繼承鏈遞移，中繼 class 的 base 不能只留在被改動的那幾支檔案裡；
  未變動檔案的 base 表從這裡取回，不用重讀原始碼
- catalog 表加 `self_obsolete` 欄。resolve_obsolete 會把 base 的 [Obsolete] 遞移給子類，
  增量時若拿「遞移後的值」當種子，base 拿掉標記後子類會永遠清不掉

**實測**：無變動 0.18s、改 1 支 0.24s、初次全建 4.0s；增量結果與全建 4462 列逐欄比對 0 差異。

**刻意不做**：沒有把增量下放到「只重算受影響的子樹」——繼承鏈重解整批也才幾十毫秒，
不值得為此維護反向依賴圖。

## `prefab read` 翻掉 FullExpand 預設（2026-09-03）

**問題**：`uprefab.py` 的 `fold = not args.fold`，而 `--fold` 是 `store_true` 且連 help 都沒寫 ——
不帶旗標就送 `fullExpand=true`，於是拿到 `HierarchyExportOptions.FullExpand`：不摺已知子樹、
不排除 Renderer/ParticleSystem/AudioSource/Light/Cloth、欄位無上限。918 次 read 只有 14 次帶
`--fold`。最省的那個模式被命名成 opt-in，等於整年都在付全展開的錢。

**做法**：旗標反轉成 `--full`（`prefab read` / `scene ls` / `obj` 三個入口一起），預設走
`Options.Default` + 排除視覺 component。

**先補的安全欄（順序不能顛倒）**：`PrefabTextReader.Options()` 一律
`options._expandPaths.Add("")`，讓匯出的根節點永不摺疊、只摺後代。沒有這一步，
「`--node` 下鑽到某個 StateFolder」會被 `SubtreeSummarizerRegistry` 折回一行摘要 ——
使用者為了看細節才點名，卻只拿到摘要，read 整趟白花。三個入口共用 `Options()`，一處到位。

**踩到的坑**：`IsForcedExpand` 原本第一行是 `if (string.IsNullOrEmpty(e)) continue;`，
所以照字面「加空字串」是**無效操作**，靜默沒效果。要先讓 `""` 有語意（只匹配 path 為空的根節點），
才能拿它當 forced-expand root。`e == null` 的守衛要留著。

**連帶**：readcache 的 param key `fold` → `full`，舊 cache 自然失效（值語意反了，本來就該失效）。

**刻意不做**：沒有為「根節點永不摺疊」加獨立的 `_forceExpandRoot` bool。`_expandPaths` 已經是
既有機制，多一個旗標就多一條要同步的分支。

**驗收**：無旗標讀 Socket FSM prefab 回摺疊摘要（`:: 3 states: …`）；
`--node "[StateFolder] StateFolder"` 回 910 字元完整展開，沒被折回一行。

## `up catalog` 清單模式的 compact（2026-09-03）

不帶 keyword 的寬清單是這支工具最大的 token 消耗源（實測 `catalog action` 全量 24,695 chars，
其中縮排欄位行佔 46%）。現在 `cmd_catalog` 在「無 keyword、無 -v、無 --path」時進 compact。

**為什麼不是把欄位行全砍**：對「⚠無說明」的列，欄位行是使用者判斷這個型別在幹嘛的唯一依據，
砍掉它等於這一列白印。所以砍的只有「有 summary 那些」的欄位行；無說明的列反而保留欄位、
改砍路徑（折成表尾一行計數）。等 242 筆 summary backfill 補完，這半邊會自然收斂。

**`-n` 沒有固定預設**：`default=None`，在 cmd 裡分成「有 keyword 200 / 無 keyword 60」。
寬清單本來就不該一次吐 200 筆，帶 keyword 的已經自帶收斂（實測平均 1,206 chars）。

**刻意不做**：沒有加 `--compact` / `--brief` 旗標。多一個旗標 agent 就不會用它，
要省的量必須發生在預設路徑上；要細節的出口是既有的 `--type` / `-v`。

**驗收**：`catalog action` 全量 24,695 → 16,257，預設 -n 60 為 6,920；
`--type <X>`、`catalog action <keyword>` 維持 ~400 chars 且欄位齊全。

---

## `up prompt` 的條件改成「只補不刪」（2026-09-03）

`PromptEdit.EnsureCondition` 原本假設「case 就是這顆 value source 條件的唯一真相」：
沒給 `if:` 就清空既有 `VarBoolCompareCondition`，給了 `if:` 就搶第一顆既有的改寫。
從零建 case 時這樣最乾淨，但實際最常見的用法是**對既有 source 補一顆條件**
（文案留空、只給 `if:`），於是人工掛好的條件被靜默換掉。

為什麼特別惡劣：回傳自帶的 `[值]` 驗證抓不到。少一條 AND 條件時字串一樣組得出來，
所以「驗證通過」反而給了假的安全感，只有事後人工盤點才會發現。
**凡是驗證抓不到的破壞性行為，預設就不該做。**

現在的語意：

- 「這顆條件已存在」用 (`_varBool` 參照, `targetValue`) 判定 —— 兩者都是精確值，
  刻意不做條件的語意等價比較。
- 認不出來的（`ConditionRef` 這種 proxy）一律視為不同 → 多新增一顆。
  寧可多一顆重複條件（AND 起來不改變結果）也不動別人的節點。
- 只看 source 的**直接子節點**。孫層是巢狀 condition group，那是別人的結構。
- 要清空重建才給 `--case-replace-conditions`，而且刪任何一顆都會印
  `⚠ 已移除既有條件: <節點名>（<型別>）`。刪除不准靜默。

## 同源的兩個延伸（同日）

**`EnsureToken` 有一模一樣的 hijack**：`FirstOrDefault(名字對得上) ?? FirstOrDefault()`，
那個 `??` fallback 會在 token 名對不上時搶第一顆既有 binding 改寫，把 `{grabKey}` 變成
`{throwKey}`。改成以 `_variableName` 為身分：同名就只更新 `_promptData`（換資產會印出來），
沒有同名的才新增。找同名掃全部後代（binding 是 `[AutoChildren]` 收的，不限一層，
只看直接子節點會多長出一顆同名的壞資料），但新建與清除只動直接子節點。

`--case-replace-tokens` 和 `--case-replace-conditions` **刻意分成兩個旗標**：
conditions 與 tokens 是無關的兩棵子樹，合成一顆會讓「我要重建條件」順手把 token binding
清光 —— 那正是這一輪在修的 bug 類型。而且 replace-tokens 只清 `InputPromptTokenBinding`；
`SmartStringTokenBinding` 是 `--case` 語法蓋不到、只能手工組的，清掉沒人補得回來。

**條件的掃描範圍要以 `ConditionGroup` 為準，不是以 `--case` 能產生什麼為準**。
`ConditionGroup._conditions` 是 `[AutoChildren(DepthOneOnly = true, _isSelfInclude = false)]
AbstractConditionBehaviour[]`（直接子節點、任何子型別、AND）。原本只認
`VarBoolCompareCondition` 造成兩個症狀：只掛 `ConditionRef` 的 source 被誤判成「無條件」
而報假 `[warn]`；proxy 型別也不在保護與警告範圍內。

現在計數／警告／清除都認 `AbstractConditionBehaviour`，**等價比對才縮回只對
`VarBoolCompareCondition` 做**。proxy 要追到它指向的目標才知道等不等價，那等於在
editor 這邊重新實作一次條件語意 —— 不做，一律當作「不同的」並印
`⚠ 偵測到 N 顆非 VarBoolCompare 條件（ConditionRef 等），未納入等價比對，一律保留`。
多一顆重複條件 AND 起來不改變結果，弄丟一顆會改變結果。

順手補了 `ConditionRef` 的 `/// <summary>`（catalog 原本是 `⚠無說明`）。

## `readcache` 翻預設 + 收窄 TOOL_FILES + 加一層 60 秒 argv memo（2026-09-03）

原本 `prefab read` 的磁碟快取要顯式 `--cache`，實測 415 次只有 21 次命中（5.1%）——
**旗標式的正確性保證是假的**：沒人記得加，所以那層快取等於不存在。改成預設開、
保留 `--no-cache` 當逃生口，正確性靠 key 本身（依賴 prefab 的 mtime+size ＋ 匯出工具指紋）。

**TOOL_FILES 收窄到只剩「決定輸出格式」的 C#**（`PrefabTextReader.cs`、`FsmTextExporter.cs`、
`HierarchyText/*.cs` 用 glob 收，新增檔案自動納入）。拿掉 `uprefab.py` 與 `readcache.py`：
那兩支開發期天天存檔，而改的多半是別的子指令，卻會炸掉整包快取 —— 這是「改一行 CLI
就要重打 400 次 Unity」的直接來源。驗收：`touch uprefab.py` 後同指令仍然 hit。

**踩到的順序陷阱（重要）**：把 `readcache.py` 移出 TOOL_FILES 之後，改它的切片邏輯**不會**
改變 key —— 切片改壞時毒掉的 `.txt` 會在修好後繼續命中。所以選了「手動 bump
`CACHE_FORMAT_VERSION`」這條（3 → 4），並在該常數旁寫死一句「動到 `_slice` 或 key 組成就 +1」。
沒有選「把切片邏輯留在 TOOL_FILES 裡」，因為那等於把 readcache.py 整支放回去，
又回到「改個註解就全失效」。

**本地切片只做 node 前綴，刻意不做 depth**：exporter 的摺疊是整棵樹一起算的
（`PrefabTextReader.Layered` 用 charBudget 探出一個全域深度），本地無法重現「只讀這顆子樹時
budget 允許展到第幾層」。裁 depth 會給出一顆看起來完整、其實少幾層的子樹，而那種錯誤
沒有任何徵兆。node 前綴則是純文字操作：只要目標那行到子樹末尾**沒有任何摺疊標記**
（`(+N nodes)` / `(+N more siblings)`），這段就是完整展開的子樹。有標記就放棄，寧可多打一趟。
同名 sibling 或路徑帶 `[n]` 索引也一律放棄（文字裡分不出是哪一顆）。

**切片與直接 read 有一處不等價（實測發現，不是推論）**：指到子樹**外面**的引用，
在大匯出裡是 `@../../../[Var] Hunger 飢餓值#VarFloat`，直接 read 那顆節點時會變成
`res:0_Gameplay/0_Base/PPlayer.prefab#VarFloat`（`GetRelativePath` 在匯出 root 處被夾住）。
18 行裡 7 行有這個差異。判斷是「資訊更多不是更少」（相對路徑點名了目標節點，`res:` 形式
連節點都看不到），而且兩種寫法都解析得回去，所以保留切片，但把這件事寫進切片的表頭
—— 不能宣稱「逐字等同」。

`key_for` 的 `rel.endswith(".prefab")` 限制**沒有拆**：scene 的依賴（instance + override）
算不準，快取 scene 讀取不安全。

**第二層 memo（`memo.py`）**：同 argv 60 秒內直接回上次**實際印出**的內容（含截斷提示，
所以 replay 逐字相同）。失效條件刻意做得很粗 —— TTL 60 秒，且期間跑過任何一個寫入類
指令就整批失效（bump `.uprefab-cache/memo/epoch`，且是在指令**跑之前**bump，中途炸掉
也不會留下可疑的 memo）。`peek` / `logs` / `effect-trace` 列在 `NEUTRAL`：**讀但不 memo**，
因為那些的答案本來就每秒都在變，60 秒的 memo 會讓「改完再確認」看到改前的值。

**驗收數字**
- 十次擬真調查（PPlayer + Welder，含回頭複查與往更深下鑽）：hit 5 / slice 2 / miss 3
  = **70%**（原本 5.1%）；命中那幾次 17–19ms，miss 是 200–516ms。
- `touch uprefab.py` 後同指令仍 `cache=hit`（14ms）。
- 同一條 `find` 連打兩次：第二次 memo 命中、in-process 0ms、整趟 67ms（全是 Python 啟動），
  輸出 `cmp` 逐字相同。

## 給 `up logs` 與 `--budget 0` 補預設上限（安全欄，不是省量）（2026-09-03）

`logs` 的 `-n` 從 10 拉到 100（10 筆常常看不到真正的第一個錯），代價是輸出可能爆掉，
所以同時加三道：單則訊息截到 400 字元（附原長）、整體 8,000 字元、**相同訊息摺疊成 `xN`**。
摺疊是這裡最有效的一項 —— 一個 FixedUpdate 裡的 error 每幀重印，逐筆列出來是同一句話
幾十次（舊版 6 次呼叫 162,625 字元）。要看的是「有幾種錯」，不是「印了幾次」。

`--budget 0` 現在只解除 Unity 端那層，仍會被全域 `--max-chars` 攔住，並印一行明講
「真的要無上限請同時給 `--max-chars 0`」。反過來，明確給了大 budget 的人不該被全域上限
攔住，所以有效 cap 至少放到 `budget + 2000`。

**全域 Tee 當第二道網**（`usage.Tee(cap, hint)`）。截斷行一定要帶**原長 + 縮小範圍的建議**：
只說「被截斷了」會讓 agent 原封不動重打一次更貴的指令。建議句按子指令查表（`CAP_HINTS`），
`overrides` 的那句指名 `--by-target`。

`find` 的結論（`N / 共 M match(es) —— 被 -n 切掉了`）改成**印在明細之前**（表尾再重複一次）。
明細可能被 cap 攔在中途，而表尾那句是唯一會改變結論的資訊 —— 截掉它換來的是一次
錯誤結論的重查（「這個 component 只有這幾處用到」）。

**HardCap 的 +1 off-by-one 修在 Python 端**，不是 C#：`HardCap` 已經截到剛好 charBudget
（結尾自帶換行），是 `print(text)` 又補一個換行才變成 35001/35000。改用 `_emit()`
（`sys.stdout.write`，只在沒有結尾換行時補一個），三個吃 budget 的子指令
（`prefab read` / `scene ls` / `obj`）都換過去。

**驗收數字**
- `up logs`（不帶參數）798 字元；`--type all` 100 筆摺成 42 種、2,957 字元。
- `up overrides "村莊車站測試.unity" -n 100000 --all`：完整輸出 2,583,052 字元 →
  攔在 30,138 字元，截斷行報出原長並指向 `--by-target`。`--max-chars 0` 仍能取回全部。
- `up prefab read PPlayer --budget 500` 輸出**剛好 500** 字元（原本 501）。

## `up` 的錯誤路徑：大小寫不敏感 → near-match → 精簡 `--help`（2026-09-03）

三層，由便宜到貴：

1. **大小寫不敏感**。七個 enum 參數（`scope`/`find --scope`/`scene action`/`prefab action`/
   `catalog kind`/`logs --type`/`play action`）加 `type=_ci(...)`，argparse 會在 choices
   檢查前正規化。子指令名與 `asset` 的第二層則只能先改 argv（argparse 沒有這個開關）。
   `up catalog Condition`（大寫）原本整條失敗，現在跑得起來。
2. **順手修掉的同類問題**：全域旗標寫在子指令後面（`up overrides X --no-memo`）原本會
   變成「不認得 --no-memo」。`_hoist_globals` 把 `--root` / `--max-chars` / `--no-memo`
   搬到最前面 —— 那個位置限制是 argparse 的實作細節，不是使用者該記的事。
3. **統一的 near-match 出口**：`_Parser.error()` 取代 argparse 的「一行訊息 + 一整份 usage」
   （實測 ~900 字元）。invalid choice → difflib 挑最接近的三個；unrecognized argument →
   跨所有子指令的 option 表反查，並標出那個旗標屬於哪些子指令；required argument →
   一句話。Traceback 那條路不管（它不是 argparse 出口）。
4. **`peek` 缺 `--comp`**：改成呼叫新的 `EditProbe.ComponentNames` 列出該節點的 component
   名稱（`prefab peek` 與 scene `peek` 共用，`up peek` 的 `comp` 也改成選填）。
   **只取 `GetType().Name`，絕對不呼叫 property getter** —— 盲掃屬性會在 native 層
   abort 掉整個 Editor（見 `Peek` 的註解與 `reference_up_peek_property_getter_crash`）。
5. **`--help` 重寫成 compact**：`<必填>` / `[選填]` / 每個 enum 的合法值全留，砍掉的是
   每個旗標的說明段（那個去 `up <子指令> --help` 看）。`asset` 是唯一的兩層子指令，
   它的七個 action 連參數一起展開 —— 不展開的話「`asset <asset_action>`」等於什麼都沒說，
   會逼人再叫一次 `up asset --help`（5,312 字元）。**刻意保留 enum 合法值**：少列它會
   逼 agent 為了確認「condition 還是 conditions」再叫一次，比多印幾十字元貴得多。

**沒有重做**：`--node` 找不到時列 sibling（`PrefabTextReader.cs:63` → `DescribeChildren` 早就有）。

**驗收數字**
- `up catalog Condition`（大寫）正常輸出 80 個 Condition。
- 錯誤輸出：打錯子指令 73 字元、打錯旗標 92 字元（原本 argparse 的 usage ~900）。
- `up --help` 3,877 字元（含全部子指令、必填參數、七組 enum 合法值與 asset 的兩層展開）。
  註：TODO 記的 10,700 是舊版數字，改動前實測已是 2,294 —— 這一則實際做的是
  **用差不多的字元數換到完整的 enum 與參數資訊**，不是壓縮。

## `up asset do -f` 批次寫入（理由是原子性，不是 token）（2026-09-03）

**先查 `prefab peek-batch` 為什麼沒被續用**：usage log 裡是 **3 次不是 0 次**（TODO 的
「0 次」不對），而且三次都 `st=ok`、輸出 688–2,613 字元，功能是好的。門檻是
**它只吃 `-f <檔案>`**：`prefab do` 可以直接把 op 當位置參數帶進去，peek-batch 一定要先
寫一份 `/tmp/probes.txt`（三次呼叫的 `file` 欄位正是 `/tmp/probes.txt`、`/tmp/p2.txt`）。
多一個「寫暫存檔」的步驟就足以讓人退回逐顆 `peek`（300 次）。**結論：要補的是
peek-batch 的 inline 位置參數，不是格式說明**（記進 TODO 由 Jerryee 排序，這一則沒做）。

`asset do` 因此一開始就同時吃三路：`-f 檔案` / 位置參數 / stdin（與 `prefab do` 共用
`_ops_text`，只是關掉「第一個位置參數當 op」—— 那個位置在 asset 是 assetPath）。

**原子性怎麼做到的**：整批共用一個 `SerializedObject`，任一行失敗就直接回傳、
**不呼叫 `ApplyModifiedProperties`**。SerializedObject 天生就有這個性質 —— 不 Apply
就等於沒發生，不需要另外做備份/回滾。同時把 `SetField` / `SetAssetRef` /
`AddArrayElement` 重構成共用 `DoSet` / `DoRef` / `DoAddElement`：單次呼叫與批次**必須是
同一份實作**，不然「單次試通了就寫進批次」這個工作流會在批次裡出現不同語意。

**刻意不支援 `invoke`**：那是反射呼叫方法、直接改物件狀態，不 Apply 也已經發生，
放進「全成功才生效」的批次裡只是假的原子性。

順手修了 `EditBatch.Run` 的誤導訊息：原本說「前面 N 個操作**已生效**」，但 prefab 與 asset
的批次都是全成功才落地（`PrefabEdit.Batch` 不存檔、`AssetEdit.Batch` 不 Apply），只有
`scene do` 是直接改在開著的場景上。改成「前面 N 個操作執行成功、後面的都沒跑
（是否落地看下一行）」。

**驗收數字**
- 四行 ops、第三行故意寫不存在的欄位：回報停在第 3 行 + `# 整批未套用`，
  `grep` 檔案內容 `_duration: 2`（預設值）、`_priority: 0` —— **完全沒變**。
- 同樣四行改成合法後：三個欄位都寫進去。
- `addel|_events` + `aref|_events.Array.data[0]|<asset>` 同一批次成功
  （SerializedObject 撐得住「同批次內先長陣列再填元素」）。
- `ucompile.sh`：**ErrorCount=0 / warnings=0**（動到 `AssetEdit.cs`、`EditProbe.cs`、
  `EditBatch.cs`）。

## `revert|` —— 清掉單一 property override（2026-09-03）

**存在理由**：重存 descendant prefab 時，存檔前 callback 會生出「合併後等於繼承值」的
無效 override（`EffectDetectable._effectDetectTargets.Array.data[0] = null`）。
`PrefabUtility.RevertPropertyOverride` 是唯一的解，但 `prefab do` 沒有對應的 DSL。

**執行時機是這一則的全部重點**。`Dispatch` 裡所有 verb 都是同步跑在 `EditBatch.Run`
的迴圈裡，沒有 deferred 階段。如果 `revert` 也在那裡直接執行，順序會是
`revert` → `RunBeforeSaveCallbacks`（重跑 `OnBeforePrefabSave` / `[Auto*]` 填值）→ `Save`
—— callback 會把剛清掉的 override 原封不動寫回去，整個操作等於沒做。

所以 `revert` 拆成兩段：`Dispatch` 只把 `(node, comp, fieldPath)` 推進 `PendingRevert`
佇列並回一行 log（路徑 / component 名在這裡就解析，打錯還來得及走「整批不存檔」）；
真正的 `RevertPropertyOverride` 由 `PrefabEdit.Batch` 呼叫 `ApplyReverts`，插在
**`RunBeforeSaveCallbacks(root)` 之後、`SaveAsPrefabAsset` 之前**（`PrefabEdit.cs`
Batch 方法內，callbackLog 那一行的下一行）。

**刻意不做**：
- `ApplyReverts` 失敗不擋存檔。到那一行已經過了「任一行失敗整批不存檔」的閘門，
  前面的結構改動要嘛全落地要嘛全不落地；因為 revert 失敗就把它們丟掉更糟。
- 不加到 `SceneEdit`。scene 的 batch 是直接改在開著的場景上、沒有 callback 階段，
  「排隊延後執行」在那裡沒有意義；真的需要再說。
- `isDefaultOverride` 直接拒絕並說明（`m_Name` 之類 Unity 強制欄位 revert 不掉），
  不要讓它變成「呼叫成功但沒效果」。

**踩到的坑**：`RevertPropertyOverride` 直接改 instance 的 `m_Modifications`，不經
`SerializedObject`，所以不能也不該 `ApplyModifiedProperties`；但**一定要重新
`new SerializedObject` 讀一次**才知道清掉了沒 —— 舊的 `SerializedProperty` 上
`prefabOverride` 不會跟著變。另外「本來就不是 override」不算失敗（語意是「確保這欄位是繼承的」），
但會印一行講清楚。

**驗收數字**（臨時 variant，測完已刪）
- `set|…|GeneralState|_priority|7` → peek 顯示 `_priority* = 7`、寫後驗證回
  `override*（值真的留在這顆 prefab 上）`。
- 接著 `revert|…|GeneralState|_priority` → `# revert（callback 之後執行）：1 個 OK`、
  驗證 `1 個 OK`、peek 變成 `# 沒有任何 override，整顆繼承自 …`、`_priority = 0`（base 值）。
- **未能重現原始症狀**：對 `煤炭箱 Variant` / `打雷黑雲` 各開一顆 variant 再存一次，
  `up overrides` 都回 `(no overrides)`，那個 `_effectDetectTargets.Array.data[0] = null`
  沒長出來。所以「revert 排在 callback 之後才有效」這一點是照程式碼結構推的，
  不是實測對照過兩種順序。下次真的遇到那顆無效 override 時值得回來補測。

## peek / locate / 寫後驗證吐 override 狀態（2026-09-03）

**價值不是省 context**（直接節省約 5 千 token，可忽略）。價值是把「寫進去了沒」
從「另外跑一次 `overrides`（平均 21KB）」變成 peek 一眼。

判準抽成 `PrefabOverrideMark`（`prefabOverride && !isDefaultOverride`），
`HierarchyTextExporter.BuildComponentEntry` 改成呼叫它 —— 那條判斷原本是 private static
寫死在匯出器裡，複製第二份的代價是「一邊有星號一邊沒有」，比沒星號更難查。

**沒有按視角 gate**。先驗證過兩種載入視角（`AssetDatabase.LoadAssetAtPath` 對 peek，
`PrefabUtility.LoadPrefabContents` 對 locate / peek-batch / 寫後驗證）：2768 個
`(node, component)` snapshot 下 `prefabOverride` / `isDefaultOverride` /
`IsAddedComponentOverride` 結果完全一致，含 variant root 層與繼承自 nested prefab
instance 的節點。所以不需要「為了星號另開一次 asset 視角」（純多付一次載入），
也**不能**把星號 gate 在 `GetPrefabInstanceStatus` —— 那是兩種視角唯一會不同的 API
（contents 視角回 Connected、asset 視角回 NotAPrefab）。

**踩到的坑**：`EditProbe.Dump` 是反射讀值（成員名），override 判定只有
`SerializedProperty` 有（序列化名）。專案自己的 `_field` 兩邊同名，但 Unity 內建
component 是 `m_AnchoredPosition` vs C# 屬性 `anchoredPosition` —— 第一版
`--members anchoredPosition` 一個星號都沒有，明明是 override。補了
`PrefabOverrideMark.Contains` 的 `m_` + 首字大寫備援才對上。

**刻意不做**：不對 `--members` 裡的巢狀路徑（`_field.x`）算 override —— `Dump` 的
反射入口本來就不吃巢狀路徑。

**驗收數字**
- 有改過的節點（`Enemy Chasing … 神像廟 Variant` 的 `KeepDistanceMoveAction`）：
  peek 回 `_desiredDistance* = 15`、`_deadZone* = 2`，其餘 16 個欄位無星號，
  表頭 `# * = 這顆自己 override 的欄位；其餘繼承自 Enemy Chasing Flying Cloud 打雷黑雲`
  —— 與獨立量測出的 override 集合 `[_desiredDistance, _deadZone]` 完全一致。
- 純繼承的節點（同一顆 variant 的 `GeneralState` / `TransitionBehaviour`）：
  peek 與 peek-batch 都是 `# 沒有任何 override，整顆繼承自 …`，**0 個星號**（無假陽性）。
- 沒有 base 的 prefab（`打雷黑雲.prefab`）：不印來源行、0 個星號。
- nested instance 的來源標成
  `煤炭箱 Variant（nested instance root: Fixable ModulePack）`。
- `locate --members` 與 `peek-batch`（contents 視角）與 peek（asset 視角）結論一致。

## `active|` 對繼承自巢狀 prefab 的節點（2026-09-03）

**原始症狀重現不出來**。對 `煤炭箱 Variant` 的
`…/[Receiver] Interact/[Event] EffectEnterBestMatchNode`（繼承自巢狀
`Interact Trigger ModulePack`）下 `active|…|true`：存檔後 `read --no-cache` 讀回來
**是啟用的**，改回 `false` 也照樣生效。`case "active"` 裡那行
`PrefabUtility.RecordPrefabInstancePropertyModifications`（在 `SetActive` 之後）
已經把寫入修好了；TODO 記的症狀應該是在那行加上去之前量的。

**偵測本來就有**（這一則的最低要求已經滿足）：`active` 會 `touches.Add(VerifyTouch
.TransformValue(node, VerifyKind.ActiveSelf, verb))`，存檔後 `VerifyReloaded` 重新
`LoadPrefabContents` 比對 `activeSelf`，不一致就進「驗證失敗明細」。不可能靜默成功。

這一則實際補的是**第三種狀態**：值對了、但 override 沒留下來。
`VerifyTouch.NoteOverride` 讓 `active` 與 `set` / `rect` 的驗證多印一行
`# override 狀態（存檔後重讀）`，三種結果分開講：`override*`（值真的留在這顆 prefab 上）、
`defaultOverride`（Unity 強制欄位）、`繼承`（沒留下 override —— 剛改過就代表沒生效或等於 base）。
只在 `IsPartOfPrefabInstance` 為真時印，純 prefab 自己的資料沒有 override 這回事，不加噪音。

**驗收數字**：煤炭箱那個節點兩次 `active`（true 再 false）都回
`1 個 OK，0 個失敗` + `activeSelf = override*`，`read --no-cache` 兩次都跟預期一致，
**最後已改回原狀（停用）**。

## `rect|` + `rot` 吃 root + `set` 吃 Quaternion（2026-09-03）

### (a) `rect|` 而不是讓 `pos` 偷偷改語意

`pos` 寫的 `localPosition` 對 UI 節點是 Canvas 佈局的**輸出**不是輸入：對
`GGameplayUI` 的 `[Popup] Money Delta 資金變化` 下 `pos|150,0,0`，原本讀回來是
`(407.065, 0.212, 0)`。

選了「新增 `rect|`，並讓 `pos` 遇到 RectTransform 時印一行警告指向 `rect`」，
**不讓 `pos` 自動改寫 anchoredPosition** —— 同一個 verb 依目標型別做不同的事，
下次讀到 log 的人無法從指令看出到底寫了哪個欄位。

`rect|<node>|<ax,ay>|<w,h>|<anchor>|<px,py>`，四格都可留空 = 不動那一項；
一項都沒給就 abort（避免「跑成功但什麼都沒改」）。anchor 吃 Inspector 上那組 preset
名字（`center` / `top-left` / `stretch` / `stretch-h` …）或 `minX,minY,maxX,maxY`
—— 手算 anchor 是 UI 改動最容易寫錯的一步。

**驗證免費拿到**：四項都是 RectTransform 的序列化欄位（`m_AnchoredPosition` /
`m_SizeDelta` / `m_AnchorMin` / `m_AnchorMax` / `m_Pivot`），所以直接用既有的
`VerifyTouch.Serialized`，不用新增 `VerifyKind`。

### (b) `pos` / `scale` / `rot` 的 `<node>` 留空 = root

原本三者第 0 個參數走 `EditBatch.Need`（缺就 abort），導致「從場景複製出來的 prefab
root 帶殘留旋轉要歸零」這個最常見用途做不到 —— root 的路徑就是空字串。改成 `At`，
跟 `set` / `add` / `comp` / `rename` / `delcomp` 一致。
**TODO 只要求 `rot`，這裡連 `pos` / `scale` 一起改了**：留兩個 verb 語意不同是個
會害人多跑一趟的坑，而「空 = root」已經是這套 DSL 既有的慣例。

### (c) `set` 吃 Quaternion / Vector4

`EditResolve.ApplyValue` 補 `Quaternion`（`"x,y,z,w"` 四元數或 `"x,y,z"` 歐拉角）
與 `Vector4`，同時補 `Preview`（Quaternion 印歐拉角，四元數印出來沒人看得懂）
與 `PrefabEdit.Snapshot`（不補的話寫後驗證會回 `unsupported-property`）。

**驗收數字**（臨時 variant + `GGameplayUI` 的臨時 variant，測完全刪；沒有改到
`GGameplayUI` 本體）
- `rect|…|150,0|240,60|top-left|0,1` → 驗證 `5 個 OK，0 個失敗`，讀回來
  `anchoredPosition* = (150.00, 0.00)` / `sizeDelta* = (240.00, 60.00)` /
  `anchorMin* = anchorMax* = (0.00, 1.00)` / `pivot* = (0.00, 1.00)`，
  `localPosition` 也跟著變成 `(150, 0, 0)`（對比 `pos` 的 `(407.065, …)`）。
- `pos|<RectTransform 節點>|150,0,0` → 照樣寫，但多印
  `# 注意：這是 RectTransform，localPosition 會被 Canvas relayout 覆寫…`。
- `rect` 對非 RectTransform → `# 未修改：… 上是 Transform 不是 RectTransform`，整批未存檔。
- `rot||30,0,45`（空 nodePath）→ root 的 `localEulerAngles = (30, 0, 45)`，驗證 1 個 OK。
- `set||Transform|m_LocalRotation|0,0,0,1` → log `(30, 0, 45) (euler) -> (0, 0, 0) (euler)`，
  驗證 1 個 OK；`0,90,0`（三分量歐拉）同樣通過。
- 順手量到的事實：variant root 的 `m_LocalRotation` 是 **defaultOverride**，所以
  peek 不會給它星號 —— 寫後驗證那行 `defaultOverride（Unity 強制欄位）` 就是這樣看出來的。

## `prompt --var` 定位名字含字面 `\n` 的節點（2026-09-03）

三個問題疊在一起：
1. 路徑逃逸規則缺 `\\`。`\n` 一律被還原成真換行，而自動命名會生出
   `Concat Concat 4 段 (以 "\n" 相接)` 這種**名字裡真的有反斜線**的節點；
   `\\n` 在舊規則下是「反斜線 + 換行」，所以兩種寫法都指不到。
2. `PromptEdit` 自己複製了一份 `SplitPath` / `FindChild`（因為 `EditResolve` 是
   internal 且 `MonoFSMPro.Editor` 根本沒 reference `MonoFSM.Core.Editor`），
   沒有同層自動命名容錯 —— 所以 `prefab read` / `do` 靠 fallback 過得去，`prompt` 硬失敗。
3. 沒有「照字面比對」的逃生門。

三個都做了：
- `EditResolve` 補 `\\` 逃逸（`SplitPath` / `Unescape` / `HasEscapedSlash` / `EscapeName`），
  `HierarchyTextExporter.NodeName` 也一起補，不然 read 列出來的名字抄回去解不回原名。
- `MonoFSMPro.Editor.asmdef` 加 reference `MonoFSM.Core.Editor`
  （確認過反向沒有 reference，不會成環），並在 `MonoFSM/1_MonoFSM_Core/Editor/AssemblyInfo.cs`
  開 `InternalsVisibleTo("MonoFSMPro.Editor")`。**刻意不把 `EditResolve` 改成 public** ——
  那套 API 是 CLI 內部慣例，不是給 runtime 用的。`PromptEdit.ResolveNode` 改成薄薄一層
  轉呼 `EditResolve.TryNode` / `DescribeChildren`，自己那份 `SplitPath` / `FindChild` 刪掉。
- 新增 `prompt --var-literal`：路徑只按 `/` 切、段內完全不 unescape
  （`EditResolve.TryNodeLiteral`）。代價寫進 help：名字含 `/` 的節點在這個模式下指不到。

**踩到的坑**：`Unescape` 原本是連續 `Replace("\\/", "/").Replace("\\n", "\n")`，
補 `\\` 不能再加一個 `Replace` —— `\\n` 會先被 `\n` 那條吃掉變成真換行。改成一次
掃過去的迴圈，`\\` 必須先判斷。`EscapeName` 反向同理：`\\` 一定要**最先**替換。

**驗收數字**
- `up prompt --check --var '…/Concat Concat 4 段 (以 "\\n" 相接)'` → **定位成功**，
  印出四顆 value source 的組合結果與 token 檢查。
- 同一顆節點用 `--var-literal` 加單反斜線路徑（`(以 "\n" 相接)`）→ 也成功。
- 不加 `--var-literal` 用單反斜線路徑 → 也成功，但尾端多一行
  `# 節點 '…' 找不到，自動對應到同層的 '…'`：這是共用進來的同層容錯在作用，
  正好證明「抽出來共用」本身就修掉了原本的硬失敗。
- 路徑真的打錯 → `DescribeChildren` 列同層候選（名字已 `\\n` 逃逸好可直接抄），
  外加一行「名字裡真的有反斜線的話寫 `\\`，或改用 --var-literal」。
- `prefab locate --name Concat` 回的路徑現在是 `(以 "\\n" 相接)`，抄回 `--node` peek 成功
  —— 逃逸的往返在 read 與 write 兩端都閉合了。

**編譯**：五則全部做完 `.claude/scripts/ucompile.sh` → **ErrorCount = 0**（warning 187，
全是既有的）。動到 `PrefabEdit.cs` / `EditProbe.cs` / `EditBatch.cs` / `EditResolve.cs` /
新增 `PrefabOverrideMark.cs`、`AssemblyInfo.cs` / `HierarchyTextExporter.cs` /
`PromptEdit.cs` / `MonoFSMPro.Editor.asmdef` / `uprefab.py`。

### 這一輪動過的 gameplay asset（驗收用，值已還原）

`煤炭箱 Variant.prefab`：為了驗 `active|` 存檔了兩次（true → false，已回原狀）。
兩次存檔各跑了 332 個存檔前 callback，所以檔案裡多了一批**自動命名重算**的 diff
（`[local] X` → `[Var] X`、`[If] … == [Var] d_RequiredHandToolKind` → `… == d_RequiredHandToolKind`
之類），那是 `AbstractDescriptionBehaviour` 對現行命名規則重新推導的結果，任何一次
`prefab do` 存檔都會發生，不是這次改動的語意變更。

同一份 diff 裡的 `d_Coal Amount 煤量` 的 `_localField.DevValue: 30 → 0`
**不是這次造成的**：把 `煤炭箱 Variant` 整份 copy 成獨立 prefab、把 DevValue 設回 30、
再存一次（callback 照樣跑 332 個），值仍是 30 —— 存檔前 callback 不會重設它。
那筆是先前就在工作區裡的未 stage 改動。

`GGameplayUI.prefab`（18:59）與 `路邊發電鴿和底座 Base statue Variant.prefab`（17:11）
的 mtime 都早於這次 session，**沒有被寫過**：UI 的 `rect` 驗收是在 `GGameplayUI` 的
臨時 variant 上做的，prompt 的驗收只用 `--check`（唯讀）。臨時 asset
（`_tmp_uprefab_revert_test` / `_test2` / `_rect_test` / `_reg` / `_tmp_devval`）全部已刪。

## Transform 系 op 的靜默假陽性（2026-09-03）

`rect|` 那則做完後，敵對驗收抓到 `pos|` / `scale|` / `rot|` **回報「驗證 1 個 OK」但值沒進檔**。
三個重現：variant root 第一次 `rot` 落地、第二次寫不同值靜默不生效；
`pos` 對繼承自巢狀 prefab 的節點兩次不同值都回 OK 但實際始終是 base 值。

### 根因是兩層，兩層都修了

**驗證層（這層才是「靜默」的來源，優先修）**
`VerifyTouch.Capture()` 刻意排在 `SaveAsPrefabAsset` 之後（註解理由是 object reference
要等 save 分配 local file ID 才能快照成可跨 reload 比對的 identity）。但 expected 也在那時才取，
於是：值寫進 in-memory contents → 沒成為 override → save 後 instance 同步回 base 值 →
expected 跟著變成 base 值 → 跟 reload 出來的一致 → **必然通過**。
改法是把 expected 前移到「op 寫入的當下」：
- `VerifyTouch.TransformValue()` 在 factory 裡就用新增的 `TransformSnapshot()` pin 住，
  `Capture()` 的那四個 case 改成 `break` 不再重取。
- `VerifyTouch.Serialized()` 同樣在 factory pin，**但排除 `ObjectReference`**
  （那類仍需 post-save 才有穩定 identity），`Capture()` 只在 `_expected == null` 時才取。
  這一步是必要的：`set|` 對 nested instance root 的 Transform 也是同一種假陽性。
- 型別 unsupported 時刻意不 pin，留給 `Capture()` 產生原本那句錯誤訊息。

**寫入層**
只有 `case active` 呼叫了 `RecordPrefabInstancePropertyModifications`。直接寫 component 的
property（`transform.localPosition` / `rect.anchoredPosition`）**不會**在 prefab instance 上留下
override —— `SerializedObject.ApplyModifiedProperties` 會自己記，直接寫 property 不會。
抽了 `RecordTransformWrite(Transform)`（SetDirty + `IsPartOfPrefabInstance` 才 Record），
接在 `pos` / `scale` / `rot` / `rect` 四處。
`rect|` 之前看似正常是首次寫入的僥倖（既有 modification 條目不會被更新），一併補上。

### 修不掉的：巢狀 prefab instance 節點

`pos|` / `set|` 對「繼承自巢狀 prefab 的節點」（實測 nested instance root 的 Transform）
兩條寫入路徑都寫不進去，`Record` 也沒用，Unity 不報錯。這跟 `active|`／`auto|` 兩則舊待辦
以及 MEMORY 的 `reference_variant_parent_swap` 同源，是 Unity 的巢狀 override 限制。
**沒有硬擋**（同樣的巢狀節點在別的欄位上是寫得進去的，擋了會誤殺），改成 mismatch 訊息
固定附一行指路：「目標若來自巢狀 prefab，外層常產生不出這一筆 override，要改就改在該
nested prefab 本體上」。現在至少不是靜默成功。

### 驗收數字

- variant root：`rot 30,0,45` → `rot 60,0,0` → `rot 0,0,0` 連續三次都落地（修前第二次靜默失敗）
- `set||Transform|m_LocalRotation|0,0,0,1`（Quaternion 歸零，Boss Part 廢料 那個用途）通過
- nested instance root：`pos` 與 `set` 都明確回「0 個 OK，1 個失敗」＋ 巢狀提示（修前回 1 個 OK）
- `rect|` 對 UI 節點連續兩次不同值，四個欄位都是 `override*`
- 非 prefab-instance 的普通 prefab：`pos` + `scale` 2 個 OK，無迴歸
- `active|` / `revert|` / `peek` / `read` 迴歸正常；ucompile ErrorCount=0

### 刻意不做

- `Serialized` 的 `ObjectReference` 仍是 post-save capture。同類假陽性風險已知但沒觸發
  （`ref|` 走 SerializedObject，會正確產生 override），不為它把 identity 快照搬到 op 當下。
- 沒有為巢狀限制加預先 Abort，理由見上。
