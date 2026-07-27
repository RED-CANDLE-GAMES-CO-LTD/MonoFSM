# uprefab 開發進度

> 工具本身的使用說明在 [README.md](README.md)。這份是**開發進度與待辦**，用來接續工作。

## Resume

```bash
claude --resume e978684e-7697-4a61-b6b8-a018fe03c42e
```

- 最後更新：2026-07-27
- 分支：`develop`
- 狀態：**Phase 1 完成並實測通過**，Phase 2 / 3 未開始

---

## 這個工具要解決什麼

專案規模變大後，大量 gameplay 資料放在 Unity serialize data（scene / prefab）上，
但 LLM 不擅長直接讀這些檔案 —— 主場景 `0_下山逃脫_July.unity` 是 **182MB**，
根本塞不進 context。

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

### 實測數字（5323 個資產，含三個 120–190MB 的 scene）

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
