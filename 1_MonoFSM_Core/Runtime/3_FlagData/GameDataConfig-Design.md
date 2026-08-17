# GameData Config 供值機制 — 調查結論與設計

> 起因：`VarFloatEffectApplyAction.cs:330` 的 `sourceValue` 是否該優先讀 entity 的 `BindData.TryGetConfig`。
> 結論是**不要在取值端開分岔**，改用「entity 層一次性注入」把 config 餵進 VarFloat。

---

## 一、調查到的事實（決定設計的依據）

### 1. VarFloat 取值鏈

- `Value` 只是 `CurrentValue` 的 alias（`AbstractFieldVariable.cs:362-373`），`FinalValue` 也是（:355）。
- 取值核心 `GetCurrentValueCore()`（`AbstractFieldVariable.cs:441-460`）優先序：
  `_isNetOverridden` 網路覆寫值 → `valueSource` → `varRef`（parent entity proxy）→ `_localField.CurrentValue`。
  Editor 非 play 時直接回 `EditorValue`。
- 多個 valueSource 誰贏：`ValueResolver.GetActiveValueSource`（`AbstractValueSource.cs:60-79`）按 `_valueSources`
  **陣列順序，第一個 `IsValid` 的贏**。`IsValid = _conditionGroup.IsValid && isActiveAndEnabled`（:25）。
- **完全沒有 cache**：每次讀 `CurrentValue` 都重跑 `GetActiveValueSource()`，foreach 檢查每個 source。這是熱路徑。
- `_isSelfInclude`：`_valueSources` 宣告為 `[AutoChildren(DepthOneOnly = true, _isSelfInclude = true)]`
  （`AbstractMonoVariable.cs:649`）＝同節點的 IValueProvider 也會被撿。所以 bridge 掛在 VarFloat 同節點。

### 2. 初始化 / 重置：注入值的正確時機是 `IResetStart`

- 初值來源：`FlagField.ResetToDefault()`（`FlagField.cs:618-643`）→ runtime 取
  `IsDebugMode ? DevValue : ProductionValue`。
- 會**重設值**的 hook 有兩個，都呼叫 `Field.Init(TestMode.Production, this)`（清 modifiers + ResetToDefault）：
  - `ISceneStart.EnterSceneStart()`（`AbstractFieldVariable.cs:248-254`）
  - `IResetStateRestore.ResetStateRestore()`（`AbstractFieldVariable.cs:687-695`，另外會 `ClearNetworkOverride()`）
- 實際順序（`WorldUpdateSimulator.cs:469-471, 492-497`）：
  `SceneAwake → SceneStart → WorldReset(ResetLevelRestore → ResetLevelStart)`。
  Pool spawn 也是 `ResetStateRestore(false); ResetStart();`（`MonoObj.cs:370-374`）。
- **⇒ 用 ISceneAwake / ISceneStart / IResetStateRestore 寫值都會被後面的 `Field.Init` 蓋掉。
  只有 `IResetStart` 正確，且同時涵蓋「開場 / level reset / pool spawn」三條路徑。**
  （對應 `MonoObj.cs:40` 的註解：「摸別人、set 變數之類的，要不然會 reset 掉」）

### 3. entity 下的變數蒐集

- `VariableFolder : MonoDictFolder<VariableTag, AbstractMonoVariable>`（`VariableFolder.cs:18`）。
  蒐集靠 `MonoDict._collections`（`MonoDict.cs:29-32`）＝ `[CompRef][AutoChildren]`，遞迴撈整個子樹（含 disabled）。
- `IsAddValid` 會排除 `HasParentVarEntity` 的 proxy var（`VariableFolder.cs:66-83`）
  ⇒ `_dict` 就是「這顆 entity 自己擁有的變數」，正是要注入的集合。
- 列舉用 `folder.Collections`（陣列，無 GC）；沒有「所有 VarFloat + tag」的專用 API，
  但 `foreach (var v in folder.Collections) if (v is VarFloat f && f._varTag != null)` 就夠（`_varTag` 是 public）。
