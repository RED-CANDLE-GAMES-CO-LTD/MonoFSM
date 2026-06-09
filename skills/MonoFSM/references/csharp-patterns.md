# C# 效能模式（Unity）

## GC Avoidance

避免在 hot path 使用 LINQ，大多數 LINQ 方法會造成 heap allocation。

### 取集合第一個元素

| 集合型別 | 替代方式 | 備註 |
|---|---|---|
| `List<T>` / 陣列 | `list[0]` | 零 GC，最快 |
| `IList<T>` | `list[0]` | 直接 indexer |
| `HashSet<T>` / `IEnumerable<T>` | `foreach + break` | 無 indexer，只能用此法 |

#### HashSet 範例

```csharp
// 改前（有 GC）
var only = _receivers.First();

// 改後（零 GC）
T only = null;
foreach (var r in _collection) { only = r; break; }
if (only == null) return; // 消除靜態分析 nullable 警告
```

> `if (only == null) return;` 是為了讓 Rider 靜態分析不報 nullable 警告，
> 因為分析器不知道 `Count == 1` 保證 foreach 一定執行。

### 泛型型別的 Equals 比較

`EqualityComparer<T>.Default.Equals(a, b)` 對有實作 `IEquatable<T>` 的 struct（如 Vector3）不會 boxing，可替代 `.Equals()`：

```csharp
// 改前（有 GC，Vector3 會 boxing）
if (tempValue.Equals(CurrentValue))

// 改後（零 GC）
if (EqualityComparer<T>.Default.Equals(tempValue, CurrentValue))
```

### 泛型型別的 null 檢查

**陷阱：** 未加 `where T : class` 約束的泛型，`value != null` 會隱式 boxing → GC。
Unity Mono（Editor）不會優化掉此行為，即使 T 是 struct。

```csharp
// 有 GC（T 無約束時隱式 boxing）
public bool HasValue => Value != null;
```

**解法 ①**：T 一定是 class，加約束：
```csharp
public abstract class MyClass<T> where T : classㄒ
// 則 Value != null 是純 reference 比較，無 boxing
```

**解法 ②**：T 可能是 struct，用 `EqualityComparer`：
```csharp
public bool HasValue => !EqualityComparer<T>.Default.Equals(Value, default);
// 注意：語意變成「不等於 default(T)」，struct 的 zero value 視為無值
```

### ToList() 造成 GC

每次呼叫 `ToList()` 都會 new 一個新 List → GC。若外部只需要 read，直接回傳內部 List reference：

```csharp
// 改前（有 GC）
private readonly HashSet<MonoEntity> _items = new();
public List<MonoEntity> GetItems() => _items.ToList();

// 改後（零 GC）
private readonly List<MonoEntity> _items = new();
public List<MonoEntity> GetItems() => _items;
```

**HashSet vs List 選擇原則（小集合）：**
- 需要 `Contains` 去重查詢 → 用 `HashSet`
- 只需要 iterate / Add / Remove，且元素數量少（< ~10）→ 用 `List`
- `List.Remove` 雖然 O(n)，但 n 很小時實際比 `HashSet` 的 hash 計算更快

### 衍生集合去重（多 source → 單 entity 場景）

當多個 source（如多個 `GeneralEffectReceiver`）對應同一個 entity，需要維護一份「不重複 entity List」時，
**不要用 LINQ `Distinct().ToList()`（每次呼叫都 new List + GC）**，改為主動維護：

```csharp
// 改前（有 GC）
public List<MonoEntity> GetHittingEntities()
    => _receivers.Select(r => r.BindEntity).Distinct().ToList();

// 改後（零 GC，主動維護）
private readonly List<MonoEntity> _hittingEntities = new();

// OnHitEnter：加入前檢查
if (!_hittingEntities.Contains(receiverEntity))
    _hittingEntities.Add(receiverEntity);

// OnHitExit：只有在沒有其他 receiver 指向同一 entity 時才移除
_receivers.Remove(exitReceiver);
bool entityStillActive = false;
foreach (var r in _receivers)
{
    if (r.BindEntity == entity) { entityStillActive = true; break; }
}
if (!entityStillActive)
    _hittingEntities.Remove(entity);

// Getter 直接回傳 reference，零 GC
public List<MonoEntity> GetHittingEntities() => _hittingEntities;
```

> 重點：`_receivers.Remove` 必須在檢查 `entityStillActive` **之前**執行，
> 這樣才能正確判斷「exit 之後還有沒有其他 receiver」。

## 時間與 DeltaTime

**不要用 `Time.time`、`Time.deltaTime`、`Time.fixedDeltaTime`**，應使用 `WorldUpdateSimulator` 的靜態屬性：

| Unity API | 替代方式 | 用途 |
|---|---|---|
| `Time.deltaTime` / `Time.fixedDeltaTime` | `WorldUpdateSimulator.DeltaTime` | 每幀時間間隔 |
| `Time.time` | `WorldUpdateSimulator.CurrentTick` | 時間點記錄 |

### 經過時間計算

用 tick 差值乘以 DeltaTime，避免浮點精度問題：

```csharp
private int _lastActionTick;

// 記錄時間點
_lastActionTick = WorldUpdateSimulator.CurrentTick;

// 計算經過時間
float elapsed = (WorldUpdateSimulator.CurrentTick - _lastActionTick) * WorldUpdateSimulator.DeltaTime;
if (elapsed > threshold) { /* ... */ }
```

> **Why：** 專案透過 `WorldUpdateSimulator` 統一管理更新迴圈（含網路同步時的 `Runner.DeltaTime`），
> 直接用 `Time.time` 會在 Fusion 網路模式下與 simulation tick 不同步。

## 欄位引用 Attribute 選擇

| Attribute | 用途 | 場景範例 |
|---|---|---|
| `[Auto]` / `[AutoParent]` / `[AutoChildren]` | 自動 cache 自身/父/子物件上的 component | `bindingState`、`_actionParent` 等內部依賴 |
| `[CompRef]` | 標記為「同物件或子物件上的 component reference」，Inspector 顯示為 disabled | 自身持有的 component（如 `DelayActionModifier`） |
| `[DropDownRef]` | 從場景/Prefab 既有物件中選擇引用，Inspector 顯示下拉選單 | 引用外部既有的 `ParticleSystem`、`VarFloat`、`StateMachineLogic` 等 |

**判斷原則：** 如果引用的對象是「自己身上或子物件自動擁有的」用 `[CompRef]`；如果是「場景中其他既有物件」用 `[DropDownRef]`。

## Component 取得方式

### MonoEntity.GetCompCache\<T\>()

在 Action 或其他邏輯中需要從 MonoEntity 取得 component 時，**不要用 `GetComponentInParent` / `GetComponentInChildren`**，
應使用 `MonoEntity.GetCompCache<T>()`，這是專案自行維護的快取機制，避免每次呼叫 Unity 的 GetComponent 開銷。

```csharp
// ❌ 不要用
var rb = source.GetComponentInParent<Rigidbody>();

// ✅ 用 GetCompCache
var rb = source.GetCompCache<Rigidbody>();
```