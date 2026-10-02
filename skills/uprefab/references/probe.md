# 查型別 / 查欄位 / 讀 runtime 值 / 引用反查 / 數物件

## `types` / `fields` / `peek`

這三個的存在理由都是省 context —— 替代方案是把幾百行 .cs 讀進來，而且讀到的可能是
註解掉的舊欄位。這裡回的是反射看到的真值。

```bash
up types CountDownTimer                    # 名稱含這段的 Component 型別（+ 離線補的 [Serializable] plain class / struct）
up fields VarFloatCountDownTimer --own     # 可 serialize 欄位（--own = 不含繼承）
up fields ConditionGroup                   # 非 component 的 [Serializable] 型別：離線從原始碼抽欄位
up peek "資源生成器 FSM/Timer" VarFloatCountDownTimer --members "IsTimerUp,Description"
```

**`up types` 有一段「[Serializable] class / struct —— 非 component」**：那些是別的 component
序列化欄位的型別（例 `AbstractConditionBehaviour.cs` 裡的 `ConditionGroup`），不能 add 成 component，
但也**不是不存在** —— 查到這段就別另外造一顆同功能的 component。

`peek` 在 Play Mode 下讀的是**當下的 runtime 值** —— 除「為什麼沒動」最快的一步。
不寫 `--members`（或只寫 `--members` 不帶值）= serialize 欄位 + 可查的屬性名清單；要屬性值就 `--members` 點名。
**不知道該給哪顆 component 時把 comp / `--comp` 留空** —— 會列出該節點上掛了哪些
component 的名稱（只取型別名，不呼叫任何 property getter）。

**路徑第一段是 prefab 名時**（`up peek "PPlayer/CharacterModules/…"`）不會再只回
「scene 的 root 有（26 個）…」害你以為節點不存在 —— 它會認出那是 prefab，
直接把 asset 路徑與該用的 `up prefab peek …` 指令印出來。prefab 不用開 stage 就讀得到。

**node 貼成資產路徑也行**：`up find` 第一行的 anchor（`Assets/….unity#<fileID>`）可以直接當 node，會叫 Unity 解成真路徑再 peek，並印「下次直接打：up peek "<節點路徑>" …」。目標 scene 不是 Editor 開著的那個時**不會讀、也不會自己切 scene**（有 dirty scene 時 `up scene open` 會拒絕，要使用者決定存不存），只印開著的 / 目標各是哪個；anchor 是 prefab 的話印對應的 `up prefab peek … --node … --comp …`。

**object reference 會印 asset 路徑**：指向別的 asset 的引用印成 `v_IsDead <VariableTag> @Packages/com.monofsm.pro/…/v_IsDead.asset`，`@` 後面可以直接貼給 `aref`（內建資源印 `@builtin:Cube`）；同一支 prefab 內部互指、scene 物件不印。ScriptableObject（.asset）的值用 `up asset peek`，`up peek <x.asset>` 也會自動轉過去。

### 巢狀 `[Serializable]` 類別：點路徑與 `--deep`

`IgnoreColliderFilter` / `TargetPositionResolver` 這種純資料類別直接印只會得到型別名
（等於什麼都沒查到）。兩種讀法：

```bash
# 1. 點路徑：只要那一格，輸出最小。可帶 [n] 走陣列/List 元素
up prefab peek "Assets/…/PPlayer.prefab" --node "…/[Raycast] RaycastCache[4]" \
    --comp RaycastCache --members "_ignoreFilter._ignoreSelfEntity,_ignoreFilter._selfEntities[0]"

# 2. --deep：把巢狀類別整個攤開（不帶數字 = 2 層），先探勘用
up prefab peek "Assets/…/PPlayer.prefab" --node "…" --comp RaycastCache \
    --members _ignoreFilter --deep
#   _ignoreFilter = {_ignoreEntities=[], _ignoreSelfEntity=True, _selfEntities=[PPlayer <MonoEntity>]}
```

`--deep` 也吃 `prefab peek-batch` 與 `prefab locate --members`。規則：