- prepared 時機：`MonoDict.Awake()` 與 `ISceneAwake.EnterSceneAwake()`（`MonoDict.cs:350-364`）。
  未 prepared 時 `Get()` 會 LogError 回 default（:212-217）。**IResetStart 時已 prepared，安全。**

### 4. entity ↔ GameData 的綁定現況

- **MonoEntity 身上沒有 GameData 欄位**：`data` 整段被註解掉（`MonoEntity.cs:410-429`），
  `IGameDataProvider` 已標 `[Obsolete]`（`IGameDataProvider.cs:9-14`）。
- **實際慣例＝在 entity 的 VariableFolder 下放一顆有 tag 的 `VarGameData`**。既有用法：
  - `神像 Base statue.prefab`、`測試車廂.prefab` → `[Var] d_BindTrainCarData 車廂類型`
  - `Base Character.prefab` → `[Var] v_目前陣營 currentFaction`
  - `Char_lowpoly_v5_Ragdoll.prefab`、`Item Slot.prefab` → `[Var] d_BindData`
  - 跨 entity 取用掛 `GetVarFromParentEntitySource`
- `SlotEntryMonoEntity.BindData`（`MonoFSM-Pro/Runtime/InventorySystem/New/SlotEntryMonoEntity.cs:44-47`）
  是「`_bindDataVar` 優先、fallback inspector 欄位」的個案寫法，不是通用機制。
- **`GameDataConfigValueSource` 目前 0 個 prefab / scene 在用**（script guid `6383717af1b8b4ab2872fa7efc61af48`
  全 `Assets/` 掃過，命中 0）⇒ 沒有 migrate 成本，可自由改路線。

### 5. 網路同步限制

- `ChanneledNetworkedVarSync` 系列：`VarFloat ↔ NetworkArray<float>`（`ChanneledNetworkedVarSync.cs:11-25`）。
- Authority gate **不是預設開的**：只有 Var 節點掛 `NetworkedVarTag` 且勾 `_stateAuthorityOnlyWrite`，
  才會 `SetNetworkAuthorityOwner`（`AbstractNetworkedVarSync.cs:480-486`）；被認領後非 SA 端的 `SetValue`
  直接被擋（`AbstractFieldVariable.cs:489-493`、`AbstractMonoVariable.cs:191-192`），只記 debug 不寫值。
  需要繞過時有 `SetValueLocalPredicted`（`AbstractFieldVariable.cs:472-475`）。
- **bridge（valueSource）路線在網路下會白算**：該 Var 是 `HasProxySource`，sync 每 tick 當 polled getter 寫出；
  client 收到權威值後 `_isNetOverridden = true`，`CurrentValue` 直接回網路值，
  **本地 config 計算被完全繞過**（`AbstractFieldVariable.cs:94-108, 443-444`）。
- reset 時序：sync 的 `ResetStart()` 只在非 SA 端 `ReadFromNetwork()`（`AbstractNetworkedVarSync.cs:571-580`），
  與 injector 同在 IResetStart 階段，**兩者順序由 `_resetStarts` 的 AutoChildren 順序決定，不保證**。

### 6. 既有的「批次注入」形狀可以照抄

- `DynamicVariableBinder`（`MonoFSM/1_MonoFSM_Core/Runtime/2_Variable/DynamicVariableBinder.cs:6-17`）：
  `AbstractFolder, IResetStart, IBinder`，`ResetStart()` 裡 foreach `[AutoChildren]` entries 呼叫 `Bind()`。
  **正是要的形狀**，但它是一 entry 一顆的手動配對。
- `MonoEntity.BindModulePackFolders()`（`MonoEntity.cs:557+`）是既有的批次綁定 pattern，但綁 folder 不綁值。
- `VariableStateSerializer`（`MonoFSM-Pro/Runtime/Virtualization/VariableStateSerializer.cs:21, 62`）是唯一既有的
  「掃整個 VariableFolder 批次讀寫值」，但走反射 + Dictionary，作者自註 GC 問題，**不要照抄**。
