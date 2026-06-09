---
name: prefab-text-exporter
description: Unity Prefab 文字匯出工具的設計指南。當需要：(1) 理解或修改 PrefabToTextExporter 相關程式碼 (2) 擴充匯出格式或新增支援的類型 (3) 新增 Component 類型對應 (4) 調整篩選邏輯或輸出格式時使用此 skill。
---

# Prefab Text Exporter

將 Unity Prefab 轉換為 Godot `.tscn` 風格的純文字格式，方便 LLM 閱讀理解場景結構。

## 設計理念

- **單向輸出**：只匯出文字，不需要反向轉換回 Prefab
- **LLM 友善**：輸出格式易於理解，包含階層結構和欄位資訊
- **篩選機制**：可排除預設值、指定 Component 類型、排除特定欄位

## 檔案結構

```
MonoFSM/1_MonoFSM_Core/Editor/PrefabExporter/
├── UnityTypeFormatter.cs      # 類型格式化（Vector, Color, Object Reference 等）
├── PrefabExportSettings.cs    # 設定資料結構 + EditorPrefs 持久化
├── PrefabToTextExporter.cs    # 核心轉換邏輯（遍歷 GameObject、比較預設值）
├── PrefabToTextContextMenu.cs # 右鍵選單（Assets/MonoFSM/複製 Prefab 為文字）
└── PrefabToTextWindow.cs      # OdinEditorWindow 進階視窗
```

## 輸出格式

```
[gd_scene format=3 uid="unity_prefab"]

[node name="Player" type="RigidBody3D"]
position = Vector3(0, 1, 0)
# Component: Rigidbody
mass = 10

[node name="Arm" type="MeshInstance3D" parent="."]
position = Vector3(0.5, 0, 0)
# Component: MeshRenderer
material = ExtResource("Assets/Materials/Mat.mat")

[node name="Hand" type="Node3D" parent="Arm"]
scale = Vector3(0.5, 0.5, 0.5)
```

## 類型格式化規則 (UnityTypeFormatter)

| Unity 類型 | 輸出格式 |
|-----------|---------|
| Vector3 | `Vector3(x, y, z)` |
| Quaternion | `Vector3(euler.x, euler.y, euler.z)` |
| Color | `Color(r, g, b, a)` |
| Asset Reference | `ExtResource("Assets/path/to/file.ext")` |
| Scene Object | `NodePath("Parent/Child")` |
| Enum | `"EnumValueName"` |
| Array | `[item1, item2, ...]` |

### 新增類型支援

在 `FormatValue()` 的 switch expression 中新增：

```csharp
return value switch
{
    // 新增自訂類型
    MyCustomType custom => $"Custom({custom.field1}, {custom.field2})",
    // ... existing cases
};
```

## 節點類型對應 (DetermineNodeType)

根據 GameObject 上的 Component 決定 Godot 節點類型：

| Unity Component | Godot Type |
|----------------|------------|
| Camera | Camera3D |
| Rigidbody | RigidBody3D |
| CharacterController | CharacterBody3D |
| MeshRenderer | MeshInstance3D |
| Collider | CollisionShape3D |
| Animator | AnimationPlayer |
| Canvas | CanvasLayer |
| (default) | Node3D |

### 新增對應

在 `PrefabToTextExporter.DetermineNodeType()` 中新增：

```csharp
if (go.GetComponent<MyComponent>()) return "MyGodotType";
```

## 預設值比較機制

使用臨時 GameObject 建立 Component 預設實例，快取預設值後比較：

```csharp
// 快取結構
Dictionary<Type, Dictionary<string, object>> _defaultCache

// 比較流程
1. GetDefaultValues(componentType) - 取得或建立預設值快取
2. IsDefaultValue(property, defaults) - 比較當前值與預設值
3. 若相同則跳過輸出
```

## 使用方式

### 右鍵快速複製
Project 視窗選擇 Prefab → 右鍵 → `MonoFSM/複製 Prefab 為文字`

### 進階視窗
`Tools → MonoFSM → Prefab Text Exporter`
- 可調整 Component 篩選
- 可設定欄位排除規則
- 即時預覽輸出結果
- 設定自動儲存到 EditorPrefs

## 常見擴充需求

### 新增排除欄位
在 `PrefabExportSettings._excludedFieldNames` 中加入欄位名稱。

### 自訂輸出格式
修改 `PrefabToTextExporter.TraverseGameObject()` 中的 StringBuilder 輸出。

### 支援新的 SerializedPropertyType
在 `UnityTypeFormatter.FormatPropertyValue()` 的 switch 中新增 case。
