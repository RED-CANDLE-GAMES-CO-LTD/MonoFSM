# PoolObject/PoolManager 系統使用手冊

## 📋 目錄
- [系統架構概覽](#系統架構概覽)
- [快速開始](#快速開始)
- [池預熱 (Prewarming) - 推薦使用 PoolBank](#池預熱-prewarming---推薦使用-poolbank)
- [MonoPoolObj 依賴說明](#monopoolobj-依賴說明)
- [基本功能](#基本功能)
- [進階功能](#進階功能)
- [編輯器工具](#編輯器工具)
- [最佳實踐](#最佳實踐)
- [疑難排解](#疑難排解)
- [API 參考](#api-參考)

---

## 系統架構概覽

### 核心組件

| 組件 | 職責 | 檔案位置 |
|------|------|----------|
| **PoolManager** | 池系統的中央管理器 | `PoolManager.cs` |
| **PoolObject** | 可被池化的物件基類（基礎池功能） | `PoolObject.cs` |
| **MonoPoolObj** | 生命週期管理器（進階池功能 + WorldUpdateSimulator 整合） | `MonoPoolObj.cs` |
| **ObjectPool** | 管理單一類型物件的池 | `ObjectPool.cs` |
| **PoolBank** | 🌟 自動化場景池配置管理（推薦使用） | `PoolBank.cs` |
| **SceneLifecycleManager** | 場景生命週期管理 (在 MonoFSM.Runtime 命名空間中) | `SceneLifecycleManager.cs` |

### 新增的輔助系統

| 系統 | 功能 | 檔案位置 |
|------|------|----------|
| **PoolLogger** | 統一日誌管理 | `PoolLogger.cs` |
| **TransformResetHelper** | Transform重置輔助 | `TransformResetHelper.cs` |
| **PoolServiceLocator** | 服務定位器（降低耦合） | `PoolServiceLocator.cs` |
| **Interface系統** | 抽象介面定義 | `IPoolManager.cs` |

---

## 快速開始

### 1. 基本設置

#### 建立池管理器
```csharp
// PoolManager 會自動作為 Singleton 建立
// 在場景中添加 PoolManager 預製體或空物件並掛載 PoolManager 腳本
```

#### 建立可池化物件
```csharp
// 為你的 GameObject 添加 PoolObject 組件
public class MyPoolableObject : MonoBehaviour, IPoolObject
{
    public void PoolOnReturnToPool()
    {
        // 物件回到池時的清理邏輯
    }
    
    public void PoolOnPrepared(PoolObject poolObj)
    {
        // 物件從池取出時的初始化邏輯
    }
    
    public void PoolBeforeReturnToPool()
    {
        // 物件回到池前的預處理邏輯
    }
}
```

### 2. 基本使用

#### 借用物件
```csharp
// 方法 1: 直接借用或實例化
GameObject obj = PoolManager.Instance.BorrowOrInstantiate(
    prefab, 
    position: Vector3.zero, 
    rotation: Quaternion.identity, 
    parent: transform
);

// 方法 2: 使用泛型方法
MyPoolableObject myObj = PoolManager.Instance.BorrowOrInstantiate<MyPoolableObject>(
    myPrefab, 
    Vector3.zero, 
    Quaternion.identity, 
    transform
);
```

#### 歸還物件
```csharp
// 方法 1: 直接調用物件的歸還方法
poolObject.ReturnToPool();

// 方法 2: 通過管理器歸還
PoolManager.Instance.ReturnToPool(poolObject);
```

---

## 基本功能

### 池預熱 (Prewarming) - 推薦使用 PoolBank

#### 🌟 方法一：PoolBank 自動化管理（推薦）

PoolBank 提供完全自動化的預熱資料管理，是最簡單且推薦的使用方式：

**設置步驟**：
1. 在場景中放置一個 GameObject
2. 添加 PoolBank 組件
3. 點擊 Inspector 中的「Create」按鈕自動生成預熱資料
4. 系統會自動在 `Assets/15_PoolManagerPrewarm/` 創建對應場景的預熱資產

```csharp
// PoolBank 會自動處理所有設置，無需手動程式碼
// 只需在場景中放置 PoolBank 組件即可

public class MySceneController : MonoBehaviour
{
    void Start()
    {
        // PoolBank 會在 EnterSceneAwake 時自動執行：
        // 1. 準備全域預熱資料
        // 2. 設置場景預熱資料  
        // 3. 重新計算池大小
        // 4. 顯示動態 Pool 統計資訊
    }
}
```

**自動化特性**：
- ✅ 自動根據場景名稱生成 `{SceneName}_Prewarm.asset`
- ✅ 自動處理全域預熱資料 `_Global_Prewarm.asset`  
- ✅ 在 `EnterSceneAwake` 時自動設置和計算池
- ✅ 支援 Protected 物件統計和動態監控
- ✅ 場景保存時自動維護預熱資料
- ✅ 無需手動撰寫初始化程式碼

#### 方法二：手動 PoolPrewarmData 管理（進階）

適用於需要精細控制或跨場景共享預熱資料的情況：

```csharp
// 1. 建立 PoolPrewarmData 資產
// 在 Project 視窗右鍵 -> Create -> Boa -> PoolManager -> Create PoolPrewarmData

// 2. 手動設定預熱物件
public class GameLevelController : MonoBehaviour
{
    public PoolPrewarmData prewarmData;
    
    void Start()
    {
        PoolManager.Instance.SetPrewarmData(prewarmData, this);
        PoolManager.Instance.ReCalculatePools();
    }
}
```

**使用時機**：
- 需要跨多個場景共享預熱設定
- 需要在運行時動態調整預熱參數
- 需要程式化控制預熱時機

### MonoPoolObj 依賴說明

**重要**：PoolBank 需要 MonoPoolObj 組件才能正常運作。

MonoPoolObj 是生命週期管理組件，負責處理物件的場景喚醒、狀態重置等生命週期事件。當你使用 PoolBank 時，系統會自動添加 MonoPoolObj 組件到同一個 GameObject 上。

#### 自動添加
```csharp
// PoolBank 已設置 [RequireComponent(typeof(MonoPoolObj))]
// 添加 PoolBank 組件時會自動添加 MonoPoolObj
```

#### 生命週期處理
MonoPoolObj 會自動處理以下生命週期事件：
- **場景喚醒**：處理 ISceneAwake 介面
- **狀態重置**：處理 IResetStateRestore 介面  
- **場景開始**：處理 ISceneStart 介面

大多數情況下，你不需要直接操作 MonoPoolObj，PoolBank 會自動管理所有相關功能。

### Transform 管理

#### 自動 Transform 重置
```csharp
public class MyPoolObject : PoolObject
{
    void Start()
    {
        // Transform 會自動記錄初始狀態
        // 歸還時會自動重置到初始位置
    }
    
    public void CustomTransformSetup()
    {
        // 自定義 Transform 設置
        OverrideTransformSetting(
            position: new Vector3(1, 2, 3),
            rotation: Quaternion.identity,
            parent: customParent,
            scale: Vector3.one
        );
    }
}
```

---

## 進階功能

### 物件保護系統

物件保護系統提供簡單的二元狀態管理，防止物件被意外回收。

#### 保護狀態
- **Protected（保護）**：物件不會被強制回收
- **Recyclable（可回收）**：物件可以正常回收到池中

#### 基本保護操作
```csharp
// 設置為保護狀態
poolObject.MarkAsProtected();

// 設置為可回收狀態
poolObject.MarkAsRecyclable();

// 檢查保護狀態
if (poolObject.IsProtected())
{
    Debug.Log("物件受到保護");
}

// 檢查是否可回收
if (poolObject.IsRecyclable())
{
    poolObject.ReturnToPool();
}
```

#### 實際使用範例
```csharp
public class AnimatedPoolObject : PoolObject
{
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
    }
    
    public void PlayAnimation(string animName)
    {
        // 播放動畫時保護物件
        MarkAsProtected();
        animator.Play(animName);
        
        // 動畫結束後設為可回收
        StartCoroutine(UnprotectAfterAnimation());
    }
    
    IEnumerator UnprotectAfterAnimation()
    {
        yield return new WaitUntil(() => !animator.IsPlaying());
        MarkAsRecyclable();
    }
}
```


### 服務定位器模式

服務定位器提供松耦合的服務訪問方式，避免直接依賴具體的池管理器實作：

#### 可用的服務
- **PoolManager**：主要的池管理服務
- **SceneLifecycleManager**：場景生命週期管理服務  
- **TransformResetManager**：Transform重置管理服務

#### 使用方式
```csharp
public class MyCustomSystem : MonoBehaviour
{
    void Start()
    {
        // 通過服務定位器獲取池管理器，無需直接依賴
        var poolManager = PoolServiceLocator.PoolManager;
        if (poolManager != null)
        {
            var obj = poolManager.BorrowOrInstantiate(myPrefab);
        }
        
        // 獲取場景生命週期管理器
        var sceneManager = PoolServiceLocator.SceneLifecycleManager;
        if (sceneManager != null)
        {
            sceneManager.ResetReload(gameObject);
        }
        
        // 獲取Transform重置管理器
        var transformManager = PoolServiceLocator.TransformResetManager;
        if (transformManager != null)
        {
            var transformData = transformManager.CaptureTransformData(transform);
        }
    }
}
```

#### 服務註冊（通常由系統自動處理）
```csharp
// PoolManager 會在 Awake 時自動註冊
PoolServiceLocator.RegisterPoolManager(this);

// 手動註冊服務（如果需要）
PoolServiceLocator.RegisterSceneLifecycleManager(customManager);
```

### 介面抽象系統

系統使用介面抽象來提供更好的解耦和擴展性：

#### 核心介面

**IPoolManager**：池管理器介面
```csharp
public interface IPoolManager
{
    GameObject BorrowOrInstantiate(GameObject obj, Vector3 position = default, 
        Quaternion rotation = default, Transform parent = null, 
        Action<PoolObject> handler = null);
    void ReturnToPool(PoolObject poolObject);
    void ReCalculatePools();
}
```

**IPoolableObject**：可池化物件介面
```csharp  
public interface IPoolableObject
{
    PoolObject OriginalPrefab { get; }
    bool IsFromPool { get; }
    void ReturnToPool();
}
```

**ISceneLifecycleManager**：場景生命週期管理介面
```csharp
public interface ISceneLifecycleManager
{
    void PreparePoolObjectImplementation(PoolObject obj);
    void ResetReload(GameObject root);
    void OnBeforeDestroyScene(Scene scene);
}
```

#### 使用介面的好處
- **降低耦合**：組件間通過介面通信，不依賴具體實作
- **便於測試**：可以輕鬆建立Mock物件進行單元測試
- **提升擴展性**：可以提供不同的實作而不影響使用者程式碼

### 自定義池行為

#### 實作自定義池物件
```csharp
public class CustomPoolObject : PoolObject, IPoolObject
{
    [Header("自定義設置")]
    public float customValue;
    public ParticleSystem particles;
    
    public void PoolOnPrepared(PoolObject poolObj)
    {
        // 從池取出時的初始化
        particles.Clear();
        particles.Play();
    }
    
    public void PoolBeforeReturnToPool()
    {
        // 回到池前的清理
        particles.Stop();
    }
    
    public void PoolOnReturnToPool()
    {
        // 回到池時的最終清理
        customValue = 0;
    }
}
```

---

## 編輯器工具

### PoolPrewarmData 編輯器

1. **建立預熱資料**：
   - 右鍵 -> Create -> Boa -> PoolManager -> Create PoolPrewarmData

2. **編輯器按鈕功能**：
   - `OpenAndSavePreWarmPrefabs`: 批量處理預熱資料中的預製體

### PoolBank 編輯器

1. **自動尋找預熱資料**：
   - `FindOrCreatePoolPrewarmData`: 自動建立或尋找對應場景的預熱資料

### 除錯工具

#### 系統完整性驗證
```csharp
// 在 Editor 中驗證池系統完整性
[MenuItem("Tools/Pool System/Validate System Integrity")]
public static void ValidateSystem()
{
    PoolManager.Instance.ValidateSystemIntegrity();
}

// 檢視保護物件報告
[MenuItem("Tools/Pool System/Log Protected Objects Report")]
public static void LogProtectedObjectsReport()
{
    if (PoolManager.Instance != null)
    {
        PoolLogger.LogInfo(PoolManager.Instance.GetSystemProtectedObjectsReport());
    }
}
```

#### 自定義Inspector控制
```csharp
// 自定義池物件控制器
[RequireComponent(typeof(PoolObject))]
public class MyPoolObjectController : MonoBehaviour
{
    private PoolObject poolObject;
    
    [Header("保護狀態控制")]
    public bool startProtected = false;
    
    void Awake()
    {
        poolObject = GetComponent<PoolObject>();
    }
    
    void Start()
    {
        if (startProtected)
        {
            poolObject.MarkAsProtected();
        }
    }
    
    [ContextMenu("Toggle Protection")]
    void ToggleProtection()
    {
        if (poolObject.IsProtected())
        {
            poolObject.MarkAsRecyclable();
            Debug.Log("物件已設為可回收");
        }
        else
        {
            poolObject.MarkAsProtected();
            Debug.Log("物件已受保護");
        }
    }
    
    [ContextMenu("Check Protection Status")]
    void CheckProtectionStatus()
    {
        string status = poolObject.IsProtected() ? "Protected" : "Recyclable";
        Debug.Log($"物件 {name} 當前狀態: {status}");
    }
}
```

---

## 最佳實踐

### 1. 效能優化

#### 預熱策略選擇指南

**🌟 推薦順序**：
1. **PoolBank 自動化**：適合 90% 的使用場景
2. **手動 PoolPrewarmData**：需要跨場景共享或精細控制時使用
3. **動態池調整**：運行時根據需求動態調整池大小

```csharp
// 方法一：PoolBank 自動化（推薦）
// 無需程式碼，只需在場景中放置 PoolBank 組件
// PoolBank 會自動在 EnterSceneAwake 時處理所有預熱邏輯

// 方法二：手動控制（進階）
public class LevelManager : MonoBehaviour
{
    public PoolPrewarmData levelPrewarmData;
    
    void Start()
    {
        // 設置池預熱
        PoolManager.Instance.SetPrewarmData(levelPrewarmData, this);
        
        // 預熱全域物件
        PoolManager.Instance.PrepareGlobalPrewarmData();
        
        // 重新計算池大小
        PoolManager.Instance.ReCalculatePools();
    }
}
```

**性能考量**：
- ✅ PoolBank 會在場景喚醒時自動處理預熱，無需手動介入
- ✅ 配合 MonoPoolObj 可獲得完整的生命週期管理
- ✅ Protected 物件會被自動保護不被回收
- ✅ 自動統計和監控池使用狀況

**使用場景對照表**：

| 需求 | PoolBank 自動化 | 手動 PoolPrewarmData |
|------|:--------------:|:------------------:|
| 單一場景池管理 | ✅ 推薦 | ⚠️ 過度複雜 |
| 跨場景共享池 | ❌ 不支援 | ✅ 適合 |
| 運行時調整 | ❌ 有限 | ✅ 靈活 |
| 初學者使用 | ✅ 簡單 | ❌ 複雜 |
| 維護成本 | ✅ 極低 | ⚠️ 中等 |

#### 批量操作
```csharp
// 批量歸還物件以提高效能
public void ClearLevel()
{
    // 使用場景歸還所有物件
    var currentScene = SceneManager.GetActiveScene();
    PoolManager.Instance.ReturnAllObjects(currentScene);
}
```

### 2. 記憶體管理

#### 適當的池大小設置
```csharp
// 在 PoolPrewarmData 中設置合理的最大數量
// 避免過度預熱造成記憶體浪費
public class PoolSizeController : MonoBehaviour
{
    void ConfigurePoolSizes()
    {
        // 常見物件：10-20個
        // 特效物件：5-10個  
        // UI物件：2-5个
        // 敵人物件：依關卡設計決定
    }
}
```

### 3. 除錯和監控

#### 日誌設置
```csharp
public class PoolDebugger : MonoBehaviour
{
    [Header("除錯設置")]
    public bool enablePoolLogging = true;
    
    void Start()
    {
        if (enablePoolLogging)
        {
            // PoolLogger 會自動記錄池操作
            PoolLogger.LogInfo("Pool debugging enabled");
        }
    }
    
    [ContextMenu("Show Pool Status")]
    void ShowPoolStatus()
    {
        foreach (var pool in PoolManager.Instance.allPools)
        {
            PoolLogger.LogPoolStatus(
                pool._prefab.name,
                pool.TotalObjectCount,
                pool.InUseObjectCount, 
                pool.AvailableObjectCount
            );
        }
    }
}
```

### 4. 錯誤處理

#### 優雅的錯誤處理
```csharp
public class SafePoolUser : MonoBehaviour
{
    public PoolObject prefab;
    
    void SpawnObject()
    {
        // 安全的物件借用
        if (prefab == null)
        {
            PoolLogger.LogError("Prefab is null", this);
            return;
        }
        
        var obj = PoolManager.Instance.BorrowOrInstantiate(prefab.gameObject);
        if (obj == null)
        {
            PoolLogger.LogError("Failed to borrow object", this);
            return;
        }
        
        // 使用物件...
    }
}
```

---

## 疑難排解

### 常見問題

#### Q: 物件沒有正確歸還到池
**A:** 檢查以下幾點：
1. 物件是否有 `PoolObject` 組件
2. 是否調用了 `ReturnToPool()` 方法
3. 物件是否被設置為保護狀態

```csharp
// 除錯代碼
if (!poolObject.IsFromPool)
{
    PoolLogger.LogError("物件不是來自池", poolObject);
}

if (poolObject.IsProtected())
{
    PoolLogger.LogWarning("物件受到保護，無法歸還", poolObject);
}
```

#### Q: 池物件的 Transform 沒有正確重置
**A:** 確保使用了正確的設置方法：

```csharp
// 正確的方式
poolObject.OverrideTransformSetting(position, rotation, parent, scale);
poolObject.TransformReset();

// 錯誤的方式 - 直接修改 Transform
// transform.position = newPosition; // 這樣不會記錄到池系統
```

#### Q: 記憶體洩漏問題
**A:** 檢查以下幾點：
1. 確保在場景切換時清理池
2. 檢查是否有循環引用
3. 確保事件監聽器被正確移除

```csharp
// 場景切換時清理
void OnDestroy()
{
    // PoolManager 會自動清理，但確保自定義清理也執行
    // 移除事件監聽器以防止記憶體洩漏
    // 如果有使用事件系統，確保正確移除監聽器
}
```

### 效能問題

#### Q: 池系統影響幀率
**A:** 優化建議：
1. 減少預熱數量
2. 使用批量操作
3. 避免頻繁的池重計算
4. 使用 PoolLogger 監控效能問題

#### Q: SceneLifecycleManager 找不到類型
**A:** 確保引用正確的命名空間：
```csharp
using MonoFSM.Runtime; // SceneLifecycleManager 在這個命名空間中
```

#### Q: 服務定位器返回 null
**A:** 檢查以下幾點：
1. PoolManager 是否已經初始化
2. 服務是否已經正確註冊
```csharp
if (!PoolServiceLocator.IsPoolManagerAvailable)
{
    PoolLogger.LogError("池管理器服務不可用");
}
```

```csharp
// 優化的批量操作
public class OptimizedSpawner : MonoBehaviour
{
    public PoolObject prefab;
    private List<PoolObject> tempList = new List<PoolObject>();
    
    void SpawnBatch(int count)
    {
        tempList.Clear();
        
        // 批量借用
        for (int i = 0; i < count; i++)
        {
            var obj = PoolManager.Instance.BorrowOrInstantiate(prefab.gameObject);
            if (obj != null)
            {
                var poolObj = obj.GetComponent<PoolObject>();
                if (poolObj != null)
                {
                    tempList.Add(poolObj);
                }
            }
        }
        
        // 批量設置保護狀態
        foreach (var obj in tempList)
        {
            obj.MarkAsProtected();
        }
    }
}
```

---

## API 參考

### PoolManager 主要方法

| 方法 | 說明 | 參數 |
|------|------|------|
| `BorrowOrInstantiate` | 借用或實例化物件 | `GameObject, Vector3, Quaternion, Transform, Action<PoolObject>` |
| `ReturnToPool` | 歸還物件到池 | `PoolObject` |
| `ReCalculatePools` | 重新計算所有池 | 無 |
| `ValidateSystemIntegrity` | 驗證系統完整性 | 無 |

### PoolObject 主要方法

| 方法 | 說明 | 參數 |
|------|------|------|
| `ReturnToPool` | 歸還到池 | 無 |
| `MarkAsProtected` | 設置為保護狀態 | 無 |
| `MarkAsRecyclable` | 設置為可回收狀態 | 無 |
| `IsProtected` | 檢查是否受保護 | 無 |
| `TransformReset` | 重置Transform | 無 |

### PoolObject 保護方法

| 方法 | 說明 | 參數 |
|------|------|------|
| `MarkAsProtected` | 設置為保護狀態 | 無 |
| `MarkAsRecyclable` | 設置為可回收狀態 | 無 |
| `IsProtected` | 檢查是否受保護 | 無 |
| `IsRecyclable` | 檢查是否可回收 | 無 |

### 輔助系統 API

#### PoolLogger 日誌方法

| 方法 | 說明 | 參數 |
|------|------|------|
| `LogInfo` | 記錄資訊 | `string, Object` |
| `LogWarning` | 記錄警告 | `string, Object` |
| `LogError` | 記錄錯誤 | `string, Exception, Object` |
| `LogPoolOperation` | 記錄池操作 | `string, string, Object` |

#### SceneLifecycleManager 方法 (MonoFSM.Runtime 命名空間)

| 方法 | 說明 | 參數 |
|------|------|------|
| `PreparePoolObjectImplementation` | 準備池物件實作 | `PoolObject` |
| `ResetReload` | 場景重置和重新載入 | `GameObject` |
| `OnBeforeDestroyScene` | 場景銷毀前清理 | `Scene` |

#### TransformResetHelper 方法

| 方法 | 說明 | 參數 |
|------|------|------|
| `ResetTransform` | 重置Transform | `Transform, TransformData` |
| `CaptureTransformData` | 捕捉Transform數據 | `Transform` |
| `SetupTransform` | 設置Transform | `Transform, Vector3, Quaternion, Vector3, Transform` |

#### PoolBank 方法

| 方法 | 說明 | 參數 |
|------|------|------|
| `FindOrCreatePoolPrewarmData` | 自動尋找或創建預熱資料 | 無 |
| `FindPoolPrewarmDataFor` | 為指定 PoolBank 尋找預熱資料 | `PoolBank` |
| `FindGlobalPrewarmData` | 尋找全域預熱資料 | 無 |
| `OnBeforeSceneSave` | 場景保存前的處理 | 無（自動調用）|
| `EnterSceneAwake` | 場景喚醒時的自動處理 | 無（自動調用）|

#### MonoPoolObj 相關介面（由 PoolBank 自動處理）

| 介面 | 說明 |
|------|------|
| `ISceneAwake` | 場景喚醒處理 |
| `IResetStateRestore` | 狀態重置還原 |
| `ISceneStart` | 場景開始處理 |

---

## 總結

這個重構後的 PoolObject/PoolManager 系統提供了：

✅ **更清晰的架構**：分離關注點，每個組件職責明確  
✅ **更好的效能**：智能池管理和優化的記憶體使用  
✅ **更強的擴展性**：介面抽象和服務定位器模式  
✅ **更容易除錯**：統一日誌系統和完整的監控工具  
✅ **更安全的操作**：物件保護系統和錯誤處理  

通過遵循本手冊的指導，你可以有效地使用這個簡化後的池系統來管理遊戲中的動態物件，提高效能並簡化開發流程。

### 系統簡化說明

本版本已經簡化了原本複雜的保護系統，移除了過於複雜的功能，保持系統的簡潔性和可維護性。主要改進包括：

- **簡化保護狀態**：只保留 Protected/Recyclable 二元狀態
- **模組化架構**：分離關注點，提升可維護性
- **介面抽象**：降低耦合度，提升擴展性
- **統一日誌**：使用 PoolLogger 進行一致的日誌記錄

---
*最後更新：2025年*  
*版本：3.0 （簡化版）*