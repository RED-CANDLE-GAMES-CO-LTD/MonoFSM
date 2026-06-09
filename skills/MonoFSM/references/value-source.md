# ValueSource 與 Variable 系統

## Tag-mapping Var（重宣告一顆 Var、用 tag 對應來源 entity 的同 tag 變數）

需求：在某 prefab（常見於 UI）裡「重宣告」一顆 Var，讓它自動對應到某來源 entity 上同 `VariableTag` 的變數，外部只注入一次來源即可，不用每顆手拉 reference。

**正解：用 opt-in 的 value source `GetVarFromParentEntitySource`，掛在那顆 Var 下。**

```
[Var] f_title                                  # 重宣告的 tag-mapping Var
└── GetVarFromParentEntitySource               # value source：_varTag = Title
        _parentEntity  ([AutoParent] 預設來源)   # in-hierarchy 場景零設定
        _overrideSourceEntity (可選 VarEntity)   # 注入場景：指向要取值的 entity
```

- 它實作 `IValueProvider`，掛在 Var 下會被 `TypedMonoVariable._valueSources`（`[AutoChildren]`）撿走成為該 Var 的 value source。
- 取值：`SourceEntity.GetVar(_varTag).GetValue<T>()`；`SourceEntity` = 有設 `_overrideSourceEntity` 就用它的 `.Value`，否則用 `[AutoParent] MonoEntity`。
- 腳本：`MonoFSM/1_MonoFSM_Core/Runtime/0_Pattern/DataProvider/GetVarFromParentEntitySource.cs`

**VarListEntity 注入場景**（從 list 取某 index 的 entity，再取它的 GameData/Title）：
用一顆 `VarEntity` 掛 entity source（取 list 當前/指定 index 的 item）算出單一 entity，再讓各 tag-mapping Var 的 `GetVarFromParentEntitySource._overrideSourceEntity` 指向那顆 VarEntity。注入點 = 那顆 VarListEntity 一處。

### 不要用 `_parentVarEntity` proxy 做這件事

`AbstractMonoVariable._parentVarEntity` 是 `[AutoParent]` 抓的 proxy 機制，會讓 **subtree 內所有後代 Var 都被迫變 proxy（Nested Proxy）**，且本該用自己 value source 的 Var 也被蓋掉。`GetVarFromParentEntitySource` 是 per-Var、opt-in 的，不會污染兄弟節點。

### 解析優先序（已修正）

各 Var 取值時 **value source 優先於 `_parentVarEntity` proxy**：
- `AbstractFieldVariable.CurrentValue`（primitive var）：`valueSource` → `varRef`(proxy) → local field
- `GenericObjectVariable.GetValueInternal`（object var）：`HasValueSource` → `HasParentVarEntity`(proxy) → `_defaultValue`

`_parentVarEntity` proxy 保留為 fallback（其他情境仍會用），只是降到 value source 之後。

---

## AbstractValueSource\<T\> 基礎架構

泛型基類，子類需實作 `Value` 屬性回傳計算結果。

- 搭配 `IUpdateSimulate` 做每幀計算（`Simulate(float deltaTime)`）
- 搭配 `ISceneAwake` 做初始化
- 繼承自 `MonoBehaviour`，掛在 GameObject 上作為組件使用

腳本路徑：`MonoFSM/1_MonoFSM_Core/Runtime/0_Foundation/AbstractValueSource.cs`

---

## 撰寫新 ValueSource 的 Pattern

### 基本結構

繼承 `AbstractValueSource<T>` 並實作對應的 Provider 介面（如 `IFloatProvider`）。

```csharp
using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    public class MyFloatValueSource : AbstractValueSource<float>, IFloatProvider
    {
        [Required] [DropDownRef] public VarFloat _dropDownRef;

        public override float Value => _dropDownRef != null ? _dropDownRef.CurrentValue : 0f;

        public override string Description => _dropDownRef?.PathName;
    }
}
```

### 關鍵要點

- **繼承**：`AbstractValueSource<T>` 提供 `IsValid`（含 ConditionGroup）、`HasValue`、hierarchy 資訊
- **Provider 介面**：float 用 `IFloatProvider`，bool 用 `IBoolProvider`，Vector3 用 `IValueProvider<Vector3>`
- **`[DropDownRef]`**：搭配 `[Required]` 讓 Inspector 可以用下拉選單選擇同層級的 Variable
- **Null guard**：`Value` getter 要處理 `_dropDownRef == null` 的情況
- **Description**：override `Description` 屬性讓 Inspector 顯示有意義的名稱
- **檔案位置**：Float 相關放 `MonoFSM/1_MonoFSM_Core/Runtime/0_Pattern/DataProvider/ComponentWrapper/Float/`

### 存取 Variable 不同層級的值

| 目標 | 存取方式 | 說明 |
|------|---------|------|
| VarFloat 當前值 | `varFloat.CurrentValue` | 經過所有處理的最終值 |
| VarStat 最終值 | `varStat.CurrentValue` | 經過 StatModifier 計算的值 |
| VarStat BaseValue | `varStat.Field.CurrentValue` | 未經 modifier 的原始值 |

### 範例：VarStatBaseValueRef

取得 VarStat 的 BaseValue（未經 modifier 計算的原始值）：

