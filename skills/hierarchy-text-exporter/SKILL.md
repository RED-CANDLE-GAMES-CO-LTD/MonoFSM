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

## 輸出格式 spec

### Node 行

```
[縮排2空格][flags]Name [transform] [<components>] [(prefab:res:路徑)]
```

- **flags**：`~` = inactive GameObject、`+` = prefab instance 新增的 GameObject（`IsAddedGameObjectOverride`）
- **transform**：只輸出非 identity 的 local transform；`p=(x,y,z)`、`r=(x,y,z)`（localEulerAngles）、`s=0.5`（等比例縮寫）或 `s=(x,y,z)`；數字整數不帶小數點，float 最多 3 位小數（`0.###`）
- root 是 prefab asset 時，第一行輸出 `# prefab: res:路徑`；node 本身是巢狀 prefab instance root 時，行尾附 `(prefab:res:路徑)`（`IsAnyPrefabInstanceRoot` + `GetPrefabAssetPathOfNearestInstanceRoot`）

### Component 區塊

```
<CompA f1=v1 f2=v2 | CompB | -CompC f=v>
```

- 用 component 型別的短名（`Type.Name`）；` | ` 分隔多個 component
- `-` 前綴 = component 目前 disabled（`Behaviour.enabled` / `Collider.enabled` / `Renderer.enabled` 為 false）
- `+` 前綴 = prefab instance 新增的 component（`IsAddedComponentOverride`）
- 只列出「非預設值」的欄位（`_excludeDefaults`，用 `ComponentDefaultCache` 判斷）；欄位若是 prefab override（`SerializedProperty.prefabOverride && !isDefaultOverride`）則欄位名後加 `*`（例：`mass*=10`），override 欄位即使等於型別預設值也會被列出
- 沒有任何欄位輸出時只寫型別名（不含 `<>`）
- Transform 元件不進 `<>`（已經用 node 行的 `p=/r=/s=` 表示）
- missing script（GetComponents 回傳的 null 元件）輸出 `<!MissingScript>`
- `_showOverridesOnly=true` 時只輸出 prefab override 欄位，忽略 `_excludeDefaults`
- 單一 component 的欄位文字總量超過 `_maxFieldCharsPerComponent`（預設 400）時，從超標處截斷並補 `…(+N more)`（N = 被截掉的欄位數；ParticleSystem 這類密集設定的 component 常見）
- 巢狀 serializable（`{...}`）內的子欄位也會逐一跟 component 預設值比對過濾——整包 Generic 不會因為一個子欄位有改就全展開；過濾後全空的欄位（`{}`）整欄略過

### 值格式化（CompactValueFormatter）

