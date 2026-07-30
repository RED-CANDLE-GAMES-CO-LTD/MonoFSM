# 讀 prefab / scene —— 分層下鑽（要 Unity 開著）

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

同名節點的 `[n]` 後綴語法與 `do` 共用，見 [edit.md](edit.md)。

## 預設就是安全的

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
它自己用 regex 撈。匯出參數（`--node` / `--depth` / `--budget` / `--fold` / `--fsm`）
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
