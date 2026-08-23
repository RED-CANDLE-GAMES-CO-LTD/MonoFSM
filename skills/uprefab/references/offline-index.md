# 離線索引 CLI（不需要 Unity）

只負責回答「東西在哪個檔案」。內容一律走 Unity 匯出（見 [read.md](read.md)）。

**這條路還沒被取代**：Unity 端沒有全專案搜尋（`refs` 只掃單一 prefab / scene，`types`
只查型別名），跨資產定位就只有 `find`。也不要退回 `grep` prefab YAML —— 慢、抓不到
variant 繼承、而且拿不到節點路徑。

**每次 `find` 之前先確認索引是新的** —— `find` 不會自己更新。改過 prefab / scene 就
`up index`（mtime 增量；實測一天的變更量 234 個資產 2.3 秒）。`(no match)`、或找到的節點
路徑跟現況對不上，第一個嫌疑都是索引過期。

```bash
up index                        # 建立/更新索引（mtime 增量）
up find --comp GrabSlotHolder
up guid 66750e1a364434c63b2d3fd15d471000
up overrides PPlayer.prefab
up scope stats
```

索引在 repo root 的 `.uprefab.db`（已 gitignore，隨時可重建）。
實測 5323 個資產（含三個 120–190MB 的 scene）：全量 25 秒、增量 3 秒、查詢 0.12 秒。

| 指令 | 用途 |
|---|---|
| `index [--rebuild] [-q]` | 預設走 mtime 增量。改了 `indexer.py` 的 schema 要 `--rebuild` |
| `find [--comp X] [--name Y] [--path Z] [-n N] [--resolve] [--by-asset]` | 定位節點，回傳 anchor。條件都是模糊比對。`--resolve` 另外附上可直接餵給 `--node` 的完整路徑（**要 Unity 開著**，見下）。`--by-asset` 只回分佈 |
| `guid <token> [-v] [-n N]` | guid ⇄ 資產路徑互查 |
| `overrides <asset> [-n N] [--all] [--by-target]` | prefab override 稽核。`--by-target` 只回分佈 |
| `scope list \| stats \| init` | `stats` 列出節點數最多的資產，用來決定還要濾掉什麼 |

anchor 格式 `Assets/.../PPlayer.prefab#272130150518276317`，`#` 後是 fileID，對改名穩定。

## 命中很多時先看分佈，不要硬讀

常用的 component 一撈就是幾千筆（`find --comp SetVarBoolAction` 實測 2809 筆），
逐節點列出是幾十萬字元。**先問「集中在哪」，再對那一個下鑽**：

```bash
up find --comp SetVarBoolAction --by-asset -n 10   # 哪個資產各幾筆
up find --comp SetVarBoolAction --path "%PPlayer%" # 再限定範圍逐節點看

up overrides "0_下山逃脫_July_lake.unity" --by-target -n 10   # 改動集中在哪個 instance / 節點
```

被 `-n` 切掉時表尾會講「50 / 共 2809」—— **看到這行就不要把列出的那幾筆當成全部**
（「這個 component 只有這幾處用到」這種結論會整個是錯的）。

## `find --resolve` —— 把命中變成可以直接下鑽的路徑

**`find` 印的那條節點路徑是局部的，不能直接餵給 `--node`。** 理由跟離線索引讀不到 variant
繼承內容一樣（見 [internals.md](internals.md)）：繼承來的父節點在本檔查不到，所以路徑前面
常常缺一整段；同層同名的節點也沒有 `[n]` 索引，照抄會指到第一個。

`--resolve` 是 opt-in（不加就維持純離線行為）。加了會多問 Unity 一次，一趟解完所有命中，
在每筆下面補一行 `--node <完整路徑>`：

