# Hierarchy 匯出格式 spec

## 輸出格式 spec

### Node 行

```
[縮排2空格][flags]Name [transform] [<components>] [(prefab:res:路徑)] [   # note]
```

- **flags**：`~` = inactive GameObject、`+` = prefab instance 新增的 GameObject（`IsAddedGameObjectOverride`）
- **transform**：只輸出非 identity 的 local transform；`p=(x,y,z)`、`r=(x,y,z)`（localEulerAngles）、`s=0.5`（等比例縮寫）或 `s=(x,y,z)`；數字整數不帶小數點，float 最多 3 位小數（`0.###`）
- root 是 prefab asset 時，第一行輸出 `# prefab: res:路徑`；node 本身是巢狀 prefab instance root 時，行尾附 `(prefab:res:路徑)`（`IsAnyPrefabInstanceRoot` + `GetPrefabAssetPathOfNearestInstanceRoot`）
- **note**（`NoteText.NodeSuffix`）：節點上 component 的 `_note`（`AbstractDescriptionBehaviour` / `AbstractSOConfig`）或 `Note` 的舊 `note` 欄位，攤成單行掛在行尾當註解。節點名多半自動命名（`[Action] Stamina 電力 += 2`），看得出做什麼、看不出為什麼，why 只寫在 note 裡。同節點多個 component 各有 note 時標出型別：`# Note: xxx | HpHandler: yyy`。長度上限 `_maxNoteLength`。
  - `_note` / `note` 因此**不再出現在 component 欄位堆裡**（避免被 `_maxFieldCharsPerComponent` 截掉，那是掃階層時最該一眼看到的東西）

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

摺疊行的尾巴（`FoldTail`，含 summarizer 摺疊與深度摺疊）：`(+N nodes, M notes)` + 節點自己的 `# note`。
`M notes` 是**子樹裡（不含自己）**的 note 數 —— 沒有它，讀的人無從判斷這個 `(+N nodes)` 值不值得下鑽。
純 Transform 骨架摺疊不帶 note 數（整棵只有 Transform，不可能有 note）。
- 同層子節點超過 `_maxChildrenPerNode` → 列出前 N 個，其餘顯示 `… (+M more siblings)`