| 型別 | 格式 | 範例 |
|---|---|---|
| bool true | 裸欄位名（不接 `=值`） | `trigger` |
| bool false | `name=off` | `trigger=off` |
| int/float | 整數不帶小數點，float 最多 3 位小數 | `mass=10`、`speed=1.5` |
| string | `"..."`，超過 `_maxStringLength` 截斷加 `…` | `"Hello…"` |
| enum | 裸名稱（不加引號） | `state=Idle` |
| Color | `#RRGGBB`；a<1 時 `#RRGGBBAA` | `color=#FF0000` |
| Vector2/3/4、Vector2Int/3Int | `(x,y,z)` | `offset=(1,0,0)` |
| LayerMask | 命中的 layer 名以 `,` 連接；沒命中任何 layer 為 `none` | `mask=Default,Player` |
| AnimationCurve | `curve(N keys)` | `curve=curve(4 keys)` |
| Quaternion | euler `(x,y,z)` | `rot=(0,90,0)` |
| Bounds/BoundsInt | `bounds(center,size)` | `m_AABB=bounds((0,1,0),(2,2,2))` |
| Rect/RectInt | `rect(x,y,w,h)` | `r=rect(0,0,100,50)` |
| Gradient | `gradient`（不展開細節） | `g=gradient` |
| Character | `'c'` | `sep=','` |
| Hash128 | hash 字串 | — |
| fixed buffer / 無法解讀的型別 | `…`（絕不輸出 `SerializedProperty.ToString()` 的 `UnityEditor.SerializedProperty`） | `data=…` |
| Asset reference（樹外、persistent） | `res:路徑`（去掉 `Assets/` 前綴，`Packages/` 保留） | `mat=res:Materials/Red.mat` |
| 樹內 reference（root 子樹內的 GameObject/Component） | `@相對路徑`（相對「目前 node」；自己是 `@.`）；欄位宣告型別與實際 instance 型別不同時附 `#Type` | `target=@../Hand`、`action=@State/Idle#JumpAction` |
| 樹外 scene reference（非 persistent、不在子樹內） | `@/場景絕對路徑` | `other=@/World/Enemy` |
| null | 只有「預設非 null 卻被改成 null」才輸出 `name=null`；其餘 null 一律因為等於預設值被跳過 | `target=null` |
| 陣列/List | `[a,b,c]`；元素數超過 `_maxArrayElements` → `[N items: a,b,…]`；全部元素相同（>2 個）→ `[N×值]` | `items=[1,2,3]`、`m_Planes=[6×null]` |
| 巢狀 serializable（Generic，非 array） | `{f1=v1,f2=v2}`；深度超過 `_maxNestedDepth` → `{…}` | `range={min=0,max=10}` |

### 摺疊摘要行（已知子樹）

```
[縮排][flags]Name <主Component> :: 摘要 (+N nodes)
```

3 個內建 summarizer（`SubtreeSummarizerRegistry`，依 `Priority` 排序，`CanSummarize` 命中第一個生效）：

```
States <StateFolder> :: 5 states: Idle, Walk, Run, Attack, Die (+42 nodes)
Vars <VariableFolder> :: 6 vars: HP:VarFloat, Speed:VarFloat (+6 nodes)
Hurt <EffectDetectable> :: 3 receivers: Damage, Knockback, Stun (+11 nodes)
```

- 超過 8 個項目時列前 8 個 + `…`
- `StateFolder` → direct children 找 `MonoStateBehaviour`，名稱用 `CleanName`（去除 `[Tag] ` 前綴）
- `VariableFolder` → `GetComponentsInChildren<AbstractMonoVariable>` 過濾出「父層第一個 VariableFolder 是自己」的，格式 `Name:TypeName`
- `EffectDetectable` → `GetComponentsInChildren<GeneralEffectReceiver>`，只列名稱

### 純 Transform 骨架摺疊（`_foldBareTransformChains`，預設開）

某節點整棵子樹（含自己）的所有節點都「只有 Transform、無其他 component」且有子節點時，摺成一行：

```
DEF-spine p=(0,-0.001,0.005) r=(80.945,180,180) :: bones/transform-only (+6 nodes)
```

節點本身的非 identity transform 照常輸出。rig bones（DEF-/MCH- 骨架鏈）是主要受益者。root 節點（depth 0）不摺疊；命中 `_expandPaths` 時不摺疊。

通用 fallback：
- `_foldKnownSubtrees=false` 或 node 路徑命中 `_expandPaths` → 不摺疊，正常展開
- 超過 `_maxDepth`（且未被 `_expandPaths` 強制展開）→ 摺成 `Name (+N nodes)`（無 summarizer 時的純深度摺疊）
- `_includeInactive=false` 時，inactive 子樹摺成 `~Name (+N nodes)`
- 同層子節點超過 `_maxChildrenPerNode` → 列出前 N 個，其餘顯示 `… (+M more siblings)`

## HierarchyExportOptions 欄位

