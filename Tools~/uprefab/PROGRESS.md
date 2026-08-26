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
