# 讀 prefab / scene —— 分層下鑽（要 Unity 開著）

```bash
up prefab read "Assets/0_Gameplay/0_Base/PPlayer.prefab"          # 先看目錄
up prefab read "Assets/…/PPlayer.prefab" \
    --node "CharacterModules/Character FSM/[StateFolder] StateFolder"   # 再下鑽
up prefab read "Assets/…/X.prefab" --fsm                          # 附狀態機 markdown
up prefab read "Assets/…/X.prefab" --fsm-only                     # 只要狀態機，不重印 hierarchy
up prefab read "Assets/…/X.prefab" --structure-only               # 只要結構 / component 名

# scene 版：--node 留空只列 root 一層（附 (+N nodes) 展開成本）
up scene ls
up scene ls --node "資源生成器 FSM/[StateFolder] StateFolder" --depth 2 --budget 8000
```

| 參數 | 預設 | 說明 | `prefab read` / `obj` | `scene ls` |
|---|---|---|---|---|
| `--node` | 整棵 / scene 的 root 一層 | 子樹路徑。**scene 的第一段是 root object 名稱** | ✅ | ✅ |
| `--depth` | -1 | 最多往下幾層；**仍受 budget hard cap** | ✅ | ✅ |
| `--full` | 關 | 不摺疊已知子樹、保留視覺 component（Renderer / ParticleSystem / IK / HighlightEffect …）與完整欄位。**預設就是省 token 的摺疊模式**，只在摘要不夠時才加 | ✅ | ✅ |
| `--budget` | 20000 | hierarchy + FSM 的總字元 hard cap；`0` = 明確允許不限 | ✅ | ✅ |
| `--fsm` | 關 | 附 `FsmTextExporter` 的 states / transitions / conditions markdown | ✅ | ❌ |
| `--fsm-only` | 關 | 只輸出 FSM；仍受 budget | ✅ | ❌ |
| `--structure-only` | 關 | 只列結構與 component 名，不列 serialized 欄位 | ✅ | ✅ |

**你用 `--node` 點名的那個節點永不摺疊，只摺它的後代。**
如果摺疊摘要行（`:: …` / `(+N nodes)`）出現在你點名的節點自己身上 = 工具壞了，回報。

`scene ls` 現在與 prefab / obj 共用 hard budget。下鑽大子樹仍建議先看 root 的 `(+N nodes)`，
再用 `--node` + `--depth`；若真的要完整輸出才明確給 `--budget 0`。scene 上的 FSM markdown
仍走 `up obj --fsm` / `--fsm-only`。

同名節點的 `[n]` 後綴語法與 `do` 共用，見 [edit.md](edit.md)。

## `prefab read` / `obj` 預設就是安全的

不帶參數不會噴一大坨 —— `--budget` 是 hierarchy、header 與 FSM 合計的 hard cap。
它會先取塞得進預算的最深一層；連第一層都超標時改回 compact 摘要，最後才用帶續讀指引的
截斷守住上限。明確 `--depth` 也不能繞過；只有 `--budget 0` 代表使用者接受無上限。

```
# 依 charBudget 20000 摺到第 3 層（下一層會到 57425 字元）。折疊行的 (+N nodes) 是展開成本，
# 要細節用 --node 指定子樹下鑽。
```

實測 PPlayer：全展開 122KB → 預設 17KB。摺疊行帶展開成本，看到數字再決定下鑽哪一支：

```
[StateFolder] StateFolder <StateFolder> :: 36 states: init, any, Player Idle, … (+498 nodes)
[VarFolder] VariableFolder <VariableFolder> :: 131 vars: Stamina:VarFloat, … (+233 nodes)
```

**layer 會印在節點行上**：不是 Default 的印 `layer=<名字>`；`TriggerDetectorSource` 不在 `Detector` layer
會印 `layer=Default ⚠應在Detector`，而且 header 會列出**整棵子樹**（含摺疊起來的）所有違規節點的修正指令：

```
# ⚠ layer 慣例：1 顆 TriggerDetectorSource 不在 Detector layer（collision matrix 照 Detector 配，放錯可能靜默打不到）。修正：
#   up prefab do "Assets/…/PPlayer.prefab" "layer|…/[Detector] Melee/[DetectionSource] TriggerDetectorSource|Detector"
```

`prefab peek` / `prefab locate` / 只列 component 名的 peek 也會在節點後面帶同一個 layer 標記。

**路徑打錯不會白跑** —— 它會沿路徑走到最後一個通的節點，把那層的子節點連同
`(+N nodes)` 列出來，照著修就好。MonoFSM 的節點名常帶 `[Tag] ` 前綴，很容易猜錯。

## read 沒有快取；常查的 prefab 要進 skill

read **沒有磁碟快取**（2026-09-11 移除；`--cache` / `--no-cache` 留成 no-op）。實測 393 次讀取有
349 組不同參數，精確 key 幾乎不會重複命中，而且命中與否吐回 context 的字元一樣多。
取而代之的是 read 結尾的 `# [hot] …` 提示與 `up usage hot`：近 7 天被 ≥3 段調查碰過、但
skill 沒提到（或提到了 prefab 但常查子樹沒入口）的 prefab 會被點名，該把入口寫進 skill。

