---
name: uprefab
description: 不把整個 200MB scene 塞進 context 就能讀懂 Unity serialized data（prefab / scene / ScriptableObject）。當需要：(1) 找某個 component / 節點在哪些 prefab 或 scene 裡 (2) 讀某個 prefab 的階層結構或 FSM 狀態機架構 (3) 看某個子樹的 component 欄位細節 (4) prefab override 稽核 (5) 理解或修改 uprefab 離線索引（MonoFSM/Tools~/uprefab/*.py）與 prefab text cache（PrefabTextCacheMarker / PrefabTextCacheWriter）時使用此 skill。
---

# uprefab

讀 Unity serialized data 的兩套工具。**先決定用哪一套** —— 這是這份 skill 最重要的部分，
選錯會白跑一趟或讀到不完整的資料。

## 決策表

| 你要做什麼 | 用什麼 | 需要 Unity 開著 |
|---|---|---|
| 這個 component / 名稱在哪些檔案裡 | CLI `find` | ❌ |
| 大 prefab 的結構、FSM 狀態機架構（有 cache 檔） | 讀 `Tools/uprefab/cache/**.md` | ❌ |
| 同上但 cache 檔不存在 | `RefreshCacheFor()` 產檔再讀 | ✅ |
| 一般 prefab / 某個子樹的 component 欄位細節 | `ExportSubtree()` | ✅ |
| prefab override 稽核 | CLI `overrides` | ❌ |
| 索引範圍有多大、還能濾掉什麼 | CLI `scope stats` | ❌ |
| **改** prefab 結構（建節點 / 設欄位 / 設引用 / 刪節點） | `PrefabEdit` | ✅ |

一句話版本：**定位走 CLI，大 prefab 走 cache 檔，其他走 ExportSubtree，要改走 PrefabEdit。**

## 為什麼不能只用 CLI 讀內容

離線 YAML 讀不到 variant 繼承來的東西。Unity 只在「本檔有東西引用到」時才寫出 stripped
佔位 document，那些節點的名稱、component、真值**全部只存在 base prefab 裡**。

實際後果：`PPlayer.prefab` 694 個索引節點裡有 259 個 `parent=0`，因為它們的 `m_Father`
指向 stripped Transform（沒有 `m_GameObject` 欄位）。多層 variant 的合成 fileID 更是
任何單一檔案裡都查不到。

所以 CLI 只負責「在哪個檔案」，內容一律讀 cache（那是 Unity 匯出的**合併後**結果）。

---

## 一、離線索引 CLI

```bash
python3 MonoFSM/Tools~/uprefab/uprefab.py index          # 建立/更新索引（mtime 增量）
python3 MonoFSM/Tools~/uprefab/uprefab.py find --comp GrabSlotHolder
python3 MonoFSM/Tools~/uprefab/uprefab.py overrides PPlayer.prefab
python3 MonoFSM/Tools~/uprefab/uprefab.py scope stats
```

索引在 repo root 的 `.uprefab.db`（已 gitignore，隨時可重建）。
實測 5323 個資產（含三個 120–190MB 的 scene）：全量 25 秒、增量 3 秒、查詢 0.12 秒。

| 指令 | 用途 |
|---|---|
| `index [--rebuild] [-q]` | 預設走 mtime 增量。改了 `indexer.py` 的 schema 要 `--rebuild` |
| `find [--comp X] [--name Y] [--path Z] [-n N]` | 定位節點，回傳 anchor。條件都是模糊比對 |
| `overrides <asset> [-n N] [--all]` | prefab override 稽核 |
| `scope list \| stats \| init` | `stats` 列出節點數最多的資產，用來決定還要濾掉什麼 |

anchor 格式 `Assets/.../PPlayer.prefab#272130150518276317`，`#` 後是 fileID，對改名穩定。

### 設定 `.uprefab.json`（repo root）

| 欄位 | 說明 |
|---|---|
| `include` | 完整索引（節點、component、引用邊、override） |
| `includeShallow` | 淺層索引（只有節點名與型別），供 override target 解析用 |
| `exclude` | **降級成 shallow**，不是完全排除 |
| `scriptOnly` | 只索引「自己或後代掛有自家 script」的節點 —— 大 scene 能索引的關鍵 |
| `sceneRootFilter` | 針對特定 scene 指定整棵跳過的 root |

### 中文名稱

Unity 把非 ASCII 的 `m_Name` 逃逸成 `\uXXXX`。索引時已還原，但**直接查 DB 時**
拿到的可能還是 escape 字串，要自己 decode：

```python
re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m.group(1), 16)), name)
```

---

## 二、Prefab Text Cache

掛了 `PrefabTextCacheMarker` 的 prefab，存檔時自動把文字版寫進
`Tools/uprefab/cache/<原 asset path>.md`（**進 git**，所以不開 Unity 也讀得到）。

### 這份 cache 是目錄，不是全文

折疊行的 `(+N nodes)` 就是展開成本：

```
[StateFolder] StateFolder <StateFolder> :: 36 states: init, any, Player Idle, … (+498 nodes)
[VarFolder] VariableFolder <VariableFolder> :: 131 vars: Stamina:VarFloat, … (+233 nodes)
```

看到成本後決定要不要下鑽。檔尾若有 `---` 分隔線，後面是 `FsmTextExporter` 產的
FSM 段（states / transitions / conditions 的 markdown），**讀狀態機架構直接看那段**。

### Marker 欄位

`MonoFSM/1_MonoFSM_Core/Runtime/PrefabCache/PrefabTextCacheMarker.cs`

| 欄位 | 預設 | 說明 |
|---|---|---|
| `_cacheEnabled` | true | 關掉就不寫，但保留設定 |
| `_maxDepth` | 6 | 超過這層的子樹摺成 `Name (+N nodes)` |
| `_excludeVisual` | true | 排除 Renderer / ParticleSystem / AudioSource / Light / Cloth / IK / HighlightEffect（`IsAssignableFrom` 比對，填 base type 即涵蓋子類）。PPlayer 實測省 35% |
| `_foldInactive` | false | inactive 子樹摺一行。**開之前確認** —— MonoFSM 有些邏輯物件本來就是 inactive |
| `_fullExpand` | false | 不摺疊任何已知子樹。開了 `_maxDepth` / `_excludeVisual` 就不生效 |
| `_expandPaths` | [] | 指定要展開的子樹（相對 root），尾端 `/*` 整棵展開 |
| `_exportFsm` | true | 附 FSM markdown 段 |
| `_maxFieldCharsPerComponent` | 0 | 0 = 用 exporter 預設 400 |

存檔掛點是 `IBeforePrefabSaveCallbackReceiver`（Unity 原生 Ctrl+S）+
`ICustomPrefabSaveCallbackReceiver`（專案的 Shift+S 檢查式存檔），兩種都會寫。
內容沒變就不碰檔案 —— cache 進 git，不該產生無意義 diff。

全量重建：menu `MonoFSM/Prefab Text Cache/重建全部`。

### On-demand 精讀（要 Unity 開著）

用 `uloop execute-dynamic-code`：

```csharp
return MonoFSM.Editor.PrefabEditing.PrefabTextCacheWriter.ExportSubtree(
    "Assets/0_Gameplay/0_Base/PPlayer.prefab",
    "CharacterModules/Character FSM/[StateFolder] StateFolder");
```

| 參數 | 預設 | 說明 |
|---|---|---|
| `assetPath` | — | prefab asset path |
| `subPath` | null | 子樹相對 root 的路徑；留空 = 整棵 |
| `depth` | -1 | 往下幾層；-1 不限 |
| `fullExpand` | true | false 時會套用視覺 component 排除 |

**路徑打錯不會白跑** —— 它會沿路徑走到最後一個通的節點，把那層的子節點連同
`(+N nodes)` 列出來，照著修就好。MonoFSM 的節點名常帶 `[Tag] ` 前綴，很容易猜錯。

---

## 三、PrefabEdit —— 寫入（要 Unity 開著）

`MonoFSM.Editor.PrefabEditing.PrefabEdit`，四個原語，跟 `ExportSubtree` **同一套節點路徑語彙**。
用 `uloop execute-dynamic-code` 一行呼叫，不用先查 fileID 或 instanceID。

```csharp
using MonoFSM.Editor.PrefabEditing;
const string P = "Assets/0_Gameplay/FireBurn/FireBurn FSM 起火點.prefab";

// 建節點 + 掛 component
PrefabEdit.AddNode(P, "[StateFolder] StateFolder/[State] burning",
    "[Event] OnStateUpdate", "OnStateUpdateHandler");

// 設欄位（fieldPath 支援巢狀）
PrefabEdit.SetField(P, "...路徑...", "FloatChangePerSecondAction", "_multiplier", -1f);

// 設物件引用；省略最後一個參數就用欄位的宣告型別去目標節點上找 component
PrefabEdit.SetRef(P, "...路徑...", "FloatChangePerSecondAction", "_targetVar",
    "[VarFolder] VariableFolder/[Var] Heat");

PrefabEdit.DeleteNode(P, "...路徑...");
```

| 方法 | 說明 |
|---|---|
| `AddNode(assetPath, parentPath, name, params componentTypes)` | `parentPath` 留空 = 掛在 root 下。同名節點已存在就不動 |
| `SetField(assetPath, nodePath, componentType, fieldPath, value)` | float / int / bool / string / enum（enum 可傳名稱字串） |
| `SetRef(assetPath, nodePath, componentType, fieldPath, targetNodePath, targetComponentType = null)` | 物件引用專用 |
| `DeleteNode(assetPath, nodePath)` | 回傳含子節點數 |

要點：

- **失敗不存檔**。路徑 / 型別 / 欄位名解析不出來就直接 abort，不會留下半殘 prefab。
- **錯誤訊息會給下一步的線索**：路徑錯 → 列出走到哪、那層有哪些子節點；型別重名 → 列出候選 FullName；
  欄位名錯 → 列出該 component 的頂層 serialized 欄位；`SetField` 碰到物件引用 → 叫你改用 `SetRef`。
- **存檔後 cache 自動更新**（走 `PrefabTextCacheWriter.RefreshCacheFor`），所以改完直接讀 cache md 驗證。
- 走 `LoadPrefabContents` / `SaveAsPrefabAsset`，一次呼叫 = 一次 load/save。多步操作就多呼叫幾次。

**為什麼不是 MenuItem**：MenuItem 無法帶參數，而「在 X 下建 Y 型別、把 Z 欄位指向 W」天生就是
參數化操作。做成 static API 才能被 dynamic code 組合。也**不要**用 MonoFSM skill 裡那套 MCP
`instanceID` 流程 —— 那只在 scene 有效，對 prefab asset 不適用。

---

## 已知限制

- **CLI 的階層在 variant 邊界會斷**（見上面「為什麼不能只用 CLI 讀內容」）。
  已有 `pending_parent` 表 + `_resolve_stripped_parents()` 跨檔回推，但只解出 153/2414 ——
  中間層常常只有 stripped Transform、沒有對應的 stripped GameObject，鏈就斷了。
  **不要再往這個方向投資**，要階層就讀 cache。
- **override target 解析率約 66%**（30% 只知道來源資產、2% 完全未解析）。
- 每個 document 最多收 64 條引用邊（`MAX_REFS_PER_DOC`）。
- **cache 只涵蓋掛了 marker 的 prefab**，沒掛的要自己去 Unity 撈。
- **CLI 只讀不寫**（離線索引沒有寫入路徑）。要改 prefab 走 `PrefabEdit`，需要 Unity 開著。

## 模組

```
MonoFSM/Tools~/uprefab/
  uyaml.py     Unity YAML streaming document scanner（不用通用 YAML parser）
  scripts.py   .cs.meta → guid/class/namespace 對照表
  config.py    .uprefab.json 讀取與路徑比對
  indexer.py   SQLite schema 與索引建置
  query.py     find / overrides / scope stats
  uprefab.py   CLI 進入點

MonoFSM/1_MonoFSM_Core/Runtime/PrefabCache/PrefabTextCacheMarker.cs   marker（runtime）
MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/PrefabTextCacheWriter.cs  匯出與寫檔（editor）
MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/PrefabEdit.cs             結構編輯（editor）
Assets/0_Gameplay/Editor/PrefabTextCacheConfig.cs                     專案設定注入
```

工具本體都在 MonoFSM，**專案端只剩 `PrefabTextCacheConfig`**：指定 `CacheRoot`
（= `Tools/uprefab/cache`，對齊離線索引）與專案特有的視覺 component（FMOD
`StudioEventEmitter` / FinalIK `IK` / `HighlightEffect`）。MonoFSM 那邊只放 Unity 內建的。

marker 在 `MonoFSM.Core.Runtime`、writer 在 `MonoFSM.Core.Editor`，runtime 參照不到 editor，
所以走 `[InitializeOnLoadMethod]` 注入兩個 static delegate（`CacheWriter` / `CachePathResolver`）。

實際的文字格式規則（node 行、component 區塊、值格式化、摺疊摘要）見
`monofsm:hierarchy-text-exporter` skill —— 那才是格式的真相來源，這裡不重複。

開發進度與待辦見 `MonoFSM/Tools~/uprefab/PROGRESS.md`。
