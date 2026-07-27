---
name: uprefab
description: 不把整個 200MB scene 塞進 context 就能讀懂並改動 Unity serialized data（prefab / scene / ScriptableObject）。當需要：(1) 找某個 component / 節點在哪些 prefab 或 scene 裡 (2) 讀某個 prefab 的階層結構或 FSM 狀態機架構 (3) 看某個子樹的 component 欄位細節 (4) prefab override 稽核 (5) 用 API 改 prefab / scene 結構、建 prefab variant、複製場景模板、組 FSM (6) 查某個型別有哪些 serialized 欄位、讀 Play Mode 下的 runtime 值、數場上物件驗證生成邏輯 (7) 理解或修改 uprefab 離線索引（MonoFSM/Tools~/uprefab/*.py）、prefab text cache 或 PrefabEdit / SceneEdit 時使用此 skill。
---

# uprefab

讀 / 改 Unity serialized data 的工具組。**先決定用哪一條路** —— 這是這份 skill 最重要的
部分，選錯會白跑一趟或讀到不完整的資料。

## 決策表

| 你要做什麼 | 用什麼 | 需要 Unity 開著 |
|---|---|---|
| 這個 component / 名稱在哪些檔案裡 | CLI `find` | ❌ |
| 大 prefab 的結構、FSM 狀態機架構（有 cache 檔） | 讀 `Tools/uprefab/cache/**.md` | ❌ |
| 同上但 cache 檔不存在 | CLI `prefab cache` 產檔再讀 | ✅ |
| 一般 prefab / 某個子樹的 component 欄位細節 | CLI `prefab read` | ✅ |
| prefab override 稽核 | CLI `overrides` | ❌ |
| 索引範圍有多大、還能濾掉什麼 | CLI `scope stats` | ❌ |
| **改** prefab 結構 | CLI `prefab do` | ✅ |
| **改** scene 結構、開/複製/存 scene | CLI `scene do` / `scene copy` | ✅ |
| 某個型別叫什麼、有哪些欄位 | CLI `types` / `fields` | ✅ |
| Play Mode 下場上有幾個某某物件 | CLI `scene count` | ✅ |
| Play Mode 下某個 component 現在的值 | CLI `peek` | ✅ |

一句話版本：**定位走 CLI `find`，大 prefab 走 cache 檔，讀細節走 `prefab read` /
`scene ls`，要改走 `prefab do` / `scene do`。**

所有需要 Unity 的操作都有 CLI 入口 —— **不要直接寫 `uloop execute-dynamic-code`**，
它每次回傳 15 行 JSON envelope（Logs / SecurityLevel / Diagnostics…），CLI 只回結果那一行。

## 為什麼不能只用離線索引讀內容

離線 YAML 讀不到 variant 繼承來的東西。Unity 只在「本檔有東西引用到」時才寫出 stripped
佔位 document，那些節點的名稱、component、真值**全部只存在 base prefab 裡**。

實際後果：`PPlayer.prefab` 694 個索引節點裡有 259 個 `parent=0`，因為它們的 `m_Father`
指向 stripped Transform（沒有 `m_GameObject` 欄位）。多層 variant 的合成 fileID 更是
任何單一檔案裡都查不到。

所以離線索引（`find` / `overrides`）只負責「在哪個檔案」，內容一律走 Unity 匯出的結果
（cache 檔或 `prefab read` / `scene ls`）—— 那才是**合併後**的真值。

---

## 一、離線索引 CLI

本文範例裡的 `up` 是這個 shell function（**zsh 不會對 `$VAR` 做斷詞，所以不要用
`UP="python3 …"` 這種寫法**，會被當成一個檔名而找不到）：

```bash
up() { python3 "MonoFSM/Tools~/uprefab/uprefab.py" "$@"; }
```

```bash
up index                        # 建立/更新索引（mtime 增量）
up find --comp GrabSlotHolder
up overrides PPlayer.prefab
up scope stats
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
`Tools/uprefab/cache/<原 asset path>.md`（**不進 git**，本機產物，隨時可重建）。

### 為什麼要落成檔案（而不是每次 ExportSubtree 直接回傳）

省的不是 Unity 呼叫，是 **context**。`PPlayer.md` 有 71KB ≈ 18k tokens；
`ExportSubtree` 的回傳值會整份進 context，而落成檔案可以先 `grep` 定位、再只讀那 60 行。

臨界點就在這：

- **大 prefab（掛了 marker 的那幾個核心 prefab）** → 走 cache 檔。cache 不在就
  `prefab cache` 產一次，之後都用 grep / 分段 Read。
- **一般 prefab** → 直接 `prefab read`，整棵幾 KB 而已，落檔再讀反而多一趟。
  **不要**為了讀一次就去掛 marker —— 那會寫進 prefab、產生 git diff，代價遠大於收益。
  （沒 marker 的話 `prefab cache` 會直接 return，本來也走不通。）

補產單一 prefab 的 cache：

```bash
up prefab cache "Assets/0_Gameplay/0_Base/PPlayer.prefab"
```

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
內容沒變就不碰檔案 —— 存檔本來就頻繁，不該每次都動 mtime。

全量重建：menu `MonoFSM/Prefab Text Cache/重建全部`。

### On-demand 精讀（要 Unity 開著）

```bash
up prefab read "Assets/0_Gameplay/0_Base/PPlayer.prefab" \
    --node "CharacterModules/Character FSM/[StateFolder] StateFolder"

# scene 版：--node 留空只列 root 一層（附 (+N nodes) 展開成本）
up scene ls
up scene ls --node "資源生成器 FSM/[StateFolder] StateFolder"
```

| 參數 | 預設 | 說明 |
|---|---|---|
| `--node` | 整棵 / scene 的 root 一層 | 子樹路徑。**scene 的第一段是 root object 名稱** |
| `--depth` | -1 | 往下幾層；-1 不限 |
| `--fold` | 關 | 開了會摺疊已知子樹並排除視覺 component（大子樹用） |

**路徑打錯不會白跑** —— 它會沿路徑走到最後一個通的節點，把那層的子節點連同
`(+N nodes)` 列出來，照著修就好。MonoFSM 的節點名常帶 `[Tag] ` 前綴，很容易猜錯。

---

## 三、寫入 —— 批次 DSL（要 Unity 開著）

一行一個操作，欄位用 `|` 分隔。**分隔符不用空白** —— MonoFSM 節點名帶空白與 `[Tag] `
前綴（`[State] Player Idle`），中文名稱也很常見，空白分隔一定炸。

```bash
# prefab：整批共用一次 LoadPrefabContents / SaveAsPrefabAsset
up prefab do "Assets/…/FireBurn FSM 起火點.prefab" -f ops.txt

# scene：對當前開著的 scene；scene 不需要 load/save 配對，最後一行 save 就好
up scene do -f ops.txt
up scene do "add||資源生成器|MonoEntity,MonoObj" "save"    # 也可以直接帶參數
```

| 操作 | 說明 |
|---|---|
| `add\|<parent>\|<name>\|<comp,comp>` | 建節點並掛 component。parent 留空 = prefab root 下 / scene root 層 |
| `prefab\|<prefabPath>\|<parent>\|<name>` | 放 prefab 實例（**只有 scene**） |
| `comp\|<node>\|<comp,comp>` | 對既有節點加 component |
| `set\|<node>\|<comp>\|<field>\|<value>` | 設值。float / int / bool / string / enum（傳名稱）/ Vector3（`"x,y,z"`） |
| `ref\|<node>\|<comp>\|<field>\|<target>[\|<targetComp>]` | 指向另一個節點。targetComp 省略 = 用欄位宣告型別去找 |
| `aref\|<node>\|<comp>\|<field>\|<assetPath>` | 指向 asset（prefab / SO）。prefab 會按欄位型別取 component |
| `pos\|<node>\|x,y,z` | 設 localPosition（**只有 scene**） |
| `mv\|<node>\|<newParent>` | 換 parent（**只有 scene**） |
| `auto\|<node>` | **重跑 `[Auto*]` 綁定 —— 結構改完一定要下這行**，見下面 |
| `del\|<node>` | 刪節點 |
| `save` | 存 scene（**只有 scene**；prefab batch 結束自動存） |

要點：

- **第一個失敗就停**，並回報「停在第幾行、前面幾個已生效」。後面的操作通常依賴前面的
  結果，硬跑下去只會產生一長串誤導性錯誤。prefab batch 更進一步：**任何一行失敗就整批不存檔**。
- **`add` 重複不算錯**，回「（跳過）已存在」。批次的實際用法是「修一行再整份重跑」。
- **錯誤訊息會給下一步的線索**：路徑錯 → 列出走到哪、那層有哪些子節點；型別打錯 → 列出
  名稱相近的候選；欄位名錯 → 列出可用欄位；**巢狀路徑錯 → 列出走得通的那一層底下有什麼**
  （`_timeMax._constValue` → 「走到 `_timeMax`（VarFloatWrapper），這層底下有 `_tempValue: float`」）。
- **prefab 存檔後 cache 自動更新**，改完直接讀 cache md 驗證。

### 結構改完一定要 `auto`

MonoFSM 大量欄位靠 Auto 系列 attribute 填 —— `TransitionBehaviour._conditions` 是
`[AutoChildren]`、Action 的 `_parentObj` 是 `[AutoParent]`。平常是 Inspector 畫到時
順手綁的，用 API 建節點不經過 Inspector，**不補這步會存出一份「看起來對、欄位全是 null」
的資料**，而且只有進 Play Mode 才會發現。

### 建新東西：一律開 variant / 複製模板，不要從零建

專案的 prefab 帶著大量共用底盤（MonoEntity / MonoObj / NetworkObject / Culling /
ModulePack），scene 也需要 WorldUpdateSimulator / SpawnProcessor / PoolManager /
AutoAttributeManager。從零建看起來乾淨，實際會漏，而且漏掉的只在 Play Mode 才炸。

```bash
# FSM 物件：從乾淨的 init/idle 骨架開 variant
up prefab variant "Packages/com.monofsm.fusion/MonoFSM_Fusion/Network FSM.prefab" \
    --out "Assets/…/我的 FSM.prefab" --name "我的 FSM"

# 場景：複製模板（不要用 scene new）
up scene copy --template "Assets/1_Prototype/Module Test/Network FSM Template.unity" \
    "Assets/…/我的測試.unity"
```

---

## 四、查型別 / 查欄位 / 讀 runtime 值

這三個的存在理由都是省 context —— 替代方案是把幾百行 .cs 讀進來，而且讀到的可能是
註解掉的舊欄位。這裡回的是反射看到的真值。

```bash
up types CountDownTimer                    # 名稱含這段的 Component 型別
up fields VarFloatCountDownTimer --own     # 可 serialize 欄位（--own = 不含繼承）
up peek "資源生成器 FSM/Timer" VarFloatCountDownTimer --members "IsTimerUp,Description"
```

`peek` 在 Play Mode 下讀的是**當下的 runtime 值** —— 除「為什麼沒動」最快的一步。
`--members` 留空會 dump 所有 public 屬性（很吵，通常指定幾個就好）。

---

## 五、驗證：數場上的物件

```bash
up scene count --name 測試資源 --sample 4     # 也可以 --comp <型別>
```

```
count=10 activeInHierarchy=10  [PlayMode]  filter: comp=* name=測試資源
  scenes: DontDestroyOnLoad=10
```

Play Mode 下也能用，回的是數字不是整棵 hierarchy。

**`scenes:` 那行不是裝飾。** 借出中的 pool 物件掛在 `DontDestroyOnLoad`，不在 active
scene 底下。數字和預期不符時，第一個要問的是「東西在哪個 scene」而不是「有沒有生成」。

配套的 Play Mode 流程：

```bash
up clear                                   # 清 Console，免得撈到舊的 error
up play play
sleep 8
up scene count --name 測試資源
up logs --type Error -n 4 --stack 4        # 精簡版 Console（原生 get-logs 太肥）
up play stop
```

分段取樣就能驗速率：每 4 秒 +4 顆 = 1 顆/秒，對得上 `_timeMax = 1`。

---

## 六、完整實例：從零組一個「定時生資源」FSM 並驗證

這一整套實測跑過（產物在 `Assets/1_Prototype/uprefab Test/`），照抄即可。
**全程沒有手動開過 Inspector。**

```bash
# 1. 資源 prefab：從既有的可搬礦石開 variant（root 有 PoolObject，SpawnAction 需要）
up prefab variant "Assets/0_Gameplay/Physics Object/[Base] Carriable Mineral (Rock).prefab" \
    --out "Assets/…/測試資源 Rock Variant.prefab" --name "測試資源 Rock"

# 2. FSM prefab：從乾淨的 init/idle 骨架開 variant
up prefab variant "Packages/com.monofsm.fusion/MonoFSM_Fusion/Network FSM.prefab" \
    --out "Assets/…/資源生成器 FSM.prefab" --name "資源生成器 FSM"

# 3. 場景：複製模板
up scene copy --template "Assets/1_Prototype/Module Test/Network FSM Template.unity" \
    "Assets/…/定時生資源 Test.unity"
```

`fsm.ops`（`up prefab do "Assets/…/資源生成器 FSM.prefab" -f fsm.ops`）：

```
add||Timer|VarFloatCountDownTimer
set|Timer|VarFloatCountDownTimer|_timeMax._tempValue|1

add|[StateFolder] StateFolder|[State] spawn|GeneralState

# idle：進入時重置計時器；時間到就去 spawn
add|[StateFolder] StateFolder/[State] idle|[Event] OnStateEnter|OnStateEnterHandler
add|[StateFolder] StateFolder/[State] idle/[Event] OnStateEnter|[Action] Reset Timer|ResetTimerAction
ref|[StateFolder] StateFolder/[State] idle/[Event] OnStateEnter/[Action] Reset Timer|ResetTimerAction|timer|Timer
add|[StateFolder] StateFolder/[State] idle|[Transition] => spawn|TransitionBehaviour
ref|[StateFolder] StateFolder/[State] idle/[Transition] => spawn|TransitionBehaviour|_target|[StateFolder] StateFolder/[State] spawn
add|[StateFolder] StateFolder/[State] idle/[Transition] => spawn|[If] Timer Up|IsTimerUpCondition
ref|[StateFolder] StateFolder/[State] idle/[Transition] => spawn/[If] Timer Up|IsTimerUpCondition|_timer|Timer

# spawn：進入時生一顆，然後回 idle
add|[StateFolder] StateFolder/[State] spawn|[Event] OnStateEnter|OnStateEnterHandler
add|[StateFolder] StateFolder/[State] spawn/[Event] OnStateEnter|[Action] Spawn 資源|SpawnAction
aref|[StateFolder] StateFolder/[State] spawn/[Event] OnStateEnter/[Action] Spawn 資源|SpawnAction|_poolObjFoldOut._constObjValue|Assets/…/測試資源 Rock Variant.prefab
add|[StateFolder] StateFolder/[State] spawn|[Transition] => idle|TransitionBehaviour
ref|[StateFolder] StateFolder/[State] spawn/[Transition] => idle|TransitionBehaviour|_target|[StateFolder] StateFolder/[State] idle

auto|
```

放進場景並驗證：

```bash
up scene do "prefab|Assets/…/資源生成器 FSM.prefab" "pos|資源生成器 FSM|0,3,0" "save"
up scene ls --node "資源生成器 FSM/[StateFolder] StateFolder"   # 確認 _conditions 有被 auto 綁上

up clear && up play play
for t in 4 8 12; do sleep 4; up scene count --name 測試資源 | head -1; done
up play stop
```

實測輸出（每 4 秒 +4 顆 = 1 顆/秒，對得上 `_timeMax = 1`）：

```
count=2  …   count=6  …   count=10
```

沒生成時的除錯順序 —— 這三步各對應一類原因：

1. `up logs --type Error --stack 4` —— 有沒有炸
2. `up peek "…/Timer" VarFloatCountDownTimer --members IsTimerUp,Description` ——
   計時器有沒有在跑（沒跑通常是 `ShouldSimulte` / 沒被 simulator 註冊）
3. `up scene count --name X` 的 `scenes:` 那行 —— 東西是不是生在別的 scene
   （pool 物件在 `DontDestroyOnLoad`）

---

## 已知限制

- **CLI 的階層在 variant 邊界會斷**（見上面「為什麼不能只用 CLI 讀內容」）。
  已有 `pending_parent` 表 + `_resolve_stripped_parents()` 跨檔回推，但只解出 153/2414 ——
  中間層常常只有 stripped Transform、沒有對應的 stripped GameObject，鏈就斷了。
  **不要再往這個方向投資**，要階層就讀 cache。
- **override target 解析率約 66%**（30% 只知道來源資產、2% 完全未解析）。
- 每個 document 最多收 64 條引用邊（`MAX_REFS_PER_DOC`）。
- **cache 只涵蓋掛了 marker 的 prefab**，沒掛的要自己去 Unity 撈。
- **離線索引（`index` / `find` / `overrides` / `scope`）只讀不寫**。要改就要 Unity 開著
  （`prefab do` / `scene do` 都走 uloop）。
- **`fields` 只吃 Component 型別**。ScriptableObject 與巢狀 serializable class 查不到，
  但巢狀欄位打錯時 `set` 的錯誤訊息會列出那一層有什麼，繞得過去。
- **`scene` 系列作用在「當前開著的 active scene」**，不是路徑參數。先 `scene open` / `scene copy`。
- **Play Mode 中不能開 / 建 scene**（會直接 abort，不會半途壞掉）。

## 模組

```
MonoFSM/Tools~/uprefab/
  uyaml.py     Unity YAML streaming document scanner（不用通用 YAML parser）
  scripts.py   .cs.meta → guid/class/namespace 對照表
  config.py    .uprefab.json 讀取與路徑比對
  indexer.py   SQLite schema 與索引建置
  query.py     find / overrides / scope stats
  unity.py     uloop 橋接：只回 Result，Domain Reload 時自己等再重試
  uprefab.py   CLI 進入點

MonoFSM/1_MonoFSM_Core/Runtime/PrefabCache/PrefabTextCacheMarker.cs   marker（runtime）
MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/
  PrefabTextCacheWriter.cs  匯出與寫檔
  EditResolve.cs            路徑 / 型別 / 欄位解析與錯誤訊息（prefab 與 scene 共用）
  EditBatch.cs              一行一操作的 DSL
  PrefabEdit.cs             prefab 寫入 + CreateVariant
  SceneEdit.cs              scene 寫入 + CopyScene + Export + Count
  EditProbe.cs              Types / Fields / Peek
  AssetRef.cs               asset path → 該塞進 ObjectReference 的物件
Assets/0_Gameplay/Editor/PrefabTextCacheConfig.cs                     專案設定注入
```

`EditResolve` 是刻意共用的：prefab 與 scene 只差在 root 怎麼來（prefab 有唯一 root、
scene 有多個 root object），路徑語彙與**錯誤訊息**不該有兩份 —— 錯誤訊息是修正下一步
的唯一線索。

工具本體都在 MonoFSM，**專案端只剩 `PrefabTextCacheConfig`**：指定 `CacheRoot`
（= `Tools/uprefab/cache`，對齊離線索引）與專案特有的視覺 component（FMOD
`StudioEventEmitter` / FinalIK `IK` / `HighlightEffect`）。MonoFSM 那邊只放 Unity 內建的。

marker 在 `MonoFSM.Core.Runtime`、writer 在 `MonoFSM.Core.Editor`，runtime 參照不到 editor，
所以走 `[InitializeOnLoadMethod]` 注入兩個 static delegate（`CacheWriter` / `CachePathResolver`）。

實際的文字格式規則（node 行、component 區塊、值格式化、摺疊摘要）見
`monofsm:hierarchy-text-exporter` skill —— 那才是格式的真相來源，這裡不重複。

開發進度與待辦見 `MonoFSM/Tools~/uprefab/PROGRESS.md`。