```csharp
public class VarStatBaseValueRef : AbstractValueSource<float>, IFloatProvider
{
    [Required] [DropDownRef] public VarStat _dropDownRef;

    public override float Value => _dropDownRef != null ? _dropDownRef.Field.CurrentValue : 0f;

    public override string Description => _dropDownRef?.PathName + " (Base)";
}
```

路徑：`MonoFSM/1_MonoFSM_Core/Runtime/0_Pattern/DataProvider/ComponentWrapper/Float/VarStatBaseValueRef.cs`

---

## 常見 ValueSource 實作

### Vec3 系列

| 類別 | 用途 | 路徑 |
|------|------|------|
| `Vec3FromTransformPositionSource` | 靜態 Transform 位置 | `MonoFSM-Pro/Runtime/ValueProvider/Vec3FromTransformPositionSource.cs` |
| `Vec3FromTransformRotationSource` | Transform forward 方向 | `MonoFSM-Pro/Runtime/ValueProvider/Vec3FromTransformRotationSource.cs` |
| `Vec3AverageFromEntity` | 多 Entity 位置平均 | `MonoFSM-Pro/Runtime/ValueProvider/Vec3AverageFromEntity.cs` |
| `Vec3ForceFromSplineRiver` | 河流物理力計算 | `MonoFSM-Pro/Runtime/ValueProvider/Vec3ForceFromSplineRiver.cs` |
| `Vec3HomingDirectionSource` | 追蹤導彈式方向（Slerp 慣性轉向） | `MonoFSM-Pro/Runtime/ValueProvider/Vec3HomingDirectionSource.cs` |

### Vec2 / Float 系列

| 類別 | 用途 | 路徑 |
|------|------|------|
| `Vec2MonoInputValueSource` | 玩家輸入 | `MonoFSM/MonoFSM_InputAction/InputAction/Vec2MonoInputValueSource.cs` |
| `NavMeshAgentMoveValueSource` | NavMesh 導航方向（Vec2） | `MonoFSM-Pro/Runtime/NavMeshPro/NavMeshAgentMoveValueSource.cs` |
| `FloatDisBetweenEntity` | 兩 Entity 間距離 | `MonoFSM-Pro/Runtime/ValueProvider/FloatDisBetweenEntity.cs` |
| `Vector3FromFloatSource` | 從 Float 組合 Vec3 | `MonoFSM-Pro/Runtime/ValueProvider/Vector3FromFloatSource.cs` |

---

## TargetPositionResolver 共用目標解析

`[Serializable]` class，可用 `[InlineProperty]` 內嵌到任何 MonoBehaviour 中，統一解析「目標位置」。

### 優先順序

1. **VarVector3** — 直接位置座標
2. **VarTransform** — 從 Transform 取 position
3. **VarEntity** — 透過 `TransformOfEntity` 取 position

### 使用方式

```csharp
[SerializeField] [InlineProperty] private TargetPositionResolver _targetResolver;

// 在 ISceneAwake 中初始化
_targetResolver.Init(gameObject);

// 取得目標位置
Vector3 targetPos = _targetResolver.GetPosition();
```

### 關鍵細節

- 用 `IsValueExist` 判斷 runtime 有值，不是只檢查 Var 引用是否存在（引用存在 ≠ runtime 有值）
- 向後相容策略：保留舊的獨立欄位（如 `_target`），新的 Resolver 優先，fallback 到舊欄位

腳本路徑：`MonoFSM-Pro/Runtime/ValueProvider/TargetPositionResolver.cs`

---

## Variable IsValueExist 慣例

各型別 `IsValueExist` 的語意：

| 型別 | IsValueExist 語意 | 說明 |
|------|-------------------|------|
| `VarTransform` | `Value != null` | Transform 引用存在 |
| `VarEntity` | `Value != null` | Entity 引用存在 |
| `VarVector3` | `!IsNull` | 有特殊 nullable 機制 |
| `VarFloat` | `CurrentValue != 0f` | 非零即有值 |
| `VarBool` | always `true` | 永遠有值 |

**使用時機**：在 runtime 判斷 Var 是否有有效值，常見於 TargetPositionResolver 的優先順序判斷。

---

## Slerp 追蹤方向 Pattern

`Vec3HomingDirectionSource` 使用的導彈式追蹤模式：

```csharp
// 在 Simulate(float deltaTime) 中
Vector3 toTarget = (targetPos - currentPos).normalized;
_currentDirection = Vector3.Slerp(_currentDirection, toTarget, _turnSpeed * deltaTime);
```

### 設計要點

- 用 `Vector3.Slerp` 而非 `Lerp`：保持 magnitude 不變，弧線轉向更自然
- `_turnSpeed * deltaTime` 控制慣性：值越小轉向越慢，飛行弧度越大
- `_currentDirection` 是有狀態的欄位，每幀漸進更新

### 網路注意事項（Fusion）

- 在 `Simulate` 裡計算，使用傳入的 `deltaTime`（不用 `Time.deltaTime`）
- `_currentDirection` 有狀態，需考慮網路同步

---

## TransformOfEntity 工具

從 VarEntity 取得位置的共用 helper，定義在 `Vec3AverageFromEntity` 中。

- 優先找 `Animator` 的 transform（角色模型根節點）
- 找不到則 fallback 到 entity 的 transform
- 常用於需要取得角色「視覺位置」而非邏輯位置的場合

腳本路徑：`MonoFSM-Pro/Runtime/ValueProvider/Vec3AverageFromEntity.cs`
