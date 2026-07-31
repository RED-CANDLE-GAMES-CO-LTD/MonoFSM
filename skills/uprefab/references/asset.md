# asset —— 建立與編輯 ScriptableObject（要 Unity 開著）

`prefab do` / `scene do` 改的是「掛在節點上的 component」，`up asset` 改的是**獨立存在、
不掛在任何節點上的 ScriptableObject asset**（registry / config 類資料，例如
`PromptIconRegistry`、`DeviceIconMapConfig`）。跟 `prefab do` 同一個風格，走同一套
`EditResolve`（路徑/型別/欄位解析與錯誤訊息）。

```bash
up asset create <TypeName> <assetPath> [--overwrite]        # 建一個 ScriptableObject asset
up asset set <assetPath> <fieldPath> <value>                # 設欄位值（非物件引用）
up asset set-ref <assetPath> <fieldPath> <targetAssetPath>  # 欄位指向另一個 asset
up asset add-element <assetPath> <fieldPath> [--type <T>]   # 陣列/List 欄位尾端加一個元素
up asset invoke <assetPath> <methodName>                    # 呼叫無參數方法（按 Odin Button）
up asset fields <assetPath>                                 # 列出 asset 上的 serialized 欄位
```

| 指令 | 說明 |
|---|---|
| `create <TypeName> <assetPath>` | typeName 支援短名或 FullName；解析出的型別要真的繼承 `ScriptableObject`（不是就報錯）。assetPath 已存在時預設不覆蓋，`--overwrite` 才覆蓋 |
| `set <assetPath> <fieldPath> <value>` | fieldPath 支援巢狀，如 `_entries.Array.data[0]._family` |
| `set-ref <assetPath> <fieldPath> <targetAssetPath>` | 目標可以是 ScriptableObject / prefab / Texture2D / Sprite；prefab 會依欄位宣告型別取對應 component（同 `aref`） |
| `add-element <assetPath> <fieldPath> [--type <T>]` | 回傳新元素的 index，接著用 `set` / `set-ref` 補上 `<fieldPath>.Array.data[<index>].<子欄位>`。`--type` 只給 `[SerializeReference]` 陣列用，見下 |
| `invoke <assetPath> <methodName>` | 反射呼叫 asset 上一個無參數方法後存檔。方法名打錯會列出可用的無參數方法 |
| `fields <assetPath>` | 欄位名打錯時自我診斷用 |

## 實例：建一個 registry SO → 加一個陣列元素 → 指向另一個 SO

以專案既有的 `PromptIconRegistry`（`_entries: List<FamilyEntry>`，每筆有 `_family` enum 跟
`_config: DeviceIconMapConfig` 物件引用）示範完整流程 —— 這一整套實測跑過：

```bash
up asset create PromptIconRegistry "Assets/10_Scriptables/uprefab Test/測試 Registry.asset"
up asset create DeviceIconMapConfig "Assets/10_Scriptables/uprefab Test/測試 Config.asset"

up asset add-element "Assets/10_Scriptables/uprefab Test/測試 Registry.asset" _entries
# -> Assets/…/測試 Registry.asset._entries[0]  新增（現有 1 筆）

up asset set "Assets/10_Scriptables/uprefab Test/測試 Registry.asset" \
    "_entries.Array.data[0]._family" GamepadGeneric
up asset set-ref "Assets/10_Scriptables/uprefab Test/測試 Registry.asset" \
    "_entries.Array.data[0]._config" "Assets/10_Scriptables/uprefab Test/測試 Config.asset"
```

`up asset fields` 打錯欄位名時的診斷（實測輸出）：

```
$ up asset set "…/測試 Registry.asset" _wrongField x
# 未修改：PromptIconRegistry 上找不到欄位 '_wrongField'。可用的頂層欄位：_entries
```

路徑打錯時列出該資料夾實際有什麼（跟 `prefab read` 路徑打錯列子節點是同一套慣例）：

```
$ up asset fields "Assets/10_Scriptables/GeneralEffectType/does-not-exist.asset"
# 未修改：找不到 asset: …/does-not-exist.asset。…/GeneralEffectType 底下實際有：
  [Effect] Add Force.asset, [Effect] Break.asset, …
```

## `[SerializeReference]` 陣列要用 `--type` 指定實作型別

`GameData._dataFunctions`（`AbstractDataFunction[]`）這種多型陣列，單純 `add-element`
只會得到一個 **null 元素**（YAML 上是 `rid: -2`），看起來有加、實際什麼都沒有。
`--type` 會用 `managedReferenceValue` 塞一個具體實作進去：

```bash
up asset add-element "Assets/10_Flags/GameData/滅火器.asset" _dataFunctions --type PickableData
up asset set-ref "Assets/10_Flags/GameData/滅火器.asset" \
    "_dataFunctions.Array.data[0]._entityPrefab" "Assets/…/滅火器 Variant.prefab"
up asset add-element "Assets/10_Flags/GameData/滅火器.asset" _dataFunctions --type PriceData
up asset set "Assets/10_Flags/GameData/滅火器.asset" "_dataFunctions.Array.data[1]._basePrice" 15
```

型別池是該欄位宣告型別的非抽象衍生型別，打錯會列出候選。對非 SerializeReference 的陣列
傳 `--type` 會直接報錯（不會默默忽略）。**沒有 remove-element** —— 誤加了 null 元素，
最省事是 `create --overwrite` 重建再重填。

## 新建的 GameData 要重掃 `AllFlagCollection`

`GameData.Price` / `bindPrefab` 這類 property 在 Play Mode 下只查 `_dataFunctionDict`，
而那份 dict 是 `FlagAwake()` 建的。新建的 asset 沒被收進
`Assets/Resources/AllFlagCollection.asset` 就不會跑 `FlagAwake`，**dict 永遠是空的，
Price 靜默回 0**（Editor 下反而正常，因為 getter 會 `RebuildDataFunctionCheck()`）。

```bash
up asset invoke "Assets/Resources/AllFlagCollection.asset" FindAllFlagsInProject
```

**已知細節**：`AddArrayElement` 特意排除 `string` 欄位 —— `SerializedProperty.isArray`
對 `string` 也回 `true`（舊版序列化 API 把字串當 `char[]` 存），照字面判斷會把陣列元素
插進字串的位元組裡，存出一份壞掉的 UTF-8。已經在 `AssetEdit.AddArrayElement` 裡擋掉，
只有真正的陣列/List 欄位才會動。
