# FieldReference 系統使用指南

## 概述

FieldReference 系統是一個基於 ScriptableObject 的視覺化欄位存取解決方案，讓開發者可以在 Unity Inspector 中透過拖拉組合的方式建構 `GetVar(varName).GetField(fieldName).Get<float>()` 這樣的邏輯鏈，並保持 refactor 的維護性。

## 系統架構

### 核心組件

1. **FieldReference** - 表示單一欄位引用的 ScriptableObject
2. **ValueAccessChain** - 組合 VariableTag 和 FieldReference 鏈的 ScriptableObject  
3. **ChainedValueProvider** - 執行存取邏輯的 MonoBehaviour 組件
4. **TypeSafetyValidator** - 型別安全驗證系統

### 設計理念

- **Refactor-Safe**: 使用 PropertyInfo/FieldInfo 的 MetadataToken 作為唯一識別
- **型別安全**: 完整的型別推導和驗證系統
- **視覺化**: 在 Inspector 中透過拖拉組合建構邏輯
- **高效能**: 支援反射快取和值快取機制

## 快速開始

### 步驟 1: 建立 FieldReference

1. 在 Project 視窗右鍵選擇 `Create > RCG > Field Reference`
2. 設定來源類型 (Source Type)
3. 從下拉選單選擇欄位名稱
4. 系統會自動設定型別資訊和 MetadataToken

```csharp
// 範例：建立一個指向 ExamplePlayerData.health 的 FieldReference
```

### 步驟 2: 建立 ValueAccessChain

1. 在 Project 視窗右鍵選擇 `Create > RCG > Value Access Chain`
2. 設定變數標籤 (Variable Tag)
3. 新增存取步驟並指派 FieldReference
4. 驗證存取鏈

```csharp
// 範例存取路徑: Variable[PlayerData] -> weapon -> damage
// 結果: GetVar("PlayerData").weapon.damage
```

### 步驟 3: 使用 ChainedValueProvider

1. 在 GameObject 上新增 `ChainedValueProvider` 組件
2. 指派 ValueAccessChain
3. 設定變數提供者 (Variable Provider)
4. 驗證設定

```csharp
// 在程式碼中使用
float weaponDamage = chainedValueProvider.Get<float>();
```

## 詳細功能

### FieldReference 功能

- **動態欄位選擇**: 根據來源類型自動提供可用欄位清單
- **型別推導**: 自動推導欄位型別和相關資訊
- **Refactor-Safe**: 使用 MetadataToken 確保重構安全
- **驗證功能**: 即時驗證欄位引用有效性

### ValueAccessChain 功能

- **多步驟存取**: 支援巢狀物件的深層存取
- **陣列支援**: 支援陣列索引存取
- **型別驗證**: 每個步驟的型別相容性檢查
- **自動修復**: 嘗試修復常見的設定錯誤
- **預覽功能**: 編輯器中即時預覽存取路徑

### ChainedValueProvider 功能

- **IValueProvider 實作**: 與現有 Provider 系統無縫整合
- **快取機制**: 支援值快取以提升效能
- **錯誤處理**: 完整的錯誤檢查和報告
- **除錯工具**: 豐富的除錯和分析功能

### TypeSafetyValidator 功能

- **完整型別檢查**: 驗證整個存取鏈的型別安全性
- **相容性分析**: 評估型別之間的相容性和轉換可能性
- **建議系統**: 提供型別轉換和修復建議
- **效能分析**: 分析存取鏈的效能特徵

## 使用案例

### 案例 1: 取得玩家血量

```csharp
// 傳統方式
var player = GetVar("PlayerData").GetValue<PlayerData>();
float health = player.health;

// 使用 FieldReference 系統
// 1. 建立 FieldReference 指向 PlayerData.health
// 2. 建立 ValueAccessChain 組合 VariableTag + FieldReference
// 3. 在 Inspector 中配置 ChainedValueProvider
float health = healthProvider.Get<float>();
```

### 案例 2: 取得武器傷害

```csharp
// 傳統方式 
var player = GetVar("PlayerData").GetValue<PlayerData>();
float damage = player.weapon.damage;

// 使用 FieldReference 系統
// 存取路徑: PlayerData -> weapon -> damage
float damage = weaponDamageProvider.Get<float>();
```

### 案例 3: 取得技能名稱

```csharp
// 傳統方式
var player = GetVar("PlayerData").GetValue<PlayerData>();
string skillName = player.skills[0].name;

// 使用 FieldReference 系統  
// 存取路徑: PlayerData -> skills[0] -> name
string skillName = skillNameProvider.Get<string>();
```

## 型別安全性

### 自動型別檢查

系統會自動檢查以下項目：

- 相鄰步驟之間的型別相容性
- 陣列存取的有效性
- 最終輸出型別的正確性
- 變數提供者與存取鏈的匹配度

### 型別轉換支援

- 數值型別之間的轉換 (int ↔ float ↔ double)
- 字串轉換 (任何型別 → string)
- 繼承關係的轉換
- 介面轉換

### 錯誤報告

```csharp
// 範例錯誤報告
=== 型別安全性分析報告 ===

總體狀態: ✗ 發現錯誤
錯誤數量: 1
警告數量: 0

錯誤列表:
- 步驟 2 的輸入型別不相容。期望: WeaponData, 實際: PlayerData
  建議: 檢查存取路徑是否正確; 考慮使用中間步驟
```

## 效能考量

### 快取機制