另外有一層 60 秒的 argv memo（跨子指令）：同一條指令原封不動重打會直接回上次結果，
期間跑過任何寫入類指令就整批失效。要繞過用全域 `--no-memo`。

## `obj` —— 使用者貼 scene 物件連結時

Editor 除了 asset 連結，還會產「指某個 scene 節點」的連結（`GameObject/生成連結` 選單，
內容是 `GlobalObjectId`）：

```
[[Render] VerletRope](http://localhost:8888/webhook?globalId=GlobalObjectId_V1-2-43f0…9184-4270686736619546228-1351641103)
```

兩種連結別搞混：

| 連結 | 指的是 | 用 |
|---|---|---|
| `?asset_guid=<32hex>` | 一個 asset | `up guid` |
| `?globalId=GlobalObjectId_V1-2-<sceneGuid>-<objId>-<prefabId>` | scene 上的**某個節點** | `up obj`（別名 `up gid`） |

**不要拿 `globalId` 去 `up guid`** —— 那串 32 位 hex 是「物件所在的 scene」的 guid，不是節點，
查出來只會得到 scene 路徑，答不出使用者問的那個物件。連結本身也不含節點路徑，
所以在 `up obj` 之前，拿到這種連結等於什麼都沒拿到。

```bash
up obj "[名稱](http://localhost:8888/webhook?globalId=GlobalObjectId_V1-2-…)"  # 匯出它的子樹
up obj "<連結>" --locate          # 只要節點路徑 + component 清單
up obj "<連結>" --node "Context/Animator" --fsm    # 再往下鑽 / 附 FSM
up obj "<連結>" --select          # 順便在 Unity 裡選中並 ping（給人看）
up obj - < link.txt               # 從 stdin 讀
```

markdown 連結、裸 URL、只有 `GlobalObjectId_V1-…` 本身都吃 —— 整段貼進去就好，
它自己用 regex 撈。匯出參數（`--node` / `--depth` / `--budget` / `--full` / `--fsm`）
與 `prefab read` 同一套，因為背後是同一個 renderer。

`--locate` 的輸出可以直接餵給其他指令：

```
# owner: scene Assets/_Recovery/0_下山逃脫_July_lake.unity
GameObject_2/燈泡開關組_1/safe light bulb 燈泡/…/[Render] VerletRope
  <Transform VerletRope MyOverlap LineRenderer>
  (+0 nodes)
```

第二行就是 `up scene ls --node` / `up refs --node` 要的路徑，最後一行直接給你可貼的下一條指令
（scene 與 prefab 兩側的 `--node` 語意不同 —— scene 含 root object 名、prefab 不含，
所以那一行才存在）。

**連結指的物件也可能在 prefab 裡**（Editor 開著 Prefab Stage 時產的連結就是這種）。
**不需要開 Prefab Stage**：EditGid 對 `.prefab` 直接載 imported asset、用 local fileID 比對
（stage 開著時改比對 `GetGlobalObjectIdSlow`，nested instance 內的也精確命中），
所以 `up obj "<連結>"` **一個呼叫就拿到節點路徑 + 欄位內容**，不要先 `--locate` 再 `prefab read`。
輸出 `# owner: prefab <路徑>`；`--open` 只是順便把 stage 打開給人看，不是解析前提。

Unity 整個沒開時才退到離線索引（只能定位、不能給欄位；`targetPrefabId != 0` 或路徑接不回
root 時會自己講並改建議 `up find`）。

**連結標籤本身就帶完整路徑**（BugReportUtility 產的：`PPlayer / [Switch Simulate] Switch (FirstMatch)/[Case] SwitchCase/…`，
prefab 側不含 root、scene 側含 root，格式對齊 `--node`），所以光看貼上來的文字就知道節點在哪，
`up obj` 那一次呼叫是拿內容，不是拿位置。

`--open` 對 scene 與 prefab 都有效，但**有未存檔的 scene / Prefab Stage 時一律拒絕**
（換掉會丟掉編輯，不猜使用者想不想留）。若容器已經開著卻還是解不開，那就是**連結過期**
—— 物件被刪掉、或被打包進 prefab 了（打包後 `targetPrefabId` 會從 `0` 變成 instance 的 id，
舊連結的 id 對不上）。這時請使用者重新產一條。

**連結只有 `up obj` 吃。** 貼給 `up peek` / `up refs` / `up overrides` / `up find` 會被
攔下來並告訴你該打什麼（以前 `up overrides` 會靜靜回一句 `(no overrides)`，看起來像結論）。
`up guid` 是例外 —— 它只取連結裡那個資產 guid。

`up obj --locate` 印的完整路徑含 root object 名（`PPlayer/CharacterModules/…`），
貼回 `up prefab read --node` 也不會錯：第一段等於 prefab root 名時會自動切掉。
