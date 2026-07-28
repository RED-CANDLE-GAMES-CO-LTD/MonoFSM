---
name: uprefab
description: 不把整個 200MB scene 塞進 context 就能讀懂並改動 Unity serialized data（prefab / scene / ScriptableObject）。當需要：(1) 找某個 component / 節點在哪些 prefab 或 scene 裡 (2) 讀某個 prefab 的階層結構或 FSM 狀態機架構 (3) 看某個子樹的 component 欄位細節 (4) prefab override 稽核 (5) 用 API 改 prefab / scene 結構、建 prefab variant、複製場景模板、組 FSM (6) 查某個型別有哪些 serialized 欄位、讀 Play Mode 下的 runtime 值、數場上物件驗證生成邏輯 (7) 查某個節點被誰引用 / 它指向誰 (8) 使用者貼了 asset guid 或 Editor webhook 連結（`?asset_guid=…`）需要換成資產路徑 (9) 理解或修改 uprefab 離線索引（MonoFSM/Tools~/uprefab/*.py）、PrefabTextReader 或 PrefabEdit / SceneEdit 時使用此 skill。
---

# uprefab

讀 / 改 Unity serialized data 的工具組。**先決定用哪一條路** —— 這是這份 skill 最重要的
部分，選錯會白跑一趟或讀到不完整的資料。

## 決策表

| 你要做什麼 | 用什麼 | 需要 Unity 開著 |
|---|---|---|
| 這個 component / 名稱在哪些檔案裡 | CLI `find` | ❌ |
| 使用者貼了 asset guid / Editor webhook 連結，要換成資產路徑 | CLI `guid` | ❌ |
| prefab 的階層結構、某個子樹的 component 欄位細節 | CLI `prefab read` | ✅ |
| FSM 狀態機架構 | CLI `prefab read --fsm` | ✅ |
| prefab override 稽核 | CLI `overrides` | ❌ |
| 索引範圍有多大、還能濾掉什麼 | CLI `scope stats` | ❌ |
| **改** prefab 結構 | CLI `prefab do` | ✅ |
| **改** scene 結構、開/複製/存 scene | CLI `scene do` / `scene copy` | ✅ |
| 某個節點被誰指到 / 它指向誰 | CLI `refs` | ✅ |
| 某個型別叫什麼、有哪些欄位 | CLI `types` / `fields` | ✅ |
| Play Mode 下場上有幾個某某物件 | CLI `scene count` | ✅ |
| Play Mode 下某個 component 現在的值 | CLI `peek` | ✅ |

一句話版本：**定位走 CLI `find`，讀結構走 `prefab read` / `scene ls`（預設就會分層摺疊，
再用 `--node` 下鑽），查引用走 `refs`，要改走 `prefab do` / `scene do`。**

所有需要 Unity 的操作都有 CLI 入口 —— **不要直接寫 `uloop execute-dynamic-code`**，
它每次回傳 15 行 JSON envelope（Logs / SecurityLevel / Diagnostics…），CLI 只回結果那一行。

## 為什麼不能只用離線索引讀內容

離線 YAML 讀不到 variant 繼承來的東西。Unity 只在「本檔有東西引用到」時才寫出 stripped
佔位 document，那些節點的名稱、component、真值**全部只存在 base prefab 裡**。

實際後果：`PPlayer.prefab` 694 個索引節點裡有 259 個 `parent=0`，因為它們的 `m_Father`
指向 stripped Transform（沒有 `m_GameObject` 欄位）。多層 variant 的合成 fileID 更是
任何單一檔案裡都查不到。

所以離線索引（`find` / `overrides`）只負責「在哪個檔案」，內容一律走 Unity 匯出的結果
（`prefab read` / `scene ls`）—— 那才是**合併後**的真值。

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
| `guid <token> [-v] [-n N]` | guid ⇄ 資產路徑互查，見下方 |
| `overrides <asset> [-n N] [--all]` | prefab override 稽核 |
| `scope list \| stats \| init` | `stats` 列出節點數最多的資產，用來決定還要濾掉什麼 |

anchor 格式 `Assets/.../PPlayer.prefab#272130150518276317`，`#` 後是 fileID，對改名穩定。

### `guid` —— 使用者貼 guid 連結時的第一步

使用者常會從 Unity Editor 貼 asset 連結（對改名穩定，比手打中文路徑可靠）：

```
[TestKCC Gravity 拔神像](http://localhost:8888/webhook?asset_guid=66750e1a364434c63b2d3fd15d471000)
```

其他指令都吃資產路徑，所以先轉一次：

```bash
up guid 66750e1a364434c63b2d3fd15d471000
# → Assets/1_Prototype/Module Test/TestKCC Gravity 拔神像.unity

up guid "http://localhost:8888/webhook?asset_guid=66750e1a..."   # 整條連結直接貼也行
up guid "TestKCC Gravity 拔神像.unity"                            # 反向：路徑 → guid
```

`token` 有副檔名就當路徑（模糊比對，多筆時每行 `guid  path`），否則從字串裡抽 32 位 hex
當 guid。輸出只有一行路徑，方便直接接給 `prefab read` / `scene open`。

索引裡查不到時（`.cs`、`Packages/` 等索引範圍外的資產）會 fallback 全掃 `.meta`，最壞
約 5 秒 —— 所以 `.cs` 的 guid 也查得到。

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

## 二、讀 prefab —— 分層下鑽（要 Unity 開著）

```bash
up prefab read "Assets/0_Gameplay/0_Base/PPlayer.prefab"          # 先看目錄
up prefab read "Assets/…/PPlayer.prefab" \
    --node "CharacterModules/Character FSM/[StateFolder] StateFolder"   # 再下鑽
up prefab read "Assets/…/X.prefab" --fsm                          # 附狀態機 markdown

# scene 版：--node 留空只列 root 一層（附 (+N nodes) 展開成本）
up scene ls
up scene ls --node "資源生成器 FSM/[StateFolder] StateFolder"
```

| 參數 | 預設 | 說明 |
|---|---|---|
| `--node` | 整棵 / scene 的 root 一層 | 子樹路徑。**scene 的第一段是 root object 名稱** |
| `--budget` | 20000 | 字元上限，超標自動摺到塞得進的那層；`0` = 不限 |
| `--depth` | -1 | 明確指定往下幾層。**給了就不看 `--budget`** |
| `--fsm` | 關 | 附 `FsmTextExporter` 的 states / transitions / conditions markdown |
| `--fold` | 關 | 摺疊已知子樹並排除視覺 component（Renderer / ParticleSystem / IK / HighlightEffect …） |

### 預設就是安全的

不帶參數不會噴一大坨 —— `--budget` 會由淺往深試，取「塞得進預算的最深一層」，
並在檔頭寫下摺在第幾層、下一層要多少字元：

```
# 依 charBudget 20000 摺到第 3 層（下一層會到 57425 字元）。折疊行的 (+N nodes) 是展開成本，
# 要細節用 --node 指定子樹下鑽。
```

實測 PPlayer：全展開 122KB → 預設 17KB。摺疊行帶展開成本，看到數字再決定下鑽哪一支：

```
[StateFolder] StateFolder <StateFolder> :: 36 states: init, any, Player Idle, … (+498 nodes)
[VarFolder] VariableFolder <VariableFolder> :: 131 vars: Stamina:VarFloat, … (+233 nodes)
```

**路徑打錯不會白跑** —— 它會沿路徑走到最後一個通的節點，把那層的子節點連同
`(+N nodes)` 列出來，照著修就好。MonoFSM 的節點名常帶 `[Tag] ` 前綴，很容易猜錯。

### 為什麼沒有落檔 cache 了

原本有一套「掛 `PrefabTextCacheMarker`、存檔時寫 `.md` 到 `Tools/uprefab/cache/`」的機制，
理由是大 prefab 的匯出結果落成檔案可以先 `grep` 再只讀那 60 行，而回傳值一定整份進 context。

**2026-07-28 拆掉了。** 過期成本壓過省下的 context：實測 5 份 cache 有 2 份比來源舊
（差 80～135 秒），而照過期 cache 做的分析會給出「看起來合理但已經不成立」的結論 ——
這種錯最難察覺。加上它要靠人記得掛 marker、記得掃新舊。

`--budget` 分層拿到同樣的省 context 效果（PPlayer 122KB → 17KB），而且讀到的一定是當下真值。

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
| `delcomp\|<node>\|<comp,comp>` | 移除節點上的 component。不存在就跳過（語意是「確保它不在」）。prefab 版 `<node>` 留空 = root |
| `save` | 存 scene（**只有 scene**；prefab batch 結束自動存） |

要點：

- **第一個失敗就停**，並回報「停在第幾行、前面幾個已生效」。後面的操作通常依賴前面的
  結果，硬跑下去只會產生一長串誤導性錯誤。prefab batch 更進一步：**任何一行失敗就整批不存檔**。
- **`add` 重複不算錯**，回「（跳過）已存在」。批次的實際用法是「修一行再整份重跑」。
- **錯誤訊息會給下一步的線索**：路徑錯 → 列出走到哪、那層有哪些子節點；型別打錯 → 列出
  名稱相近的候選；欄位名錯 → 列出可用欄位；**巢狀路徑錯 → 列出走得通的那一層底下有什麼**
  （`_timeMax._constValue` → 「走到 `_timeMax`（VarFloatWrapper），這層底下有 `_tempValue: float`」）。
- **改完直接 `prefab read` 驗證** —— 讀到的一定是當下真值，不必擔心快取同步。

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

### `refs` —— 誰指向這個節點 / 它指向誰

```bash
up refs "Assets/…/Interact Device Trigger.prefab" \
    --node "Modules/Destroyable ModulePack Variant/[VarFolder] VariableFolder/[Var] Durability"
up refs --node "資源生成器 FSM/Timer"           # 省略 asset = 當前開著的 scene
up refs "…prefab" --node "…" --out              # 反向：這個節點指向誰
up refs "…prefab" --node "…" --comp VarFloat    # 只算指向該 component 的（排除同節點其他 component）
```

輸出是「節點路徑 + `型別.欄位`」：

```
14 個引用指向 Modules/Destroyable ModulePack Variant/[VarFolder] VariableFolder/[Var] Durability
  .
      NetworkedVarSyncFloat4._syncFloats.Array.data[0]  → VarFloat
  Modules/Fixable ModulePack/…/=> [Var] Durability.CurrentValue
      VarFloatRef._dropDownRef  → VarFloat
  Modules/FireBurn FSM 起火點/…/[Getter] d_DeviceBroken/[If] [Var] Durability % <= 50%
      VarFloatIsBoundCondition._varFloat  → VarFloat
```

**為什麼走 Unity 而不是離線 `refs` 表**（實測數據，不要再試離線那條）：這個專案大量引用是
prefab override，離線 `refs` 表**只收本檔直接寫出的引用邊**，對 override 型的 0 命中；
override 的目標雖在 `mods` 表裡，卻被格式化成 `→{fileID: …}` 字串塞進 `value` 欄位、
無索引（32 萬筆要 LIKE 全表掃）、且不完整；就算查到也只有裸 fileID，翻成路徑又會撞上
variant 階層斷裂。`SerializedObject` 看到的是**合併後真值**，一趟就回可讀路徑。
實測同一個目標：離線 grep + SQLite 探測數輪只湊出 4 筆，`refs` 一次給出 14 筆。

範圍限「同一顆 prefab / 當前 scene 之內」。跨資產的全庫粗查才是離線索引的活（`up find`）。

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
  **不要再往這個方向投資**，要階層就用 `prefab read`。
- **override target 解析率約 66%**（30% 只知道來源資產、2% 完全未解析）。
- 每個 document 最多收 64 條引用邊（`MAX_REFS_PER_DOC`）。
- **`refs` 只查單一 prefab / 當前 scene 之內**，跨資產的全庫粗查還沒做。
- **離線索引（`index` / `find` / `guid` / `overrides` / `scope`）只讀不寫**。要改就要 Unity 開著
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
  query.py     find / overrides / scope stats / guid ⇄ path
  unity.py     uloop 橋接：只回 Result，Domain Reload 時自己等再重試
  uprefab.py   CLI 進入點

MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/
  PrefabTextReader.cs       prefab 匯出 + charBudget 分層 + --fsm
  EditResolve.cs            路徑 / 型別 / 欄位解析與錯誤訊息（prefab 與 scene 共用）
  EditBatch.cs              一行一操作的 DSL
  PrefabEdit.cs             prefab 寫入 + CreateVariant
  SceneEdit.cs              scene 寫入 + CopyScene + Export + Count
  EditProbe.cs              Types / Fields / Peek
  EditRefs.cs               引用反查（PrefabRefs / SceneRefs）
  AssetRef.cs               asset path → 該塞進 ObjectReference 的物件
Assets/0_Gameplay/Editor/PrefabTextReaderConfig.cs                     專案設定注入
```

`EditResolve` 是刻意共用的：prefab 與 scene 只差在 root 怎麼來（prefab 有唯一 root、
scene 有多個 root object），路徑語彙與**錯誤訊息**不該有兩份 —— 錯誤訊息是修正下一步
的唯一線索。

工具本體都在 MonoFSM，**專案端只剩 `PrefabTextReaderConfig`**：把專案特有的視覺
component（FMOD `StudioEventEmitter` / FinalIK `IK` / `HighlightEffect`）加進
`PrefabTextReader.VisualComponents`。MonoFSM 那邊只放 Unity 內建的。

實際的文字格式規則（node 行、component 區塊、值格式化、摺疊摘要）見
`monofsm:hierarchy-text-exporter` skill —— 那才是格式的真相來源，這裡不重複。

開發進度與待辦見 `MonoFSM/Tools~/uprefab/PROGRESS.md`。