- 預設 `--deep 0`（維持舊的淺層輸出），不帶數字 = 2 層，超過就只印型別名
- 陣列 / List 的元素也會攤開，但最多列 6 個，其餘印 `… 還有 N 個未列出`
- **攤開時只走 serialize 欄位，不呼叫任何 property getter**（盲掃 getter 會讓 Editor
  在 native 層閃退）。要看屬性就顯式寫進 `--members`，一次一兩個
- 點路徑上的每一段都是顯式點名的，所以中間段允許是 property；`*`（override 標記）
  只對不含點的直接欄位標，巢狀段不標（override 記的是 top-level property path）
- **reference 欄位（`UnityEngine.Object`）只印 `名字 <型別>`，`--deep` 也不會展開它**（引用鏈會
  爬到整個場景）。要看被引用那顆的內容，用點路徑穿過去：`--members "_varFloat.CurrentValue"`、
  `"_target._defaultValue"`。Inspector 右鍵的兩個 `Dump … → 剪貼簿` 則會在 reference 後面
  接 `= ValueInfo`（Var 的 CurrentValue / Condition 的 FinalResult / Getter 的取值），
  九成情境要的就是那一個值；CLI 不這樣做是因為 ValueInfo 是 getter

## `prefab peek` —— 只讀 prefab 上一顆 component 的欄位

```bash
up prefab peek "Assets/…/資源生成器 FSM.prefab" \
    --node "[StateFolder] StateFolder/[State] idle/[Transition] => spawn" \
    --comp TransitionBehaviour --members _target,_conditions
```

**「那條 ref 到底接上了沒」不要用 `prefab read`。** `read` 的最小單位是一整顆子樹的摺疊
輸出（實測平均 6.4KB），同一個問題走 `prefab peek` 是一百多字元。不進 Play Mode、
不看場上實例，讀的是 asset 上的值。

`--members` 留空 = 列出這顆 component 的**所有 serialize 欄位**（不是 public 屬性 ——
asset 上沒跑過任何 runtime 邏輯，屬性大半是空的或會炸）。`--node` 留空 = prefab root。

### `*` = 這顆 prefab 自己 override 的欄位

`peek` / `peek-batch` / `locate --members` 的輸出裡，欄位名後面有 `*` 表示這個值是
**這顆 prefab 自己 override 的**，沒有 `*` 就是繼承自 base / nested prefab；表頭會多一行
講來源檔名（nested 的話連 instance root 節點一起講）。判準與 hierarchy 匯出共用
（`PrefabOverrideMark`），`isDefaultOverride` 已排除，所以 `m_Name` / instance root 的
`m_LocalPosition` 這類 Unity 強制欄位不會滿版星號。

它的用途是「改完 prefab 之後一眼確認寫進去了沒」—— 合併後的值看不出是繼承還是 override，
過去要靠 `overrides`（實測平均 21KB）才問得到。`prefab do` 的寫後驗證也會多印一段
`# override 狀態（存檔後重讀）`，其中「繼承（這顆沒留下 override）」＝ 你剛改的值沒留下來，
或剛好等於 base 值。

## `prefab locate` —— 在合併後 prefab 裡直接找節點

已經知道要查哪份 prefab、但不知道 component 在 variant 合併後落在哪時，不要用 `read` 一層層猜：

```bash
up prefab locate "Assets/…/X Variant.prefab" --comp TransitionBehaviour -n 20
up prefab locate "Assets/…/X Variant.prefab" --name "Durability" \
    --comp VarFloat --members CurrentValue,_defaultValue
up prefab locate "Assets/…/X Variant.prefab" --comp VarFloat --members   # 不帶值 = 每個命中都印全部 serialize 欄位
```

**locate 不寫 `--members` 只列路徑、不印值**（跟 peek 不一樣）；只寫 `--members` 不帶值 = 全部欄位，
點名格式跟 peek 一樣。

它在 Unity 端一次遍歷合併後真值，回 canonical escaped node path、component 與可選欄位；
表尾會給總命中與截斷提示。這是「單一已知 prefab 內定位」；跨資產仍先走離線 `find`。

## `prefab peek-batch` —— 一次查多顆欄位

逐行格式 `node|component|members`；members 是逗號分隔，可留空表示所有 serialized 欄位：

```text
[StateFolder] StateFolder/[State] idle/[Transition] => spawn|TransitionBehaviour|_target,_conditions
Timer|VarFloatCountDownTimer|_timeMax,IsTimerUp
```

