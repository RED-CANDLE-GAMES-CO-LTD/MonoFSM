# VarWrapper 系列（VarFloatWrapper / VarIntWrapper / ...）

## 是什麼

`[Serializable]` 包裝類，讓一個欄位可以在 Inspector **二選一**：

- 綁定一個 `Var` 引用（如 `VarInt`、`VarFloat`），runtime 讀該變數的值
- 或不綁，直接填一個常數（存在內部 `_tempValue`）

不需要為「吃變數」跟「吃常數」各寫一個欄位。比起直接放 `int` / `float`，多了「可由設計師接到 Var 系統」的彈性。

定義檔：`MonoFSM/1_MonoFSM_Core/Runtime/2_Variable/VarFloatWrapper.cs`
（同檔內含 `VarFloatWrapper : VarWrapper<VarFloat, float>`、`VarIntWrapper : VarWrapper<VarInt, int>` 等）

namespace：`MonoFSM.Variable` — 用到時記得 `using MonoFSM.Variable;`

## 宣告與預設值

```csharp
using MonoFSM.Variable;

// 預設常數 0
[SerializeField] private VarIntWrapper _count;

// 自訂預設常數（無參數建構子 = 0，有參數 = 指定值）
[SerializeField] private VarIntWrapper _index = new(-1);

[SerializeField] private VarFloatWrapper _speed = new(1f);
```

## 讀 / 寫值

```csharp
int v = _index.Value;              // 讀：未綁 Var 回 _tempValue，已綁回 _var.Get<int>()
_count.SetValue(_count.Value + 1, this);   // 寫：已綁更新 Var，未綁更新 _tempValue
```

| 成員 | 說明 |
|------|------|
| `.Value` | 讀目前值（int / float ...） |
| `.SetValue(value, this)` | 設值（第二參數傳 source object） |
| `.Description` | 已有描述屬性，可直接組進自訂 Action / Condition 的 `Description` |

## 要點

- 取值一律用 `.Value`，不要假設它是裸 `int`/`float`
- `Description` override 時直接用 wrapper 的 `.Description`，或讀 `.Value` 組字串
- 範例：`IntMathAction.cs`、`VarIntCompareCondition.cs`（`1_MonoFSM_Core/Runtime/Action/VariableAction/`、`1_Conditions/`）

> 把既有的裸 `int`/`float` 序列化欄位改成 Wrapper 而想保住 prefab reference 時，見 [serialization-migration.md](serialization-migration.md)。