- 沒有任何現成的「按 VariableTag 從表批次餵值」component。

---

## 二、方案比較

| | bridge 路線（每顆 VarFloat 掛 GameDataConfigValueSource） | 注入路線（每個 entity 掛一顆 injector） |
|---|---|---|
| 掛載成本 | 每顆 VarFloat 一個 component | 每個 entity 一顆 |
| runtime 成本 | 每次讀值 foreach valueSources + TryGetConfig | reset 時寫一次，之後讀值走 localField |
| 網路同步 | client 端被 `_isNetOverridden` 繞過，host 每 tick polled 寫出（白算） | 值進 localField，sync 正常運作 |
| 語意 | config 是「唯讀常數覆蓋」，遊戲邏輯改不動 | config 是「初始值」，之後可被遊戲邏輯改寫 |
| override | 查不到 key 退回 local value | 同左 |

**在取值端（Action / Condition）直接讀 config 的做法已否決**，理由：

1. 會讓「同一個 tag 的值從哪來」有兩條真相來源。
2. `VarFloatEffectApplyAction` 的 Transfer / TransferAll（:356-381）需要可寫的 `sourceVar` 物件
   （讀 `Min`/`Max`、`SetValue` 扣值），config 是唯讀 float 接不上；
   `RecipeDataFunction.cs:59`（扣材料）、`AllPlayersDeadCondition.cs:139`（遞減）同理。
3. 要一致就得三處各自補 fallback，而 entity 層根本還沒有通用的 BindData 入口。
4. Inspector 除錯會變瞎（`VarFloatEffectApplyAction.cs:211-224` 的 Runtime Preview 只會顯示「Tag 解析失敗」）。

---

## 三、定案設計

### 1. `GameDataConfigInjector`（新增）

掛在 entity 的 VariableFolder 節點上，形狀比照 `DynamicVariableBinder`，零反射零 GC。

```csharp
public class GameDataConfigInjector : MonoBehaviour, IResetStart
{
    [AutoParent] [SerializeField] private VariableFolder _folder;
    [SerializeField] private VarGameDataWrapper _bindData; // 指到 entity 下那顆 VarGameData

    public void ResetStart() => Inject();

    public void Inject() // GameData 執行期換掉（換 item / 換車廂類型）時由 FSM Action 呼叫
    {
        var data = _bindData.Value;
        if (data == null) { Debug.Log("[ConfigInject] bindData null, 用 prefab local value", this); return; }

        var comps = _folder.Collections;
        for (var i = 0; i < comps.Length; i++)
        {
            if (comps[i] is not VarFloat f || f._varTag == null) continue;
            if (data.TryGetConfig(f._varTag, out var v)) f.SetValue(v, this);
        }
    }
}
```

**語意**：config = 初始值表，VarFloat = 執行期狀態。config 沒填的 tag 用 prefab local value，
per-prefab override 天然成立（GameData 是共用設定，prefab 是特例）。

**所有既有消費端零改動**：`VarFloatEffectApplyAction.cs:330` 不用碰，
`RecipeDataFunction`、`AllPlayersDeadCondition` 自動受惠。

### 2. 編輯期 Odin 按鈕「依 GameData config 補齊 VarFloat」

解決「還是得一顆顆宣告 VarFloat」的手工成本。選中 entity → 掃 `data._configs` →
entity 下缺哪個 tag 就自動建節點、設好 `_varTag` 與預設值。

**編輯期生成，不是 runtime 生成** —— 避開序列化 / 網路同步 / Inspector 預覽的坑。
流程變成：GameData 表填一次 → 按一次按鈕 → 收工；config 表加欄位時再按一次補齊。

### 3. `GameDataConfigValueSource` 處理

目前 0 使用，傾向直接刪，避免留下第二條取值路徑。
（保留的唯一理由是「config 必須每幀反映變動」，目前想不到這種需求。）

---

## 三之二、第二輪討論定案（Schema / Variant / Sheet 編輯）