```bash
up prefab peek-batch "Assets/…/X.prefab" -f probes.txt
up prefab peek-batch "Assets/…/X.prefab" -f - < probes.txt
```

整份清單只做一次 Unity call / prefab load。單筆失敗會就地回報，但不會吞掉其它 probe。

## `poke` —— Play Mode 下設一個 Var 的值（peek 的寫入面）

```bash
up poke "訂購終端機/[VarFolder] VariableFolder/[Var] Nav Right" VarBool true
up poke "…/[Var] Global: d_TeamStatus.d_Money" VarFloat 100
```

自動測試用的「手動撥一下」。要驗「按了左鍵游標會不會動」「錢夠了買不買得成」，
得先能給錢、能把按鍵旗標撥起來 —— 真的去驅動玩家角色互動成本高得多。

走 `AbstractMonoVariable.SetValue(值, byWho, reason)`，那是專案設值的正門，會過 modifier、
觸發 valueChangedHandler，跟遊戲裡真的被改是同一條路。回傳 `Value: 舊 -> 新`。
只在 Play Mode 有意義（EditMode 會擋掉，叫你改用 `prefab do` / `scene do`）。

**別連續快速呼叫** —— 每個 `up` 都要等 Unity 回應，一行 shell 塞五六個 peek/poke
會有幾個靜默回空字串。看到空輸出先單獨重跑那一個，通常就有值了。

## `hit` —— Play Mode 下對 receiver 打一發 effect（不用玩家走過去）

```bash
up hit "發財車 FaCai Truck/…/[Receiver] Interact"            # dealer 自動找本機玩家同 effectType 的
up hit "…/某 entity 根" --effect Interact                     # 給祖先時用 effectType 名篩
up hit "…/[Receiver] Interact" --dealer "FusionFPS Core/Player1 [Local]/…/[Dealer] Interact"
```

- 走 `receiver.ForceDirectEffectHit(dealer, null)`，跟 `PlayerInteractState` 同一條：`CanHitReceiver` → receiver `IsValid`（含 `d_CanInteract` 這類 `[If]`）→ EnterNode。**跳過的只有**玩家端的選取（Interact Range Detect / CurrentInteractable）跟 PlayerInteractState 的 CanEnter。
- Unity 端不直接呼叫，排進 `TickActionQueue`，下一個非 resim tick 在 `WorldUpdateSimulator.Simulate` 裡跑（tick 外寫 networked Var 會被 resim 蓋掉）；CLI 輪詢結果最多 `--wait` 秒（預設 2），印執行 tick、`canHit`、dealer 的 `FailReason`。`canHit=False` exit 1；逾時 = simulator 沒在跑。
- receiver / dealer 不是剛好一顆 active 就 abort，把候選路徑 + effectType 列出來。
- **目標 MonoObj 沒在模擬時打到會「有 HitEnter、沒效果」**：EventHandler 遇到 `IsCulling` / `!ShouldSimulate` 會靜默跳過 EnterNode 的 action（開場 CullingEventTarget 還沒判定 near 時就是這樣）。`hit` 執行前會檢查 `activeInHierarchy` / `IsActiveInSimulator` / `IsSimulationCulling` / `ShouldSimulate`，沒過就延到下一個 tick，最多 `--max-defer N`（預設 120），CLI 印 `deferred=k（原因）`，逾時印 `TIMEOUT` exit 1。所以剛 `up play play` 就打也沒關係，不用先等 `init -> idle`。
- 要看結果時間軸：先 `up menu "Tools/MonoFSM/FSM Trace/Enabled"`，打完 `up menu "Tools/MonoFSM/FSM Trace/Dump All"` → `up fsm-trace --entity <關鍵字>`（HitEnter / HitBlocked / VarChange）。

## `debug-effect-trace` —— EffectReceiver 為什麼沒觸發

```bash
up debug-effect-trace "Zone Arrive Trigger 找到火車 Variant"          # 節點或它的任一祖先都行
up debug-effect-trace "…/Detectable Root" --effect "Zone Arrive"      # 同節點多個 receiver 時篩
```

