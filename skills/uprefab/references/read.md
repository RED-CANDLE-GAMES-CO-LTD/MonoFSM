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

**路徑打錯不會白跑** —— 它會沿路徑走到最後一個通的節點，把那層的子節點連同
`(+N nodes)` 列出來，照著修就好。MonoFSM 的節點名常帶 `[Tag] ` 前綴，很容易猜錯。

## read cache 是 opt-in

預設不讀也不寫 cache，確保拿到 Unity 當下真值。只有同一份已存檔 prefab 會反覆讀、且你
確定 Inspector 沒有未存變動時才加 `--cache`；key 會包含 prefab 依賴與 exporter/tool 版本。
`--no-cache` 保留作相容旗標，語意是完全不讀也不寫；它與 `--cache` 不能同時使用。

## `obj` —— 使用者貼 scene 物件連結時

Editor 除了 asset 連結，還會產「指某個 scene 節點」的連結（`GameObject/生成連結` 選單，
內容是 `GlobalObjectId`）：

```
[[Render] VerletRope](http://localhost:8888/webhook?globalId=GlobalObjectId_V1-2-43f0…9184-4270686736619546228-1351641103)
```

**不要拿它去 `up guid`** —— 那串 32 位 hex 是「物件所在的 scene」的 guid，不是節點。
連結本身也不含節點路徑，所以在 `up obj` 之前，拿到這種連結等於什麼都沒拿到。

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

第二行就是 `up scene ls --node` / `up refs --node` 要的路徑。

**GlobalObjectId 只在物件所在的 scene 開著時解得開**（Unity 的限制）。解不開時它會把 guid
翻成 scene 路徑告訴你要開哪個：

```
# 解不開這個 GlobalObjectId：GlobalObjectId_V1-2-43f0…-4270686737434300027-0
# identifierType=2 → scene object
# 來源資產：Assets/_Recovery/0_下山逃脫_July_lake.unity
# 物件所在的 scene 沒開著。先 up scene open "Assets/…"，或這次加 --open
```

`--open` 會幫你開，但**有未存檔的 scene 時一律拒絕**（換 scene 會丟掉編輯，不猜使用者
想不想留）。若 scene 已經開著卻還是解不開，那就是**連結過期** —— 物件被刪掉、或被打包進
prefab 了（打包後 `targetPrefabId` 會從 `0` 變成 instance 的 id，舊連結的 id 對不上）。
這時請使用者重新產一條。
