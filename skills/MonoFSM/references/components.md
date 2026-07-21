# MonoFSM 常用組件清單

## State 相關

### `GeneralState`
- 放在：`[StateFolder] StateFolder` 下
- `Key`：唯讀，自動等於 GameObject 名稱
- `Priority`：int，越高越不容易被打斷
- `_stateTags`：`List<StateTag>`，用 `[SOConfig("StateTags")]` 管理，標記該 State 的語意標籤（如 CanAttack、CanMove）
- `HasTag(StateTag)`：檢查此 State 是否帶有指定 tag
- 腳本路徑：`MonoFSM/1_MonoFSM_Core/Runtime/1_States/GeneralState.cs`

### `StateTag`
- ScriptableObject，用 Create > MonoFSM > State Tag 建立
- 代表 State 的語意分類（如 `CanAttack`、`Grounded`），供 `HasStateTagCondition` 判斷
- 腳本路徑：`MonoFSM/1_MonoFSM_Core/Runtime/1_States/StateTag.cs`

### `TransitionBehaviour`
- 放在：State 的直接子物件
- `_target`：`{"instanceID": GeneralState_component_ID}` → 目標 State 的 GeneralState component
- Conditions 從子物件 `[AutoChildren]` 自動收集

### `OnStateEnterHandler` / `OnStateUpdateHandler` / `OnStateExitHandler`
- 放在：State 的直接子物件
- 觸發子物件中所有 `IEventReceiver`（即 Action）
- Action 必須放在 Handler 下，不可直接放在 State 下

---

## Action 相關

### `AnimatorPlayAction`
- 放在：`OnStateEnterHandler`（或其他 Handler）下
- 繼承：`AbstractDescriptionBehaviour`，實作 `IRenderBehaiour`、`ITransitionCheckInvoker`
- 核心功能：進入 State 時播放指定的 Animator State（Play 或 CrossFade）
- 主要欄位：
  - `_animator`：`[DropDownRef] Animator`，目標 Animator（或透過子物件 `AnimatorRefSource` 間接取得）
  - `stateName`：`string`，要播放的 Animator State 名稱（Inspector 有 dropdown 選單）
  - `stateLayer`：`int`，Animator Layer 索引
  - `startNormalizedTimeOffset`：`float [0,1]`，播放起始 normalized time
  - `animatorEnterCrossFade`：`float`，> 0 時使用 CrossFade 而非 Play
  - `_clipLayerName`：`string`，動畫 Clip 所在的 Layer（synced layer 時用，未設定則 fallback 到 stateLayer）
  - `_skipWhenPlayingStateNames`：`List<string>`，當 Animator 正在播這些 State 時跳過播放
  - `_canInterruptSameState`：`bool`，是否可以打斷同一 State（預設 true）
  - `IsDontPlayWhenAnimatorDisabled`：`bool`，Animator 沒開時不強制開啟
  - `_isSkipOnRenderCheck`：`bool`，跳過 OnRender 的重播檢查
- 判斷方法：
  - `IsDone`：StateTime >= ClipLength（動畫播完）
  - `IsProgressPassedRatio(float ratio)`：播放進度超過指定比例
  - `IsPlayingCurrentClip()`：目前 Animator 是否正在播此 State
  - `Pause()` / `Resume()`：暫停/恢復 Animator
- 擴充：子物件可掛 `AnimatorPlayActionModule`（如 `AnimatorRandomStateModule` 隨機選 State）
- 生命週期：`OnEnterRender()` 播放動畫，`OnRender()` 每幀檢查並補播（防止被其他動畫蓋掉）
- 腳本路徑：`MonoFSM/1_MonoFSM_Core/Runtime/Action/AnimatorActions/AnimatorPlayAction.cs`

### `ResetTimerAction`
- 放在：`OnStateEnterHandler`（或其他 Handler）下
- `timer`：`[DropDownRef] VarFloatCountDownTimer`（**需手動在 Inspector 設定**，MCP 無法設定）
- 功能：呼叫 `timer.ResetTimer()`，重置計時器到 max 值
- 腳本路徑：`MonoFSM/1_MonoFSM_Core/Runtime/LifeCycle/Update/Simulate/ResetTimerAction.cs`

---

## Condition 相關

### `VarFloatIsBoundCondition`
- 放在：Transition 子物件
- `_varFloat`：`{"instanceID": VarFloat_component_ID}`
- `_boundType`：`0` = Max，`1` = Min
- 功能：檢查 VarFloat 是否到達邊界值（Min = 計時器歸零）
- 腳本路徑：`MonoFSM/1_MonoFSM_Core/Runtime/2_Variable/Condition/VarFloatIsBoundCondition.cs`