**這條鏈有六段，每一段都可能靜靜地 return**（detector 偵測 → detectable dict 登記 →
dealer 有效 → receiver 配對 → enterNode 的四道 gate → action），逐段 `peek` 要十幾次來回。
一次呼叫把每段的真值攤開，並在該段後面標 `←` 指出問題：

```
receiver …/[Receiver] d_Zone Arrive 區域抵達
  effectType=… IsValid=True HasDealerOverlap=True enterNode=[Event] EffectEnterNode
  detectable=Detectable Root IsValid=True registered=YES
    detectTargets=1 debugDetectors=[[Detector] In Melee Range 附近]
  overlapping dealers: 1
    …/[Dealer] d_Zone Arrive 區域抵達
      IsValid=True fail='Check' detector=[Detector] In Melee Range 附近 valueInfo=valid:True,objs:1
  enterNode [Event] EffectEnterNode
    lastSimulateEventTime=-1 ← 從來沒執行過底下的 action lastSkipReason='ShouldSimulate false…'
    activeSelf=True conditions=True forceWithoutAuthority=False
    parentObj=Zone… ShouldSimulte=False ← 沒有 authority，事件會靜靜地不執行
```

沒有任何 dealer 打進來時，會反查場上所有同 effectType 的 dealer，附距離與它們的 detector ——
「掛在哪顆 detector 下、那顆 detector 偵測範圍夠不夠」一眼看得到。

**在 Play Mode 跑才有意義**：`_enterNode` / `_parentObj` / dict / overlap 都是 runtime 才填的
（`[AutoChildren]` 與 `Awake` 建立），EditMode 下會退回用階層推，並且不印 `←` 結論。

## `refs` —— 誰指向這個節點 / 它指向誰

```bash
up refs "Assets/…/Interact Device Trigger.prefab" \
    --node "Modules/Destroyable ModulePack Variant/[VarFolder] VariableFolder/[Var] Durability"
up refs --node "資源生成器 FSM/Timer"           # 省略 asset = 當前開著的 scene
up refs "…prefab" --node "…" --out              # 反向：這個節點指向誰
up refs "…prefab" --node "…" --comp VarFloat    # 只算指向該 component 的（排除同節點其他 component）
```

輸出是「節點路徑（有 `_note` 就接在後面）+ `型別.欄位`」。**note 是掃這份清單的關鍵** ——
節點名是自動命名的（`[Action] Stamina 電力 += 2`），看不出用途，
`# 安全區慢慢充電` 這種資訊只在 note 裡：

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

掃的是 `SerializedObject.NextVisible(true)`，**會走進巢狀欄位**，所以 VarWrapper /
ValueProvider 那種間接引用（`_targetValue._var`）天生就涵蓋，不用另外想辦法。

範圍限「同一顆 prefab / 當前 scene 之內」。跨資產的全庫粗查才是離線索引的活（`up find`）——
目標是 asset 時（「哪些 prefab 引用這個 SO」）離線 `refs.to_guid` 就夠。

## `scene count` —— 數場上的物件

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

## Play Mode 驗證流程

```bash
up clear                                   # 清 Console，免得撈到舊的 error
up play play                               # 等到 tick 在跑 + --settle 秒（預設 1）才 return
sleep 8
up scene count --name 測試資源
up logs --type Error -n 4 --stack 4        # 精簡版 Console（原生 get-logs 太肥）
up play stop
```

分段取樣就能驗速率：每 4 秒 +4 顆 = 1 顆/秒，對得上 `_timeMax = 1`。

**`up play play` 會等 Play Mode 穩定才 return**：(1) isPlaying 且 domain reload / 編譯完成 (2) WorldUpdateSimulator
就緒、`WorldUpdateSimulator.CurrentTick` / `.SimulationTime` 真的在前進 (3) 再多等 `--settle` 秒（預設 1，開場 culling 判定之類）。
成功印 `Playing, tick=N, settled=1.0s`；超過 `--timeout`（預設 30）exit 1 並說卡在第幾步。輪詢走 Unity 端
`EditPlay.PlayStatus`（每次只回一行快照，不佔 main thread）。所以 play 之後不用再自己 `sleep` 等初始化。

