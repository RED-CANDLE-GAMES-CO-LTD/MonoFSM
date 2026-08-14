---
name: hierarchy-text-exporter
description: Hierarchy → 精簡結構化文字匯出工具（HierarchyTextExporter）。當需要：(1) 讓 LLM 用最少 token 讀懂 Unity GameObject 階層與 Component serialized 欄位 (2) 匯出 prefab 或 scene 子樹成精簡文字（uloop execute-dynamic-code 呼叫或右鍵複製）(3) 理解/修改 HierarchyTextExporter 相關程式碼、摺疊摘要規則、值格式化規則時使用此 skill。
---

# Hierarchy Text Exporter

把一個 GameObject 子樹（prefab asset 或 scene 物件）匯出成**縮排樹 + inline 欄位**的精簡文字，給 LLM 讀懂用。
Editor only、單向匯出（不 round-trip 回 GameObject）。已知子樹（StateFolder / VariableFolder / EffectDetectable）預設摺疊成一行摘要，避免爆量；`_expandPaths` 可指定要展開的路徑。

不要跟 `prefab-text-exporter` skill（Godot tscn 風格、`PrefabToTextExporter`）搞混——那是另一套舊工具，round-trip 導向的欄位級輸出，兩者互不影響、都保留著。

## 檔案結構

```
MonoFSM/1_MonoFSM_Core/Editor/PrefabExporter/HierarchyText/
├── HierarchyExportOptions.cs   # 選項 POCO（摺疊/展開/篩選/長度上限）
├── ComponentDefaultCache.cs    # Component 預設值快取（DataEquals + heuristic fallback）
├── CompactValueFormatter.cs    # SerializedProperty → 精簡值文字
├── HierarchyTextExporter.cs    # 核心遍歷、node 行組裝、Export/ExportToFile 靜態 API
│                               #（note 抽取在上一層 ../NoteText.cs，FSM 匯出與 up refs 共用）
├── SubtreeSummarizers.cs       # ISubtreeSummarizer + registry + 3 個內建 summarizer
└── HierarchyTextContextMenu.cs # 右鍵選單入口
```

## 呼叫方式（uloop execute-dynamic-code）

```csharp
var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/0_Gameplay/0_Base/PPlayer.prefab");
var opt = new MonoFSM.Editor.HierarchyExportOptions();
return MonoFSM.Editor.HierarchyTextExporter.Export(go, opt);
```

展開特定子樹（例如 States 資料夾整棵展開）：

```csharp
var opt = new MonoFSM.Editor.HierarchyExportOptions();
opt._expandPaths.Add("States/*");
return MonoFSM.Editor.HierarchyTextExporter.Export(go, opt);
```

全展開版（不摺疊任何已知子樹）：

```csharp
return MonoFSM.Editor.HierarchyTextExporter.Export(go, MonoFSM.Editor.HierarchyExportOptions.FullExpand);
```

寫檔版（回傳 `"written {chars} chars to {absolutePath}"`）：

```csharp
return MonoFSM.Editor.HierarchyTextExporter.ExportToFile(go);
// 預設路徑 "Temp/HierarchyExport/{root.name}.txt"，可傳第三參數自訂路徑
```

右鍵選單：Hierarchy/Project 選取 GameObject 後 `GameObject/MonoFSM/複製精簡階層文字`（或「(完整展開)」版本）；Inspector 上 Transform 元件右鍵 `CONTEXT/Transform/複製精簡階層文字`。

## References

- `references/format-spec.md` — 輸出格式完整 spec（node 行、component 區塊、值格式化表、摺疊摘要、純 Transform 骨架摺疊）。**要讀懂匯出結果的每個符號、或除錯匯出結果不如預期時**讀它。
- `references/extending.md` — `HierarchyExportOptions` 全欄位表、常見擴充做法、與 `UnityTypeFormatter` 的關係。**要改這個工具的程式碼、加新的 summarizer 或新的型別支援時**讀它。
