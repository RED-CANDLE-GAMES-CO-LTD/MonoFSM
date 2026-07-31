# 查型別 / 查欄位 / 讀 runtime 值 / 引用反查 / 數物件

## `types` / `fields` / `peek`

這三個的存在理由都是省 context —— 替代方案是把幾百行 .cs 讀進來，而且讀到的可能是
註解掉的舊欄位。這裡回的是反射看到的真值。

```bash
up types CountDownTimer                    # 名稱含這段的 Component 型別
up fields VarFloatCountDownTimer --own     # 可 serialize 欄位（--own = 不含繼承）
up peek "資源生成器 FSM/Timer" VarFloatCountDownTimer --members "IsTimerUp,Description"
```

`peek` 在 Play Mode 下讀的是**當下的 runtime 值** —— 除「為什麼沒動」最快的一步。
`--members` 留空會 dump 所有 public 屬性（很吵，通常指定幾個就好）。

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

## `refs` —— 誰指向這個節點 / 它指向誰

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
up play play
sleep 8
up scene count --name 測試資源
up logs --type Error -n 4 --stack 4        # 精簡版 Console（原生 get-logs 太肥）
up play stop
```

分段取樣就能驗速率：每 4 秒 +4 顆 = 1 顆/秒，對得上 `_timeMax = 1`。