**`up play play` 之後一定要 `up play stop`**：play 成功（`IsPlaying`）時 `unity.py` 會寫 `.claude/.agent-testing`（`<session>\t<時間>`），
主 toolbar 的 Agent Activity 鈕會亮「🤖 Play 測試中」叫使用者別動 Editor；只有 `up play stop` 會清。
忘了 stop 要等 10 分鐘沒碰 Unity 才過期（同 session 每次 up 碰 Unity 都會續命），使用者也能在
`Tools/MonoFSM/Agent Activity` 手動清。所有碰 Unity 的 up 呼叫都會記在 `Library/AgentActivity/activity.jsonl`
（`activity.py`；直接叫 `uloop` 的不會記）。

## 互動自動測試：測物件端 FSM，不經過玩家

使用者說「測 X 的互動」時的預設做法。**只測物件端**：不移動玩家、不模擬按鍵。玩家端的問題（選目標、interact state 進不去）要另外處理。

```bash
up menu "Tools/MonoFSM/FSM Trace/Clear"           # Enabled 存 SessionState，關著就先跑 ".../Enabled" 打開
up peek "<目標 Var 節點>"                           # 打之前的值（Edit Mode 讀到的是 serialized 值）
up play play                                      # 會等到穩定才 return，不用 sleep
up hit "<目標>/…/[Receiver] Interact"              # 看到 DONE canHit=True 才算打出去
up peek "<目標 Var 節點>"                           # 打之後
up hit …                                          # toggle 類物件要再打一次，確認會翻回來
up menu "Tools/MonoFSM/FSM Trace/Dump All"
up fsm-trace --entity "<目標名的獨特片段>" --no-snapshot
up play stop                                      # 成功或失敗都一定要 stop
```

**開始前先查靜態資料**：先用 `up obj` / `up prefab read --fsm` 讀懂「打了之後應該發生什麼」，也就是哪顆 machine 從哪個 state 轉到哪個 state、哪顆 Var 會變，再下判斷。
- 例：Toggle Device 按 E **不會切任何 state**，只跑 `[Receiver] Interact/[Event] EffectEnterNode` 底下的 action。這種物件要看 VarChange，不是看 Transition。

**怎麼從 trace 判斷結果**

| 看到的 | 意思 |
|---|---|
| `HitEnter` → `VarChange …: 舊 -> 新 by <action>` → `HitExit` | 成功。`by` 會寫出是哪顆 action 改的 |
| `HitBlocked ReceiverInvalid failCond=#i <節點>` | receiver 的 `[If]` 擋掉了（例如 `d_CanInteract`）。回頭 peek 那顆 condition 指到的 Var |
| `HitEnter` 之後沒有 VarChange，但緊接著有 `EventSkipped <原因>` | EnterNode 被跳過。Culling / NotSimulating：物件沒在模擬；ConditionInvalid：EventHandler 自己的條件沒過 |
| 什麼都沒有（連 HitEnter 都沒有） | `hit` 沒打出去（看 CLI 的 DONE 行），或 EnterNode 節點本身 inactive —— **inactive 不進 trace**，靜態讀 prefab 時看 `~` 就知道 |
| 預期的 Transition 沒出現 | 改用 `FsmTrace.Dump(name, entity)` 拍條件快照，看目前 state 的 outgoing transition 跟每顆 condition 的真假 |

**陷阱**
- `--entity` 是拿整行文字比對。同一個 scene 裡有名字相近的物件就會混進來，例如「引擎蓋」同時會撈到 `Interact Toggle Device 發財車引擎蓋` 跟 `Interact Device Trigger 發財車引擎蓋`，要用更獨特的片段。
- VarChange 預設只記掛了 `NetworkedVarTag` 的 bool / int / float / object。要看的 Var 沒出現，不代表它沒變，直接 `peek` 確認。
- 開場前一兩個 frame 的 tick 可能是上一輪的舊值（會看到 `f2 t152` 排在 `f3 t1` 前面），那幾筆的 tick 不要信。
- 吃到 `Another execution is already in progress`：代表別的 session 正在用 Unity。等幾秒再單獨重跑那一條，不要改指令。
- 要擺測試用的物件時，一律從 `TestKCC (複製我)_d.unity` 複製一份 scene（CLAUDE.md 規定），不要改使用者正在用的 scene。
- 回報時附上：`hit` 的 DONE 行、peek 前後的值、trace 裡相關的那幾行（原樣貼）。
