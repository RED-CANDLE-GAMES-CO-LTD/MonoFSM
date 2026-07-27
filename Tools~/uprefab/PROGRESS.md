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
| Glob | `find --comp X --name Y` | 索引 | ✅ Phase 1 |
| Grep | `grep`（欄位值搜尋） | 索引 | ⬜ 未做 |
| Read(offset,limit) | `read <anchor> --depth N` | Unity | ⬜ Phase 3 |
| （無對應） | `refs <anchor>` 反查引用 | 索引 | ⬜ Phase 2 |

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
| `find` 查詢 | 0.12 秒 |
| DB | 206MB（已 gitignore） |

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
不必等 Phase 3 的 Unity 精讀。

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

### Phase 2 —— `refs` 全庫反查

索引裡已經有 `refs` 表（41k 筆），只差指令與輸出。

**關鍵：不要重做 `ComponentReferenceWindow`。**
（`MonoFSM/1_MonoFSM_Core/Editor/ReferenceSystem/ComponentReferenceScanner.cs`）

它已經處理了 `AbstractVarWrapper` / `ValueProvider` 這類**間接引用**
（`ComponentReferenceScanner.cs:64-80`），那是純 YAML parse 抓不到的語意；
但它限定 `ScanFromRoot` 單一子樹、要 Unity 開著。

正確分工：
- **CLI `refs`** → 全庫粗查，找到「在哪個 prefab」
- **ComponentReferenceWindow** → 該 prefab 內精查（含間接引用）
- CLI 輸出最後一行寫「→ 建議在 X.prefab 開 ComponentReferenceWindow 查 Y」

### Phase 3 —— `read` / `fsm`（真正的「跳著讀」）

**這是原始需求還沒滿足的最後一哩。** 建議優先於 Phase 2。

要對既有 exporter 加兩個 API（格式不動）：

- `ExportAt(assetPath, fileID, depth)` —— 從中間節點當 root 展開
- `charBudget` —— 超標自動加深折疊並回報 `(+Nk chars omitted)`

折疊行要加成本標記，這樣才知道下一步要不要展開：

```
States <StateFolder> :: 5 states… (+42 nodes ~3.1k)
```

`fsm` 指令轉呼叫既有的 `FsmTextExporter`（243 行，markdown 輸出）。

### Phase 4 —— 寫入：**已完成**，但不在 CLI 裡

原本規劃是 CLI `set --dry-run`，結論是**寫入不該進 CLI**：離線 YAML 寫入沒辦法保證 prefab
override 語意與序列化正確性（這也是「不要碰 YAML」原本的理由）。改成走 Unity 側的
`MonoFSM.Editor.PrefabEditing.PrefabEdit`，由 `uloop execute-dynamic-code` 一行呼叫。

四個原語 `AddNode` / `SetField` / `SetRef` / `DeleteNode`，吃**節點路徑**（跟
`ExportSubtree` 同一套語彙），不需要先查 anchor 或 fileID —— 原本設想的
「anchor → 人工編輯指示」這一步因此整個省掉了。

實作要點：`LoadPrefabContents` + `SerializedObject` + `SaveAsPrefabAsset`，不碰 YAML；
路徑 / 型別 / 欄位解析失敗就 abort 不存檔；錯誤訊息帶下一步線索（列出該層子節點、
候選 FullName、可用欄位名）。存檔後主動呼叫 `PrefabTextCacheWriter.RefreshCacheFor`，
因為 `LoadPrefabContents` 不會觸發 `IBeforePrefabSaveCallbackReceiver`。

用法見 `MonoFSM/skills/uprefab/SKILL.md` 的「三、PrefabEdit」。

**同時把 PrefabTextCache 搬進 MonoFSM**：marker → `MonoFSM.Core.Runtime`、
writer → `MonoFSM.Core.Editor`，專案端只留 `PrefabTextCacheConfig`（`CacheRoot` 與
專案特有的視覺 component 清單）。搬檔時 `.meta` 一起移動，GUID 不變，3 個掛了 marker
的 prefab reference 沒掉。

---

## 檔案清單

| 路徑 | 狀態 |
|---|---|
| `MonoFSM/Tools~/uprefab/*.py`（6 檔） | 新增 |
| `MonoFSM/Tools~/uprefab/README.md` | 新增 |
| `MonoFSM/Tools~/uprefab/PROGRESS.md` | 本檔 |
| `.uprefab.json` | 新增（repo root，設定檔） |
| `.gitignore` | 修改（加 `/.uprefab.db`） |
| `MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/PrefabEdit.cs` | 新增（寫入四原語） |
| `MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/PrefabTextCacheWriter.cs` | 從 `Assets/0_Gameplay/Editor/` 搬入 |
| `MonoFSM/1_MonoFSM_Core/Runtime/PrefabCache/PrefabTextCacheMarker.cs` | 從 `Assets/0_Gameplay/Tools/` 搬入（GUID 保留） |
| `Assets/0_Gameplay/Editor/PrefabTextCacheConfig.cs` | 新增（專案端設定注入） |

尚未 commit。MonoFSM 是 submodule，那三個檔要在 submodule 裡另外 commit。