### `HasStateTagCondition`
- 放在：Transition 子物件
- `_fsmLogic`：`[DropDownRef] StateMachineLogic`（**需手動在 Inspector 設定**）
- `_tag`：`StateTag` ScriptableObject 引用
- 功能：檢查當前 State 是否帶有指定的 `StateTag`（透過 `GeneralState.HasTag()`）
- 腳本路徑：`MonoFSM/1_MonoFSM_Core/Runtime/1_Conditions/HasStateTagCondition.cs`

### `VarFloatCompareConstCondition`
- `_varFloat`：VarFloat component reference
- 與常數比較（大於/小於/等於）
- 腳本路徑：`MonoFSM/1_MonoFSM_Core/Runtime/2_Variable/Condition/VarFloatCompareConstCondition.cs`

---

## 計時器相關

### `VarFloatCountDownTimer`
- 放在：State 下（不需要在 Handler 下，因為它是 `IUpdateSimulate`）
- `currentTime`：`[DropDownRef]` VarFloat → 倒數用的 VarFloat
- `_autoRestart`：bool，到零後是否自動重置
- `_decayRate`：`VarFloatWrapper`，每秒衰減量（0 或未設定 = 每秒減 1，即原本行為）
- `_startDecayDelay`：`VarFloatWrapper`，Reset/SetTimer 後延遲多久才開始衰減
- `_conditions`：`[AutoChildren(DepthOneOnly = true)]` 子物件的 Condition，全部通過才會衰減
- 每幀 `Simulate()` 執行：delay 倒數完畢後，以 `decayRate * deltaTime` 衰減
- 搭配 `ResetTimerAction` 使用（呼叫 `SetTimer()` 會同時重置 delay）
- 腳本路徑：`MonoFSM/1_MonoFSM_Core/Runtime/LifeCycle/Update/Simulate/VarFloatCountDownTimer.cs`

---

## Variable 相關

### `VarFloat`
- `EditorValue`：設定初始值（可用 MCP set_property）
- `Min` / `Max`：從 `VariableFloatBoundModifier` 子物件取得
- `IsMin` / `IsMax`：是否到達邊界，供 `VarFloatIsBoundCondition` 使用
- 腳本路徑：`MonoFSM/1_MonoFSM_Core/Runtime/2_Variable/VarFloat.cs`

### `VariableFloatBoundModifier`
- 放在：VarFloat GO 的子物件（被 `[AutoChildren(false)]` 自動找到）
- `_maxValue`：`{"instanceID": VarFloat_component_ID}` → 代表 max 的 VarFloat
- `_minValue`：`{"instanceID": VarFloat_component_ID}` → 代表 min 的 VarFloat（null 時預設為 0）
- `_isResetToMaxOnRestore`：bool，State restore 時是否重置到 max
- 腳本路徑：`MonoFSM/1_MonoFSM_Core/Runtime/2_Variable/VariableModifier/VariableFloatBoundModifier.cs`

---

## MCP set_property 成功率摘要

| 屬性類型 | value 格式 | 可否用 MCP |
|---------|-----------|-----------|
| Component reference（一般） | `{"instanceID": -XXXX}` | ✅ |
| Enum | 數字（如 `1`） | ✅ |
| float / bool / string | 直接值 | ✅ |
| `EditorValue`（VarFloat 初始值） | float | ✅ |

---

## ValueSource 相關

### `TargetPositionResolver`
- 類型：`[Serializable]` helper class（非 MonoBehaviour），namespace `MonoValueProvider`
- 用 `[InlineProperty] [HideLabel]` 內嵌到任何組件
- 統一解析目標位置，優先順序：VarVector3 > VarTransform > VarEntity
- **凡需要 position / Transform 目標來源的欄位一律優先用它**，別再自己宣告 VarVector3 + Transform 手寫判斷
- 無需 Init；`GetTargetPosition(fallback)` 取位置、`HasTarget` 判斷、`ActiveSource` 顯示來源
- 詳見 `references/value-source.md`
- 腳本路徑：`MonoFSM-Pro/Runtime/ValueSource/TargetPositionResolver.cs`

### `Vec3HomingDirectionSource`
- 繼承：`AbstractValueSource<Vector3>`，實作 `IUpdateSimulate`、`ISceneAwake`
- 用途：追蹤導彈式方向計算（Slerp 慣性轉向）
- `_turnSpeed`：轉向速度，越小弧度越大
- 使用 `TargetPositionResolver` 解析目標位置
- 腳本路徑：`MonoFSM-Pro/Runtime/ValueProvider/Vec3HomingDirectionSource.cs`

### `NavMeshAgentMoveValueSource`
- 繼承：`AbstractValueSource<Vector2>`
- 用途：從 NavMeshAgent 取得導航移動方向
- 腳本路徑：`MonoFSM-Pro/Runtime/NavMeshPro/NavMeshAgentMoveValueSource.cs`
