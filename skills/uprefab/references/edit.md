# 寫入 —— 批次 DSL（要 Unity 開著）

一行一個操作，欄位用 `|` 分隔。**分隔符不用空白** —— MonoFSM 節點名帶空白與 `[Tag] `
前綴（`[State] Player Idle`），中文名稱也很常見，空白分隔一定炸。

```bash
# prefab：整批共用一次 LoadPrefabContents / SaveAsPrefabAsset
up prefab do "Assets/…/FireBurn FSM 起火點.prefab" -f ops.txt

# scene：對當前開著的 scene；scene 不需要 load/save 配對，最後一行 save 就好
up scene do -f ops.txt
up scene do "add||資源生成器|MonoEntity,MonoObj" "save"    # 也可以直接帶參數
```

| 操作 | 說明 |
|---|---|
| `add\|<parent>\|<name>\|<comp,comp>` | 建節點並掛 component。parent 留空 = prefab root 下 / scene root 層 |
| `prefab\|<prefabPath>\|<parent>\|<name>` | 放 prefab 實例（prefab / scene 都支援；prefab 端就是裝 nested prefab 模組）。name 留空 = 用 prefab 自己的名字 |
| `comp\|<node>\|<comp,comp>` | 對既有節點加 component |
| `set\|<node>\|<comp>\|<field>\|<value>` | 設值。float / int / bool / string / enum（傳名稱）/ Vector3（`"x,y,z"`） |
| `ref\|<node>\|<comp>\|<field>\|<target>[\|<targetComp>]` | 指向另一個節點。targetComp 省略 = 用欄位宣告型別去找 |
| `aref\|<node>\|<comp>\|<field>\|<assetPath>` | 指向 asset（prefab / SO）。prefab 會按欄位型別取 component。內建 primitive 用 `builtin:Cube` / `Quad` / `Sphere` / `Capsule` / `Cylinder` / `Plane` / `Default-Material` —— 它們住在 `Library/unity default resources`，`AssetDatabase` 讀不到 |
| `addel\|<node>\|<comp>\|<field>` | 陣列 / List 欄位尾端加一個元素，回傳新 index；接著用 `set` / `aref` 補 `<field>.Array.data[<i>]`。**不能用 `set` 改 `.Array.size`**（ArraySize propertyType 走不進 ApplyValue） |
| `pos\|<node>\|x,y,z` | 設 localPosition |
| `scale\|<node>\|x,y,z` | 設 localScale（**只有 prefab**） |
| `rot\|<node>\|x,y,z` | 設 localEulerAngles（**只有 prefab**） |
| `active\|<node>\|<true/false>` | 設 GameObject.activeSelf（第二格必填，不猜預設值） |
| `mv\|<node>\|<newParent>` | 換 parent（**只有 scene**） |
| `auto\|<node>` | **重跑 `[Auto*]` 綁定 —— 結構改完一定要下這行**，見下面 |
| `del\|<node>` | 刪節點 |
| `delcomp\|<node>\|<comp,comp>` | 移除節點上的 component。不存在就跳過（語意是「確保它不在」）。prefab 版 `<node>` 留空 = root |
| `save` | 存 scene（**只有 scene**；prefab batch 結束自動存） |

要點：

- **第一個失敗就停**，並回報「停在第幾行、前面幾個已生效」。後面的操作通常依賴前面的
  結果，硬跑下去只會產生一長串誤導性錯誤。prefab batch 更進一步：**任何一行失敗就整批不存檔**。
- **`add` 重複不算錯**，回「（跳過）已存在」。批次的實際用法是「修一行再整份重跑」。
- **錯誤訊息會給下一步的線索**：路徑錯 → 列出走到哪、那層有哪些子節點；型別打錯 → 列出
  名稱相近的候選；欄位名錯 → 列出可用欄位；**巢狀路徑錯 → 列出走得通的那一層底下有什麼**
  （`_timeMax._constValue` → 「走到 `_timeMax`（VarFloatWrapper），這層底下有 `_tempValue: float`」）。
- **改完直接 `prefab read` 驗證** —— 讀到的一定是當下真值，不必擔心快取同步。

## 同名節點用 `[n]` 指定第幾個

MonoFSM 的節點常常整排同名（一個 Switch 底下七個 `[Case] SwitchCase`、同一層兩個
`[Switch Simulate] Switch (FirstMatch)`）。`Transform.Find` 永遠只給第一個，所以路徑
任何一段都可以加 `[n]` 後綴（0-based，**依 sibling 順序**）：

```
[VarFolder] VariableFolder/[Switch Simulate] Switch (FirstMatch)[1]/[Case] SwitchCase[4]
```

`read` 與 `do` 走同一套解析。只有整條路徑照原樣 `Find` 失敗時才會試 `[n]`，所以名字本身
結尾就是 `[數字]` 的節點不受影響。路徑打錯時列出的子節點清單，**同名的會自己標上 `[n]`** ——
照抄就好。

## 結構改完一定要 `auto`