1. **反射快取**: Expression Tree 編譯的高效能 getter
2. **值快取**: 可設定的值快取機制
3. **MetadataToken**: 避免字串比較的高效能引用

### 效能建議

- 對於頻繁存取的值啟用快取
- 避免過深的存取鏈（建議不超過 5 層）
- 在效能敏感的地方考慮快取最終結果

## 維護性

### Refactor-Safe 機制

1. **MetadataToken**: 使用反射的 MetadataToken 作為唯一識別
2. **自動欄位名稱同步**: 當透過 MetadataToken 找到重命名的欄位時，自動更新 `_fieldName`
3. **自動型別名稱同步**: MySerializedType 也支援 MetadataToken，可自動同步型別重命名
4. **回退機制**: 當 MetadataToken 失效時回退到名稱查找
5. **驗證工具**: 提供工具檢查和修復 refactor 後的問題
6. **批量同步**: 支援批量同步專案中所有相關引用

### 自動欄位名稱同步

**運作原理**：
- 每次透過 `GetMemberInfo()` 存取欄位時，系統會比較 MetadataToken 對應的實際欄位名稱與儲存的 `_fieldName`
- 如果發現不一致，會自動更新 `_fieldName` 並在編輯器中標記為 dirty
- 這確保了即使欄位被重新命名，FieldReference 仍然能正確工作並顯示最新的欄位名稱

**觸發時機**：
- 任何時候呼叫 `GetValue()` 或 `ValidateReference()` 
- 手動點擊 "驗證欄位引用" 按鈕
- 使用 "重新整理 MetadataToken 和欄位名稱" 按鈕

**批量同步**：
- FieldReference: `檢查欄位名稱同步` / `重新整理 MetadataToken 和欄位名稱`
- ValueAccessChain: `檢查欄位名稱同步狀態` / `同步所有欄位名稱`
- ChainedValueProvider: `檢查存取鏈同步狀態` / `同步存取鏈欄位名稱`

### 🆕 自動型別名稱同步（MySerializedType）

**運作原理**：
- MySerializedType 現在也支援 MetadataToken 機制
- 儲存型別的 MetadataToken、FullName 和 Assembly 資訊
- 每次存取型別時自動檢查是否有重命名，並同步更新

**觸發時機**：
- 存取 `RestrictType` 屬性時自動觸發
- 手動點擊 "驗證型別引用" 按鈕
- 使用 "重新整理型別 MetadataToken" 按鈕

**批量同步**：
- MySerializedType: `檢查型別名稱同步` / `重新整理型別 MetadataToken`
- VariableTag: `檢查型別同步狀態` / `同步所有型別引用`
- RefactorSafeManager: 提供全域批量同步功能

### 最佳實踐

- **重大 refactor 後**：使用各層級的同步功能確保所有引用都是最新的
- **日常開發**：系統會自動處理大部分的欄位重命名，無需手動干預
- **定期檢查**：使用檢查功能驗證所有引用的同步狀態
- **描述性命名**：使用描述性的 ScriptableObject 名稱便於識別

## 除錯工具

### Inspector 按鈕

- **驗證設定**: 檢查組件設定是否正確
- **顯示除錯資訊**: 顯示詳細的除錯資訊
- **產生型別安全性報告**: 分析型別安全性
- **檢查型別相容性**: 檢查當前值的型別相容性

### Console 輸出

系統提供詳細的 Console 輸出，包括：

- 驗證結果和錯誤訊息
- 型別相容性分析
- 效能統計
- 存取路徑追蹤

## 常見問題

### Q: 為什麼我的 FieldReference 變成無效？

A: 可能的原因：
1. 來源類別被重構或刪除
2. 欄位名稱或型別被修改
3. MetadataToken 過期

解決方法：
1. 使用 "重新整理 MetadataToken" 按鈕
2. 重新選擇欄位
3. 檢查來源類別是否仍然存在

### Q: 如何提升存取效能？

A: 優化建議：
1. 啟用值快取機制
2. 減少存取鏈深度
3. 使用適當的快取有效期間
4. 避免在 Update 中頻繁存取

### Q: 型別轉換失敗怎麼辦？

A: 檢查步驟：
1. 使用 "檢查型別相容性" 功能
2. 查看型別轉換建議
3. 考慮添加中間轉換步驟
4. 檢查資料是否為預期格式

## 進階用法

### 自訂型別轉換

```csharp
// 在 ChainedValueProvider 中添加自訂轉換邏輯
public T GetWithCustomConversion<T>()
{
    var value = GetValueInternal();
    
    // 自訂轉換邏輯
    if (typeof(T) == typeof(MyCustomType) && value is SomeOtherType other)
    {
        return (T)(object)ConvertToMyCustomType(other);
    }
    
    return Get<T>();
}
```

### 動態存取鏈

```csharp
// 程式碼中動態建立存取鏈
var accessChain = ScriptableObject.CreateInstance<ValueAccessChain>();
accessChain.SetVariableTag(playerVariableTag);
accessChain.AddAccessStep(healthFieldReference);
chainedValueProvider.SetAccessChain(accessChain);
```

## 總結

FieldReference 系統提供了一個強大且易用的解決方案，讓開發者可以在 Unity Inspector 中視覺化地組合複雜的值存取邏輯，同時保持 refactor-safe 和型別安全。透過完善的驗證和除錯工具，開發者可以輕鬆地建立和維護複雜的資料存取鏈。