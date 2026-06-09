# MonoFSM Scene Editing via Unity MCP

## 基本工作流程

### 1. 找到目標 FSM

```python
find_gameobjects(search_term="Fog FSM", include_inactive=True)
# → 取得 instanceID，如 -680046
```

### 2. 讀取 FSM 層級

```python
manage_scene(action="get_hierarchy", parent=-680046, max_depth=10, page_size=100)
# StateFolder 的 instanceID 通常在層級中（帶有 StateMachineLogic 組件）
```

### 3. 新增 State

```python
# 在 [StateFolder] StateFolder 下建立
manage_gameobject(
    action="create",
    name="[State] 黃昏",
    parent="Fog FSM/[StateFolder] StateFolder",
    components_to_add=["GeneralState"]
)
# Key 屬性唯讀，自動等於 GameObject 名稱
```

### 4. 新增 Transition

```python
# 在 State 下建立，需設定 _target 指向目標 State 的 GeneralState component
manage_gameobject(
    action="create",
    name="[Transition] => 夜晚",
    parent="Fog FSM/[StateFolder] StateFolder/[State] 黃昏",
    components_to_add=["TransitionBehaviour"]
)

# 取得目標 State 的 GeneralState component instanceID
# mcpforunity://scene/gameobject/{targetStateID}/component/GeneralState → instanceID

manage_components(
    action="set_property",
    target=-687930,   # Transition GO instanceID
    component_type="TransitionBehaviour",
    property="_target",
    value={"instanceID": -686776}  # 目標 GeneralState component instanceID
)
```

### 5. 新增 Condition（放在 Transition 下）

```python
manage_gameobject(
    action="create",
    name="[Condition] timer_min",
    parent="...[Transition] => 夜晚",
    components_to_add=["VarFloatIsBoundCondition"]
)

manage_components(
    action="set_property",
    target=-687946,
    component_type="VarFloatIsBoundCondition",
    properties={
        "_varFloat": {"instanceID": -687876},  # VarFloat component instanceID
        "_boundType": 1  # 0=Max, 1=Min
    }
)
```

### 6. 新增 Action（放在 Handler 下，不是 State 下）

Action 必須放在 EventHandler 子物件下，不能直接放在 State 下。

```python
# 先建 Handler
manage_gameobject(
    action="create",
    name="OnStateEnterHandler",
    parent="...[State] 黃昏",
    components_to_add=["OnStateEnterHandler"]
)

# 再建 Action 放在 Handler 下
manage_gameobject(
    action="create",
    name="[Action] ResetTimer",
    parent="...[State] 黃昏/OnStateEnterHandler",
    components_to_add=["ResetTimerAction"]
)
```

| Handler 類型 | 觸發時機 |
|-------------|---------|
| `OnStateEnterHandler` | 進入狀態時 |
| `OnStateUpdateHandler` | 每幀更新時 |
| `OnStateExitHandler` | 離開狀態時 |

## VarFloat 60 秒計時器完整設定

```
[VarFolder] VariableFolder
├── f_黃昏Timer          (VarFloat，EditorValue=60)
│   └── BoundModifier    (VariableFloatBoundModifier，_maxValue→f_黃昏Timer_max，_isResetToMaxOnRestore=true)
└── f_黃昏Timer_max      (VarFloat，EditorValue=60)
```

```python
# 1. 建立兩個 VarFloat
manage_gameobject(name="f_黃昏Timer", parent="...VariableFolder", components_to_add=["VarFloat"])
manage_gameobject(name="f_黃昏Timer_max", parent="...VariableFolder", components_to_add=["VarFloat"])

# 2. 建立 BoundModifier（VarFloat 的子物件）
manage_gameobject(name="BoundModifier", parent="...f_黃昏Timer", components_to_add=["VariableFloatBoundModifier"])

# 3. 設定初始值
manage_components(target=f_黃昏Timer_max_ID, component_type="VarFloat", property="EditorValue", value=60)
manage_components(target=f_黃昏Timer_ID, component_type="VarFloat", property="EditorValue", value=60)

# 4. 設定 BoundModifier 的 max 和 restore 行為
manage_components(
    target=BoundModifier_ID,
    component_type="VariableFloatBoundModifier",
    properties={"_maxValue": {"instanceID": f_黃昏Timer_max_VarFloat_component_ID}, "_isResetToMaxOnRestore": True}
)

# 5. 建立 CountDownTimer（放在 State 下）
manage_gameobject(name="[Timer] 黃昏計時器", parent="...[State] 黃昏", components_to_add=["VarFloatCountDownTimer"])
manage_components(
    target=Timer_GO_ID,
    component_type="VarFloatCountDownTimer",
    properties={"currentTime": {"instanceID": f_黃昏Timer_VarFloat_component_ID}}
)
```

## 設定 object reference 的格式

| 情況 | 格式 | 成功率 |
|------|------|--------|
| Component ref（一般） | `{"instanceID": -XXXX}` | ✅ 大多可以 |
| `[DropDownRef]` component ref | `{"instanceID": -XXXX}` | ❌ 需手動在 Inspector 設定 |

## 取得 component instanceID 的方法

```python
# 方法 1：讀取 component
ReadMcpResourceTool(uri="mcpforunity://scene/gameobject/{goID}/component/{ComponentTypeName}")
# → data.component.instanceID

# 方法 2：讀取所有 components
ReadMcpResourceTool(uri="mcpforunity://scene/gameobject/{goID}/components")
```
