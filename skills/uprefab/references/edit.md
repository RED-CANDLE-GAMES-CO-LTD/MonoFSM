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
| `set\|<node>\|<comp>\|<field>\|<value>` | 設值。float / int / bool / string / enum（傳名稱）/ Vector3（`"x,y,z"`）。long（`m_TableEntryReference.m_KeyId`）超出 int 範圍會自動走 `longValue` |
| `ref\|<node>\|<comp>\|<field>\|<target>[\|<targetComp>]` | 指向另一個節點。targetComp 省略 = 用欄位宣告型別去找 |
| `aref\|<node>\|<comp>\|<field>\|<assetPath>` | 指向 asset（prefab / SO）。prefab 會按欄位型別取 component。內建 primitive 用 `builtin:Cube` / `Quad` / `Sphere` / `Capsule` / `Cylinder` / `Plane` / `Default-Material` —— 它們住在 `Library/unity default resources`，`AssetDatabase` 讀不到 |
| `addel\|<node>\|<comp>\|<field>` | 陣列 / List 欄位尾端加一個元素，回傳新 index；接著用 `set` / `aref` 補 `<field>.Array.data[<i>]`。**不能用 `set` 改 `.Array.size`**（ArraySize propertyType 走不進 ApplyValue） |
| `pos\|<node>\|x,y,z` | 設 localPosition |
| `scale\|<node>\|x,y,z` | 設 localScale（**只有 prefab**） |
| `rot\|<node>\|x,y,z` | 設 localEulerAngles（**只有 prefab**） |
| `active\|<node>\|<true/false>` | 設 GameObject.activeSelf（第二格必填，不猜預設值） |
| `idx\|<node>\|<siblingIndex>` | 調 sibling 順序。**child 順序＝優先序**（value source / condition 取第一個成立的），負數從尾端算（`-1` = 最後） |
| `mv\|<node>\|<newParent>` | 換 parent（scene 與 prefab 都支援） |
| `rename\|<node>\|<newName>` | 改節點名（`<node>` 留空 = root）。**只對沒掛 `AbstractDescriptionBehaviour` 的節點有意義**，其餘存檔後會被自動命名蓋掉，見 [naming.md](naming.md) |
| `auto\|<node>` | **重跑 `[Auto*]` 綁定 —— 結構改完一定要下這行**，見下面 |
| `del\|<node>` | 刪節點 |
| `delcomp\|<node>\|<comp,comp>` | 移除節點上的 component。不存在就跳過（語意是「確保它不在」）。prefab 版 `<node>` 留空 = root |
| `save` | 存 scene（**只有 scene**；prefab batch 結束自動存） |
| `mark\|<label>[\|<node>]` | 給節點取個短名，之後用 `$label` 代換。不給 `<node>` = 標記上一個建立節點的操作 |

**`add` / `comp` / `set` / `ref` / `aref` / `addel` / `delcomp` 的 `<node>` 留空 = prefab root**
（`MonoEntity` / `MonoObj` / `NetworkObject` 都掛在 root 上）。scene 版沒有這個語意 ——
scene 沒有唯一 root，第一段一定要是 root object 名稱。

節點名含 `/` 或換行時的逃逸規則見 [naming.md](naming.md)。

## `$` 代換 —— 不要把同一條長路徑寫兩次

MonoFSM 的節點路徑動輒六十個字元（`[StateFolder] StateFolder/[State] idle/[Event]
OnStateEnter/[Action] Reset Timer`），而「`add` 完緊接著 `ref`」是最常見的組合。
**這也是對抗自動命名最有效的一招**（見 [naming.md](naming.md)）：`$` 記的是節點本身，
不受改名影響。任何參數都可以寫：

| 寫法 | 代換成 |
|---|---|
| `$` | 上一個**建立節點**的操作（`add` / `prefab` / `state` / `trans` / `if` / `act`）碰到的節點 |
| `$/子路徑` | 同上，再往下接 |
| `$label` / `$label/子路徑` | `mark` 標過的節點 |
| `$$` | 字面上的 `$`（prompt 的 `${token}` 不是識別字，不會被誤代換，不用跳脫） |

`set` / `ref` / `pos` 這類不建節點的操作**不會**更新 `$`，所以 `add` 之後可以連著下好幾條
`ref|$|…`。

## FSM 複合操作 —— 一行取代三到四行原語