MonoFSM 大量欄位靠 Auto 系列 attribute 填 —— `TransitionBehaviour._conditions` 是
`[AutoChildren]`、Action 的 `_parentObj` 是 `[AutoParent]`。平常是 Inspector 畫到時
順手綁的，用 API 建節點不經過 Inspector，**不補這步會存出一份「看起來對、欄位全是 null」
的資料**，而且只有進 Play Mode 才會發現。

## `[AutoChildren]` 的子節點是「整個 GameObject」共用的

condition / value source 這類靠 `[AutoChildren(DepthOneOnly)]` 撈的欄位，看的是**掛載節點的
子節點**，不是「哪個 component 的子節點」。同一個 GameObject 上有兩個都用 AutoChildren 的
component 時，它們會撈到同一批。

實例：`VerletRope` 繼承 `AbstractRenderBehaviour`，自帶 `_conditionGroup`
（`[AutoNested]` → `[AutoChildren(DepthOneOnly)]`）。想用 condition 控制同節點上另一個
component，就在它底下 `add` 了 condition —— 結果 `VerletRope` 自己的 `OnRender` 也被那個
condition 擋掉，**平常整條繩子都不模擬、不更新**，而且沒有任何錯誤訊息。

要「只影響某一個 component」的條件，別放子節點，改成值引用：

```
[VarFolder] VariableFolder
  [Getter] Is Plugged or Grabbing  <VarBool>          ← VarBool 取子節點 condition 的值
    [If OR] Plugged or Grabbing <CompositeCondition _operationType=Or>
      [If] IsState Plugged   <IsStateCondition>
      [If] IsState Grabbing  <IsStateCondition>
```

component 上宣告 `[DropDownRef] public VarBool _showVarBool;`，用 `ref` 指過去。順帶好處是
這個 bool 能被 FSM action / 網路同步共用，符合「變數單一來源」慣例。

`ConditionGroup._conditions` 的 `[SerializeField]` 是被註解掉的 —— 它**不序列化**，靠
`AutoAttributeManager` 在 runtime 綁定。所以 `prefab read` 看不到值是正常的，別以為沒綁上。

## prefab batch 存檔時會跑 `IBeforePrefabSaveCallbackReceiver`

Unity 只在 PrefabStage（人工打開 prefab 編輯再存）觸發這個 callback，
`LoadPrefabContents` + `SaveAsPrefabAsset` 這條路不會 —— 所以 `prefab do` 自己補跑了。

**為什麼非跑不可**：`NetworkAutoSuggestVarSyncComp` 靠它掃 subtree 的 `NetworkedVarTag`、
反射挑最省的 sync 元件（`NetworkedVarSyncFloat4` / `Bool4Float4` / `Array`…）並填好
`_syncFloats` / `_syncInts`。不跑的話，用 API 加的 networked var **會靜默沒有同步元件**，
單機測完全正常，只有多人實測才發現。

log 尾巴會出現 `# 存檔前 callback：920 個 OK`。專案幾乎每個 MonoBehaviour 都實作這個介面，
所以只報數量，出錯的才點名。`_syncFloats` / `_syncInts` **不要手動編**，會被這步覆寫。

## nested prefab 實例：改動會存成外層的 override，不會污染源 prefab

對 nested prefab 實例底下 `add` / `set` / `ref`，`LoadPrefabContents` + `SaveAsPrefabAsset`
會把它記成**外層 prefab 的 override**，源 prefab 不動。所以「共用 prefab 想在某一處加東西、
其他使用處維持原狀」是做得到的，而且這是**唯一**能引用到外層才有的節點（例如只有 PPlayer
才有的 `[State] Plugged`）的做法。

驗證方式：改完 `grep` 一下 script 的 guid 落在哪個 `.prefab` 檔。

## 節點名會被 rename，`del` 舊節點前先看實際名稱

`AbstractDescriptionBehaviour` 會依 `DescriptionTag` 改 GameObject 名字，`add` 當下給的名字
存檔後可能變樣 —— `[If OR] …` 會變成 `[if OR] …`（tag 是 `"if " + OR`）。同一批 ops 內接續
引用剛建的節點沒問題（rename 還沒發生），但**下一批**要 `del` / `--node` 時得用實際名稱。
路徑打錯的錯誤訊息會列出該層真正的子節點，照抄就好。

## 建新東西：一律開 variant / 複製模板，不要從零建

專案的 prefab 帶著大量共用底盤（MonoEntity / MonoObj / NetworkObject / Culling /
ModulePack），scene 也需要 WorldUpdateSimulator / SpawnProcessor / PoolManager /
AutoAttributeManager。從零建看起來乾淨，實際會漏，而且漏掉的只在 Play Mode 才炸。

```bash
# FSM 物件：從乾淨的 init/idle 骨架開 variant
up prefab variant "Packages/com.monofsm.fusion/MonoFSM_Fusion/Network FSM.prefab" \
    --out "Assets/…/我的 FSM.prefab" --name "我的 FSM"

# 場景：複製模板（不要用 scene new）
up scene copy --template "Assets/1_Prototype/Module Test/Network FSM Template.unity" \
    "Assets/…/我的測試.unity"
```