```bash
$ up find --comp SwitchCase --path 鍋爐new --resolve
Assets/0_Gameplay/Physics Object/鍋爐/鍋爐new.prefab#9026768881336275425
    [Event] EffectEnterNode/[Action] Switch (FirstMatch)/[Case] SwitchCase  <SwitchCase Transform>
    --node Context/Animator/LogicRoot/LogicOn/[Detector] Trigger/[Dealer] d_Burn/[Event] EffectEnterNode/[Action] Switch (FirstMatch)/[Case] SwitchCase[1]   [own]
```

那條路徑原樣貼給 `up prefab read --node` / `up prefab do` / `up refs --node` 就會通
（scene 的第一段一樣是 root object 名稱，也會標 `[n]`）。

行尾的 `[own]` / `[inherited]` / `[foreign]` / `[by-name]` 是**用哪一層比對解出來的**，
確定性由高到低。`[by-name]` 是最後手段（fileID 對不上但全檔只有一個同名節點），
看到它就順手確認一下讀到的是不是你要的。

解不開時不會猜，會直接說原因 —— 最常見的是**索引過期**（節點被刪或改名了，`up index`
一次就好）與 **scene 沒開著**（anchor 在 .unity 裡時只有那個 scene 開著才解得開）：

```
    ✗ anchor 解不開：合併後的物件圖裡找不到 fileID 1032601356，也沒有叫 'Detect Root' 的節點（節點可能已刪除，或索引過期，先 up index）
    ✗ anchor 解不開：fileID … 在合併後對到多個節點，無法確定是哪一個
```

Unity 沒開時只有 `--resolve` 那半失敗（stderr 一行），離線的部分照印。

## `guid` —— 使用者貼 asset 連結時的第一步

使用者常從 Unity Editor 貼 asset 連結（對改名穩定，比手打中文路徑可靠）：

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

連結裡是 `globalId=`（不是 `asset_guid=`）時它會提醒你轉去 `up obj` —— 那種連結指的是
scene 上的某個節點，見 [read.md](read.md) 的 `obj`。

## `usage` —— 量測「調查一件事花了多少來回」

每次 CLI 呼叫都會在 repo root 的 `.uprefab-usage.jsonl` 追加一行（指令、參數、
output 字元數、耗時、是否落空）。被動蒐集，不影響任何既有行為；
`UPREFAB_NO_USAGE_LOG=1` 可關掉，檔案已 gitignore。

```bash
up usage                    # 統計報告
up usage --gap 600 --top 12 # 調整「間隔多久算新的一段調查」與列出筆數
```

報告直接對應四種調查成本，用數據取代猜測：

| 報告區塊 | 對應的痛點 |
|---|---|
| 落空後立刻換參數重試 N 次 | **猜不到入口關鍵字** —— `find` 的 `--comp` / `--name` 要試好幾輪 |
| 一段調查要幾次呼叫（中位數 / 最長） | **找到後要跳很多次** —— 逐層 `--node` 下鑽的鏈長 |
| 反覆重查：同一目標在多段調查裡被讀 | **重複調查** —— 該把路徑寫進 `alishan-code-map` 的訊號 |
| 各指令總輸出 / 最肥的幾次呼叫 | **回傳量太大** —— 該調 `--budget` / `--fold` 或加新摺疊規則 |

**看到「反覆重查」清單有東西，就是該回寫 code-map 的時候** —— 把那條路徑補進
對應的 `references/*.md`，下次直接跳過整段導航。

## 設定 `.uprefab.json`（repo root）

| 欄位 | 說明 |
|---|---|
| `include` | 完整索引（節點、component、引用邊、override） |
| `includeShallow` | 淺層索引（只有節點名與型別），供 override target 解析用 |
| `exclude` | **降級成 shallow**，不是完全排除 |
| `scriptOnly` | 只索引「自己或後代掛有自家 script」的節點 —— 大 scene 能索引的關鍵 |
| `sceneRootFilter` | 針對特定 scene 指定整棵跳過的 root |

## 中文名稱

Unity 把非 ASCII 的 `m_Name` 逃逸成 `\uXXXX`。索引時已還原，但**直接查 DB 時**
拿到的可能還是 escape 字串，要自己 decode：

```python
re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m.group(1), 16)), name)
```