| 操作 | 展開成 |
|---|---|
| `state\|<folder>\|<name>[\|<type>]` | 建 `[State] <name>` + `GeneralState`（或指定的 type） |
| `trans\|<from>\|<to>[\|<name>]` | 建 `[Transition] => <to 的名字>` + `TransitionBehaviour` + 接上 `_target` |
| `if\|<node>\|<name>\|<condType>[\|<field>\|<target>]` | 建 `[If] <name>` + condType，順手接一條引用（給 `_timer` / `_varBool` 這種） |
| `act\|<state>\|<phase>\|<name>\|<actionType>` | 確保 `[Event] On<Phase>` + handler 在（多個 action 共用），再掛 `[Action] <name>` + actionType |

`phase`：`enter` / `exit` / `update` / `enterRender` / `exitRender`。
名稱沒帶 `[Tag] ` 前綴會自動補上；節點已存在就沿用（跟 `add` 一致，方便整份重跑）。
每個複合操作都會更新 `$`，指向它建的那個節點（`act` 指到 action，不是 event 節點）。

```
mark|SF|[StateFolder] StateFolder
add||Timer|VarFloatCountDownTimer
mark|T
set|$T|VarFloatCountDownTimer|_timeMax._tempValue|1

state|$SF|spawn
mark|SPAWN
act|$SF/[State] idle|enter|Reset Timer|ResetTimerAction
ref|$|ResetTimerAction|timer|$T
trans|$SF/[State] idle|$SPAWN
if|$|Timer Up|IsTimerUpCondition|_timer|$T
act|$SPAWN|enter|Spawn 資源|SpawnAction
aref|$|SpawnAction|_poolObjFoldOut._constObjValue|Assets/…/測試資源 Rock Variant.prefab
trans|$SPAWN|$SF/[State] idle
auto|
```

只做「一定會這樣做」的部分（命名慣例、handler 型別對照、`_target`），其餘欄位照舊
`set` / `ref`。

要點：

- **第一個失敗就停**，並回報「停在第幾行、前面幾個已生效」。後面的操作通常依賴前面的
  結果，硬跑下去只會產生一長串誤導性錯誤。prefab batch 更進一步：**任何一行失敗就整批不存檔**。
- **`add` 重複不算錯**，回「（跳過）已存在」。批次的實際用法是「修一行再整份重跑」——
  但重跑前先 `read` 拿當下的節點名，見 [naming.md](naming.md)。
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
所以只報數量，出錯的才點名。

⚠️ **但 callback 不保證會把 var 填進陣列**。2026-08-23 在「升級訂購機 Variant」上新增
`[Var] Sold Out Mask`（VarInt + `NetworkedVarTag`）後，存檔前 callback 有跑、`_syncInts` 仍是空的。

所以流程是：加 `NetworkedVarTag` → 存檔 → **`peek` 確認該 var 真的進了 `_syncXxx`**。
**沒進的話不要自己 `addel` + `ref` 補** —— 接進 sync 陣列（含換成容量更大的
`NetworkedVarSyncBool4Float4`）由 Jerryee 在 Editor 端處理，手動動陣列容易搞亂既有槽位配置。
回報時說一句「這顆要接進 sync」就好。

## nested prefab 實例：改動會存成外層的 override，不會污染源 prefab

對 nested prefab 實例底下 `add` / `set` / `ref`，`LoadPrefabContents` + `SaveAsPrefabAsset`
會把它記成**外層 prefab 的 override**，源 prefab 不動。所以「共用 prefab 想在某一處加東西、
其他使用處維持原狀」是做得到的，而且這是**唯一**能引用到外層才有的節點（例如只有 PPlayer
才有的 `[State] Plugged`）的做法。

驗證方式：改完 `grep` 一下 script 的 guid 落在哪個 `.prefab` 檔。

## 建新東西：一律開 variant / 複製模板，不要從零建

專案的 prefab 帶著大量共用底盤（MonoEntity / MonoObj / NetworkObject / Culling /
ModulePack），scene 也需要 WorldUpdateSimulator / SpawnProcessor / PoolManager /
AutoAttributeManager。從零建看起來乾淨，實際會漏，而且漏掉的只在 Play Mode 才炸。

```bash
# FSM 物件：從乾淨的 init/idle 骨架開 variant
up prefab variant "Packages/com.monofsm.fusion/MonoFSM_Fusion/Network FSM.prefab" \
    --out "Assets/…/我的 FSM.prefab" --name "我的 FSM"

# 拿既有 prefab 當模板改成一份獨立的（不留 variant 連結，root 名稱一併改掉）
up prefab copy "Assets/…/Lightning Attack Module 落雷攻擊.prefab" \
    --out "Assets/…/Leak Electricity Module 漏電攻擊.prefab" --name "Leak Electricity Module 漏電攻擊"

# 場景：複製模板（不要用 scene new）
up scene copy --template "Assets/1_Prototype/Module Test/Network FSM Template.unity" \
    "Assets/…/我的測試.unity"
```
