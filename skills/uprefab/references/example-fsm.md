# 完整實例：從零組一個「定時生資源」FSM 並驗證

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