| 欄位 | 預設 | 說明 |
|---|---|---|
| `_maxDepth` | -1 | -1 = 不限深度 |
| `_maxChildrenPerNode` | 30 | 同層子節點上限 |
| `_excludeDefaults` | true | 排除等於型別預設值的欄位 |
| `_foldKnownSubtrees` | true | 是否啟用已知子樹摺疊 |
| `_foldBareTransformChains` | true | 整棵子樹只有 Transform 時摺成一行（rig bones） |
| `_expandPaths` | [] | root 相對路徑清單；尾端 `/*` = 整棵展開；單一 `*` = 全展開 |
| `_expandDepthOverride` | -1 | 保留欄位，目前未影響展開邏輯本身之外的行為 |
| `_includeComponents` | [] | 空 = 全部；短名或 FullName，含子類（用 assignable 比對） |
| `_excludeComponents` | [] | 同上，排除用 |
| `_showOverridesOnly` | false | 只列 prefab override 欄位，忽略 `_excludeDefaults` |
| `_markOverrides` | true | override 欄位名後加 `*` |
| `_includeInactive` | true | false 時 inactive 子樹摺成一行 |
| `_maxStringLength` | 60 | string 截斷長度 |
| `_maxArrayElements` | 8 | 陣列展開元素上限 |
| `_maxNestedDepth` | 2 | 巢狀 serializable 展開深度上限 |
| `_maxFieldCharsPerComponent` | 400 | 單一 component 欄位文字總量上限，超過補 `…(+N more)`；<=0 不限 |

`HierarchyExportOptions.Default` / `HierarchyExportOptions.FullExpand`（`_foldKnownSubtrees=false`、`_foldBareTransformChains=false`、`_maxChildrenPerNode=int.MaxValue`、`_maxFieldCharsPerComponent=0`）為兩個常用預設。

展開判定（`IsForcedExpand(path)`）：node 相對路徑 `path` 被視為強制展開，若任一 `_expandPaths` 項目 `e` 符合：`e == "*"`、`path == e`、`e` 以 `/*` 結尾且 `path` 落在該子樹下、或 `e` 是 `path` 的後代路徑（讓祖先自動展開以顯示後代的展開路徑）。

## 常見擴充

- **新增 summarizer**：實作 `ISubtreeSummarizer`（`Priority`/`CanSummarize`/`Summarize`），在 `SubtreeSummarizerRegistry` 靜態建構子或外部呼叫 `SubtreeSummarizerRegistry.Register(new XxxSummarizer())`
- **新增值格式化型別**：在 `CompactValueFormatter.FormatValue` 的 switch 補上新 `SerializedPropertyType` case
- **Component 篩選**：用 `HierarchyExportOptions._includeComponents` / `_excludeComponents` 傳短名或 FullName（`MatchesAny` 會用 `IsAssignableFrom` 涵蓋子類）
- **無法 AddComponent 的型別**：`ComponentDefaultCache` 每個型別用獨立子物件建預設 instance（避免 Renderer 互斥 / DisallowMultipleComponent）；`ParticleSystemRenderer` 已特例處理（先加 `ParticleSystem` 再抓）。若有其他「只能跟宿主 component 一起存在」的型別走了 heuristic 導致預設欄位全印，比照在 `GetDefault` 加特例

## 與 UnityTypeFormatter 的關係

`UnityTypeFormatter.FormatUnityObject` 原本有 FIXME：只要 `AssetDatabase.GetAssetPath` 非空就當成外部 asset，導致同一份 prefab 內的 node/component reference 被誤判成 `ExtResource`。已修正為：非 persistent → NodePath；persistent 但與目前序列化的 container 同一個 asset path → 也當 NodePath；其餘才是 ExtResource（透過內部 overload 帶 `containerAssetPath` 參數，對外的單參數簽名不變）。

`HierarchyTextExporter` / `CompactValueFormatter` 是完全獨立的新格式（`res:` / `@相對路徑` / `@/場景路徑`），不呼叫 `UnityTypeFormatter.FormatUnityObject`，但邏輯上做的是同一件事（區分樹內 reference vs 外部 asset vs 樹外 scene reference），只是輸出風格不同、且有 root-subtree 資訊可用，判斷更準確。