### 1. Schema 是掃出來的，不存在於任何 asset

schema 的真相＝prefab 結構（dealers/receivers/actions 消費了哪些 varTag）。GameData 永遠只是**值表**，
不承擔 schema 角色。一個家族的 schema 是所有 variant 的**聯集**，GameData 是**稀疏表**——
variant 新增的 VarFloat 在共用表查不到就用 prefab local（injector 是「掃 entity 的 var 拿 tag 查表」，
方向天然支援）；表裡有 base prefab 用不到的欄位也只是靜默閒置。
validate 工具唯一該警告的：**config entry 沒被家族任何 prefab 消費**（死欄位＝打錯字或已移除）。

### 2. 值的優先序與 variant 微調

優先序：**prefab 顯式覆蓋 > GameData > prefab 預設**。
injector 加 `[SerializeField] VariableTag[] _skipTags`（不動 VarFloat 型別），
variant 只想改一兩個值 → local value 改掉 + tag 加進 skip 清單，共用同一顆 GameData。

### 3. GameData 疊層（解抄值）

整組數值不同且有 identity 意義時才 fork GameData：加 `_baseConfig` 欄位，
`TryGetConfig` 自己查不到就問 base（遞迴 + 防循環）。variant 的 GameData 只存 delta。

### 4. GameData vs prefab local 的判準

> **多消費端共值、或需要執行期換綁 identity 的 → GameData；其他一律留 prefab local。**

某欄位在大多數 variant 都被 skip/override ＝ 訊號：它不該是 config，移出表。

### 5. Prefab-Var Sheet 編輯工具

editor window：掃 prefab 家族（base + variants）→ rows = prefab variants、columns = tags、
cell = FlagField 序列化預設值。繼承值灰字、variant override 高亮。
**寫回必須走 `PrefabUtility`，不能 raw YAML**（variant `m_Modifications` 靜默寫不進的坑）。
有了這工具，「放哪」就不再受編輯方便性影響，純看第 4 點判準。

### 6. 原待決細節的裁定

- **網路 late join**：先不加 SA gate（開場兩端 config 相同無害；offline 模式下拿 SA 也麻煩）。
  injector 留 TODO 註記 late-join client 可能被蓋回初始值的風險，等實際遇到再處理。
- **`GameDataConfigValueSource`：刪除**（0 使用，避免第二條取值路徑）。

---

## 四、實作項目

### Phase 1 — runtime 核心（unity-impl）

1. `GameData.Config.cs`：加 `_baseConfig`（GameData，可 null）疊層；`TryGetConfig`/`HasConfig`
   查不到時問 base，防循環（走訪時上限深度或 visited 檢查）。
2. 新增 `GameDataConfigInjector.cs`（同資料夾）：照「三、定案設計 1」的形狀，
   加上 `_skipTags` 過濾與 late-join TODO 註記。
3. 刪除 `GameDataConfigValueSource.cs`（+.meta）。
4. 編譯確認 0 error。

### Phase 2 — editor 工具（unity-impl，Phase 1 完成後）

放 `1_MonoFSM_Core/Editor/`（MonoFSM.Core.Editor asmdef）：

1. **補齊按鈕**：選中 entity → 掃綁定 GameData 的 `_configs` → 缺的 tag 自動建 VarFloat 節點。
2. **Schema validate**：掃 prefab 家族 derive schema（聯集）→ 報 GameData 死欄位。
3. **Prefab-Var Sheet window**：見「三之二 5」。

---

## 五、順帶要更新的 skill

`alishan-code-map` 目前沒記到：

- entity 持有 GameData 的慣例＝VariableFolder 下一顆有 tag 的 `VarGameData`（`d_BindData` / `d_BindTrainCarData`），
  `MonoEntity.data` 與 `IGameDataProvider` 都是死路。
- 注入變數值的正確 hook 是 `IResetStart`，不是 ISceneStart / IResetStateRestore（會被 `Field.Init` 蓋掉）。
