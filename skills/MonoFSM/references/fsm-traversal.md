# Programmatically Reading a MonoFSM

實作 Editor 工具（FSM 匯出、視覺化、批次修改）時，如何 traverse 一個 MonoFSM prefab／scene 物件。撰寫 `FsmTextExporter` 時提煉的經驗。

實作參考：`MonoFSM/1_MonoFSM_Core/Editor/PrefabExporter/FsmTextExporter.cs`

## 1. 找到 FSM root

一個 prefab 內可能有**多個 FSM**（main FSM + sub-FSM）。判定方式：

```csharp
var stateFolders = root.GetComponentsInChildren<StateFolder>(true);
```

- 每個 `StateFolder` 就是一個 FSM 邊界
- 它的 `transform.parent` 是 FSM root（通常掛 `MonoFSMOwner`）
- 用「離 prefab root 最淺者」當主 FSM，其餘列為 sub-FSM

## 2. 找變數區

```csharp
var varFolder = fsmRoot.GetComponentsInChildren<VariableFolder>(true)
    .FirstOrDefault(vf => vf.transform.parent == fsmRoot);
```

VariableFolder 是 FSM root 的 first-level child（與 StateFolder 同層）。folder 子樹下的所有 `AbstractMonoVariable` 就是該 FSM 的變數。

注意 var 也可能巢狀（有 parent VarEntity），要過濾：

```csharp
varFolder.GetComponentsInChildren<AbstractMonoVariable>(true)
    .Where(v => /* 走到第一個 VariableFolder 必須是 folder 本人 */);
```

## 3. 走 states

```csharp
foreach (Transform stateTr in stateFolder.transform)
{
    var state = stateTr.GetComponent<MonoStateBehaviour>();
    if (state == null) continue;
    // state 是 StateFolder 的直接子物件
}
```

`MonoStateBehaviour` 是基底，`GeneralState` 與 `AnyState` 都繼承之。

## 4. 走 transitions 與 conditions

Transitions 是 state 的 **direct child**：

```csharp
foreach (Transform child in state.transform)
{
    var tr = child.GetComponent<TransitionBehaviour>();
    if (tr == null) continue;
    var target = tr._target;          // 目標 MonoStateBehaviour
    // tr.Description 已是 "=>TargetName" 格式（除掉 [State] tag）
}
```

Conditions 是 transition 的 **direct child**：

```csharp
foreach (Transform c in tr.transform)
{
    var cond = c.GetComponent<AbstractConditionBehaviour>();
    if (cond == null) continue;
    var inverted = cond.FinalResultInverted;   // true → 顯示 "if not"
    var desc = cond.Description;               // e.g. "CartIndex == 0 == True"
}
```

## 5. 走 actions — ⚠️ 重要 gotcha

**不要只找 `AbstractStateAction`**。許多 state 的「進入動作」是 `AnimatorPlayAction`，它繼承 `AbstractDescriptionBehaviour` + `IRenderBehaiour`，**不繼承** `AbstractStateAction`。

正確做法：在 state 的非-transition 子樹中找所有 `AbstractDescriptionBehaviour`，再排除 condition / transition / state 自己：

```csharp
foreach (Transform child in state.transform)
{
    if (child.GetComponent<TransitionBehaviour>() != null) continue;
    foreach (var b in child.GetComponentsInChildren<AbstractDescriptionBehaviour>(true))
    {
        if (b is AbstractConditionBehaviour) continue;
        if (b is TransitionBehaviour) continue;
        if (b is MonoStateBehaviour) continue;
        // b 就是一個 action / render behaviour
    }
}
```

這樣 `AnimatorPlayAction`、`AbstractStateAction` 子類、`SetVarBoolAction`、`ResetTimerAction` 等都能抓到。

慣例上 action 應該掛在 `OnStateEnterHandler` 下，但實際資料**不一定遵守**（有的直接掛在 state 下），所以 traversal 不要假設 handler 存在。

## 6. 拿語意名稱：用 `Description`

每個 `AbstractDescriptionBehaviour` 都有 `public virtual string Description` 屬性 — **直接拿來顯示，不用重新造命名**。

GameObject name 通常是 `[State] xxx`、`[Transition] => yyy`、`[If] zzz` 這種帶 tag 的格式；要核心名稱用 regex 清掉前綴：

```csharp
// 移除 GameObject name 開頭的 [Tag]
var clean = Regex.Replace(raw, @"^(\[.*?\]\s*)+", "").Trim();
```

或者直接用 `b.ReformatedName`（基底已提供）。

## 7. 已知 Description 小瑕疵

寫匯出/顯示工具時要注意：

| 來源 | 現象 | 範例 |
|---|---|---|
| `VarBoolCompareCondition` | Description 自帶 ` == True` 後綴 | `CartIndex == 0 == True` |
| `AnimatorPlayAction` | Description 有前導空格 | `" Animator: Idle L:0"` |

要嘛各自 override Description 修掉，要嘛在你的匯出/顯示層做後處理。

## 8. Edit-time safety

`Description` 可能在 edit-time 依賴 runtime 欄位拋例外（例如 `_target` 為 null）。包 try/catch fallback 到 `CleanName(gameObject.name)`：

```csharp
try { return b.Description; }
catch { return CleanName(b.name); }
```

## 9. Reference 解析 — instanceId vs 語意

`TransitionBehaviour._target`、condition 的 VarBool reference 等 SerializedField 在 prefab YAML 是 `{fileID: ...}` 數字。在 runtime / Editor C# 反射拿到的是 `MonoStateBehaviour` / `AbstractMonoVariable` 物件本身，可以直接讀 `.name` / `.Description`。**不要走 instanceId 比對**，直接抓物件參考即可。
