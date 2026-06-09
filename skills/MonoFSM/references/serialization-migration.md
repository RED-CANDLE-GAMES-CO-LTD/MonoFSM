# Serialized 欄位型別遷移（零掉 ref）

當需要把一個**已序列化的欄位**改成不同型別（最常見：`VarFloat` 直接參照 → `VarFloatWrapper`），
Unity 無法自動轉換，會直接清空舊資料。本文件記錄安全遷移的標準流程。

## 為什麼直接改型別一定掉 ref

直接參照存的是單一 fileID：

```yaml
_valueVar: {fileID: 12345}
```

改成 wrapper（`VarWrapper<TVar,TValue>`）後，序列化結構變成巢狀物件，舊 ref 要進到 `_var` 那一層：

```yaml
_valueVar:
  _tempValue: 1
  _bindTag: {fileID: 0}
  _var: {fileID: 12345}   # ← 舊 fileID 必須搬到這層
```

Unity 反序列化時看到欄位從「物件參照」變成「serializable class」，**無法自動把舊 fileID 搬進 `._var`**。
`[FormerlySerializedAs]` 也救不了——它只能改名，不能改變序列化的**層級深度**。

## 安全遷移流程（6 步）

### 1. 先量影響範圍

用 script GUID 找出哪些 prefab/scene/asset 有「實際指派」此欄位（排除 `fileID: 0`）：

```bash
# 取 script GUID
grep guid YourComponent.cs.meta
# 找實際指派的行（含檔名）
grep -rn "_yourField:" Assets 2>/dev/null | grep -E '\.(prefab|unity|asset):' | grep -v "fileID: 0}"
```

> zsh 下不要用 `--include=*.prefab`（會 no matches found），改用 `grep -rn ... Assets | grep -E '\.(prefab|unity)$'`。
> 注意：scene 裡的 component 多半是 prefab instance，真正的來源 ref 通常只在少數幾個 source prefab。

### 2. 加 legacy 欄位接住舊資料

保留舊型別欄位、改名、`[HideInInspector]`，用 `[FormerlySerializedAs]` 接住原本的序列化 key：

```csharp
[FormerlySerializedAs("_valueVar")] //原本的欄位名
[SerializeField]
[HideInInspector]
private VarFloat _legacyValueVar;
```

### 3. 新增目標型別欄位（不能同名）

新欄位**不可**沿用舊名（會跟 `FormerlySerializedAs` 的 target 衝突）。給新名字：

```csharp
[SerializeField]
private VarFloatWrapper _valueVarRef = new(1f); //初始值沿用舊的 null fallback 行為
```

> `VarFloatWrapper(1f)` 的 `_tempValue = 1` 沿用舊 code `_valueVar?.Value ?? 1f` 的預設。
> 注意：C# field initializer **只在新建物件時跑**，既有序列化物件新增欄位時 Unity 寫的是 `default(T)`（=0）。
> 若未指派的預設值很重要，要在 Migrate 時補；本案因實際使用都有指派 `_var`，未指派的行為差異無影響。

### 4. 加一次性遷移方法 + 改用新欄位

```csharp
public void MigrateValueVar()
{
    _valueVarRef ??= new VarFloatWrapper(1f);
    if (_legacyValueVar != null && _valueVarRef._var == null)
    {
        _valueVarRef._var = _legacyValueVar;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
```

同時把所有讀舊欄位的程式改用新 wrapper（`_valueVar?.Value` → `_valueVarRef.Value`、cycle 驗證改檢查 `_valueVarRef._var` 等）。每步 `uloop compile` 確認 0 error。

### 5. 批次跑遷移（用 LoadPrefabContents，不靠 OnValidate）

對 source prefab 明確呼叫 `MigrateValueVar()`，比依賴 OnValidate 觸發可靠。用 `uloop execute-dynamic-code`：

```csharp
using System.Text; using UnityEditor; using YourNamespace;
var paths = new []{ "Assets/.../A.prefab", "Assets/.../B.prefab" };
var sb = new StringBuilder();
foreach (var path in paths)
{
    var root = PrefabUtility.LoadPrefabContents(path);          //取可編輯副本
    foreach (var m in root.GetComponentsInChildren<VariableStatModifier>(true))
        m.MigrateValueVar();
    PrefabUtility.SaveAsPrefabAsset(root, path);                //寫回
    PrefabUtility.UnloadPrefabContents(root);                   //一定要 unload
    sb.AppendLine("migrated: " + path);
}
AssetDatabase.SaveAssets();
return sb.ToString();
```

**驗證**：比對 legacy fileID 與新欄位 `_var` fileID 是否一一對應：

```bash
grep -rh "_legacyValueVar:" <prefabs> | grep -v "fileID: 0}"
grep -rh -A3 "_valueVarRef:" <prefabs> | grep "_var:" | grep -v "fileID: 0}"
```

### 6. 移除 legacy 欄位 + 重存 prefab 清孤兒資料

確認 ref 都在後，刪掉 `_legacyValueVar` 與 `MigrateValueVar()`，`uloop compile`。
prefab 裡殘留的 `_legacyValueVar:` 行此時是孤兒（無對應欄位），Unity 雖會忽略，但**再 LoadPrefabContents + SaveAsPrefabAsset 一輪可清掉**，最後 `grep -c _legacyValueVar` 應為 0。

## 關鍵 gotcha

- **`execute-dynamic-code` 用 `return`** 回傳結果，不要 `File.WriteAllText`（會被 Restricted security 擋）。
- **prefab 編輯一律用 `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset` / `UnloadPrefabContents`**，不要直接改 `AssetDatabase.LoadAssetAtPath` 拿到的實例。
- 遷移前**先量範圍**：若只有少數幾處且都在 source prefab，這套流程很快；若散落 scene override 才需要連 scene 一起跑。
- 每改一步 code 就 `uloop compile`，分階段確認，不要全改完才編譯。
