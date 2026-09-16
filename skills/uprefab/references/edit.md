# 寫入 —— 批次 DSL（要 Unity 開著）

一行一個操作，欄位用 `|` 分隔。**分隔符不用空白** —— MonoFSM 節點名帶空白與 `[Tag] `
前綴（`[State] Player Idle`），中文名稱也很常見，空白分隔一定炸。

```bash
# prefab：整批共用一次 LoadPrefabContents / SaveAsPrefabAsset
up prefab do "Assets/…/FireBurn FSM 起火點.prefab" -f ops.txt
up prefab do "Assets/…/FireBurn FSM 起火點.prefab" -f ops.txt --quiet  # 成功只看摘要

# scene：對當前開著的 scene；scene 不需要 load/save 配對，最後一行 save 就好
up scene do -f ops.txt
up scene do "add||資源生成器|MonoEntity,MonoObj" "save"    # 也可以直接帶參數
```

| 操作 | 說明 |
|---|---|
| `add\|<parent>\|<name>\|<comp,comp>` | 建節點並掛 component。parent 留空 = prefab root 下 / scene root 層 |
| `prefab\|<prefabPath>\|<parent>\|<name>` | 放 prefab 實例（prefab / scene 都支援；prefab 端就是裝 nested prefab 模組）。name 留空 = 用 prefab 自己的名字 |
| `comp\|<node>\|<comp,comp>` | 對既有節點加 component |
| `set\|<node>\|<comp>\|<field>\|<value>` | 設值。float / int / bool / string / enum（傳名稱）/ Vector3（`"x,y,z"`）/ Vector2（`"x,y"`）/ Vector4（`"x,y,z,w"`）/ Quaternion（`"x,y,z,w"` 或 `"x,y,z"` 歐拉角）。long（`m_TableEntryReference.m_KeyId`）超出 int 範圍會自動走 `longValue` |
| `ref\|<node>\|<comp>\|<field>\|<target>[\|<targetComp>]` | 指向另一個節點。targetComp 省略 = 用欄位宣告型別去找 |
| `aref\|<node>\|<comp>\|<field>\|<assetPath>` | 指向 asset（prefab / SO）。prefab 會按欄位型別取 component。內建 primitive 用 `builtin:Cube` / `Quad` / `Sphere` / `Capsule` / `Cylinder` / `Plane` / `Default-Material` —— 它們住在 `Library/unity default resources`，`AssetDatabase` 讀不到 |
| `addel\|<node>\|<comp>\|<field>` | 陣列 / List 欄位尾端加一個元素，回傳新 index；接著用 `set` / `aref` 補 `<field>.Array.data[<i>]`。**不能用 `set` 改 `.Array.size`**（ArraySize propertyType 走不進 ApplyValue） |
| `revert\|<node>\|<comp>\|<fieldPath>` | 清掉單一 property override，讓值回到繼承自 base / nested prefab 的值（**只有 prefab**）。`<comp>` 留空 = GameObject 本身（`m_IsActive`）。**執行時機排在存檔前 callback 之後**，否則 callback 會把 override 寫回來。存檔後會驗「真的不再是 override」 |
| `pos\|<node>\|x,y,z` | 設 localPosition（`<node>` 留空 = root）。**目標是 RectTransform 時會警告並指向 `rect`** —— Canvas relayout 會蓋掉 localPosition |
| `rect\|<node>\|<ax,ay>\|<w,h>\|<anchor>\|<px,py>` | UI 專用（**只有 prefab**）：寫 anchoredPosition / sizeDelta / anchorMin+Max / pivot，每格都可留空 = 不動。anchor 吃 preset 名（`center` / `top-left` / `stretch` / `stretch-h`…）或 `minX,minY,maxX,maxY` |
| `scale\|<node>\|x,y,z` | 設 localScale（**只有 prefab**；`<node>` 留空 = root） |
| `rot\|<node>\|x,y,z` | 設 localEulerAngles（**只有 prefab**；`<node>` 留空 = root，複製出來的 prefab 要歸零殘留旋轉就靠這個） |
| `active\|<node>\|<true/false>` | 設 GameObject.activeSelf（含 nested prefab override 記錄與 reload 驗證；第二格必填） |
| `idx\|<node>\|<siblingIndex>` | 調 sibling 順序。**child 順序＝優先序**（value source / condition 取第一個成立的），負數從尾端算（`-1` = 最後） |
| `mv\|<node>\|<newParent>` | 換 parent（scene 與 prefab 都支援） |
| `copyfrom\|<srcPrefab>\|<srcNode>\|<dstParent>[\|<newName>]` | **跨 prefab** 複製整棵子樹（只有 prefab）。nested 實例會被重建成真實例（override 保留），指向子樹外的引用依 hierarchy 相對路徑重映射到目的 prefab 的同路徑節點。見下面「跨 prefab 搬子樹」 |
| `rename\|<node>\|<newName>` | 改節點名（`<node>` 留空 = root）。**只對沒掛 `AbstractDescriptionBehaviour` 的節點有意義**，其餘存檔後會被自動命名蓋掉，見 [naming.md](naming.md) |
| `auto\|<node>` | **重跑 `[Auto*]` 綁定 —— 結構改完一定要下這行**，見下面 |
| `del\|<node>` | 刪節點 |
| `delcomp\|<node>\|<comp,comp>` | 移除節點上的 component。不存在就跳過（語意是「確保它不在」）。prefab 版 `<node>` 留空 = root |
| `delmissing\|<node>` | 移除該節點上所有 MissingScript；已刪 C# 型別後無法用 `delcomp` 時使用。prefab 版 `<node>` 留空 = root |
| `save` | 存 scene（**只有 scene**；prefab batch 結束自動存） |
| `mark\|<label>[\|<node>]` | 給節點取個短名，之後用 `$label` 代換。不給 `<node>` = 標記上一個建立節點的操作 |

**`add` / `comp` / `set` / `ref` / `aref` / `addel` / `revert` / `pos` / `rect` / `scale` / `rot` / `delcomp` / `delmissing` 的 `<node>` 留空 = prefab root**
（`MonoEntity` / `MonoObj` / `NetworkObject` 都掛在 root 上）。scene 版沒有這個語意 ——
scene 沒有唯一 root，第一段一定要是 root object 名稱。

節點名含 `/` 或換行時的逃逸規則見 [naming.md](naming.md)。

## 跨 prefab 搬子樹：`copyfrom`

把一支 prefab 拆成 base + variant（或把模組從 A 搬到 B）時用。典型流程：

```bash
# 1. 先做一份「拆之前」的快照當來源。
#    ⚠ 快照的**檔名**要跟目的 prefab 的 root 名稱一樣（見下面的命名陷阱），所以放在別的資料夾
up prefab copy "<原檔>" --out "Assets/…/_snap/<目的 root 名>.prefab"
up prefab do   "Assets/…/_snap/<目的 root 名>.prefab" "auto|"   # 讓自動命名節點跟著新 root 名字改掉

# 2. 目的 prefab 先 `auto|` 存一次，讓它的自動命名節點也定下來，路徑才對得上
up prefab do "<目的 prefab>" "auto|"

# 3. 搬
up prefab do "<目的 prefab>" \
  "copyfrom|Assets/…/_snap/X.prefab|<srcNode>|<dstParent>" … "auto|"
```

要點：

- **`Object.Instantiate` 會扯斷 nested prefab 連結**（2026-09-15 實測），所以 `copyfrom`
  對「來源是 prefab 實例 root」的節點一律 `InstantiatePrefab` + `SetPropertyModifications`
  重建。複製完一定要 `up prefab read` 確認還有 `(prefab:res:…)` 後綴。
- **實例上「額外加的 GameObject / Component」不會被複製**（它們不在 PropertyModifications 裡），
  有的話 log 會明講，要自己補。
- **兩棵互指的子樹**（拆件模組 ↔ 部件視覺）不管先搬哪一棵，第一輪都有一邊指不到；
  整批 ops 跑完會自動再解一次（log 的 `# copyfrom 延後解引用`），所以**互指的子樹要放在同一批 ops 裡**。
- 仍解不掉的引用會逐條印出「哪個欄位 → 來源的哪條路徑」，用 `ref` 手補。

### 命名陷阱：自動命名節點跟著 **asset 檔名**走

掛 `AbstractDescriptionBehaviour` 的節點（`[Anim] <root 名>`、`[Follow] [Anim] <root 名>`）
在存檔前 callback 會被改成「root 名稱」，而 **root 名稱又會被存檔改回 asset 檔名**
（`rename||X` 對 root 下去存完還是檔名）。所以：

- 改 prefab 檔名 = 改 root 名 = 改 `[Anim] …` 節點名，三件事連動，不用手動 rename。
- `copyfrom` 的路徑重映射是走**字面路徑**比對，來源與目的的 `[Anim] …` 名字不一樣就對不上。
  快照檔名取成目的 prefab 的 root 名，再各跑一次 `auto|`，是最省事的對齊方式。

## `swap-script` —— C# 重構之後把舊型別的資料搬到新型別（離線）

```bash
up prefab swap-script "<prefab>" --from OldType --to NewType --drop _oldOnlyField --dry-run
```

用在「欄位被搬到另一支 C# 型別、名字沒變」的重構收尾：直接改 YAML 裡的 `m_Script` guid，
**同名 serialized 欄位原樣被新型別吃下去**，不同名的留在檔裡讓 Unity 下次存檔丟掉
（想當場清掉就 `--drop`）。

**為什麼不能用 `comp` + `set` 手搬**：舊型別的 C# 一旦刪掉那些欄位，值就變成孤兒 ——
Unity 載入時直接丟棄，`up prefab peek` 只會回「型別上沒有這個欄位」。值只還活在檔案文字裡，
任何走 Unity API 的搬移都會搬到空值。

- `--dry-run` 同時是**讀孤兒欄位的唯一手段**（會把每個 document 的原始 `欄位: 值` 印出來）
- `--fileid <id>`（可重複）只換指定的幾顆；留空 = 這份檔案裡全部
- 只處理**自有 document**。variant 上繼承節點的型別由 base 決定，換不了 —— 命中 0 筆時
  第一個嫌疑就是這個，要去 base 改
- 改完是純文字改檔：Unity 端要 reimport（切回 Editor / Ctrl+R）才看得到，
  離線索引要 `up index`。備份放 `Temp/uprefab-swapscript-backup/`

### ⚠ Unity 開著這個專案時**不要**離線改：值會不可逆地消失

離線改的是磁碟文字，Editor 記憶體裡還是舊的那份。使用者（或任何自動存檔）之後存一次
這支 prefab，Unity 就用 **pre-swap 的記憶體整份覆寫**檔案 —— 而且因為 C# 上已經沒有那些
欄位，序列化時**直接不寫出來**。結果不是「改動被還原」，是**四個欄位的值從檔案裡永久消失**，
`.bak` 之外救不回來。這在本專案實際發生過一次（Boss 可拆卸神像廟 prefab，8 顆全滅）。

所以 CLI 現在會**偵測 `Temp/UnityLockfile` 並拒絕寫入**。三個選項由上而下優先：

1. **改走 Unity 端寫入**（Editor 開著時的正解）—— 記憶體與磁碟一致，之後怎麼存都不會丟：
   ```bash
   up prefab swap-script "<prefab>" --from Old --to New --dry-run   # 先把舊值讀出來
   up prefab do "<prefab>" 'comp|<node>|New' 'ref|<node>|New|_f|<target>' \
                           'set|<node>|New|_i|0' 'auto|<node>' 'delcomp|<node>|Old'
   ```
2. 關掉 Unity Editor 再跑 `swap-script`。
3. `--force` 硬跑，然後**立刻**切回 Editor 按 Ctrl+R reimport，中間絕對不要在 Editor
   裡動或存這支 prefab。

`--dry-run` 不受限制（純讀），而且它是**讀舊值的唯一手段** —— 選項 1 的第一步就靠它。

這條規則**不只適用於 `swap-script`，而是所有離線改 prefab / scene YAML 文字的手段**（含
手寫 sed / python）。只要 Editor 記憶體裡有那份資產的舊狀態，它存一次檔就整份覆寫磁碟；
而 C# 上已不存在的欄位在重新序列化時會**直接不被寫出來** —— 結果不是「被改回舊值」，
是「值不見了」，`SerializedObject` / `prefab peek` / `prefab locate` 一律看不到孤兒欄位。

**離線索引與 Unity 端不一致時，一律以 Unity 端（`prefab locate` / `peek`）為準** ——
`up find` 讀的是可能已過期的索引，會給出假訊號。

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
- `prefab do` 會檢查 `SaveAsPrefabAsset` 成功，並 reload 驗證可推導的 touched 欄位；至少
  `active` 一定驗證。`auto` 若無法完整推導，摘要會明講 unsupported，不會假裝已驗。
  `--quiet` 只壓縮成功 log，錯誤仍保留完整行號與下一步線索。
- 要人工補驗時用 `prefab peek`；`prefab read` 已無磁碟快取（2026-09-11 移除），存檔後直接 read 即是現況。

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

### variant 上的繼承節點照樣用 `auto`，不要預防性改寫法

曾經流傳一條「在 variant 上對繼承自 base 的既有節點改欄位，`auto` 不會寫進
`m_Modifications`、改動靜默遺失」的說法 —— **2026-08-24 實測推翻，重現不出來**：

- 在 variant 的繼承節點上純反射寫欄位 + `SaveAsPrefabAsset`（**不**呼叫
  `RecordPrefabInstancePropertyModifications`），YAML 照樣長出 `propertyPath` override。
  Unity 存 prefab contents 時是真的做 diff。
- 陣列從空長到 1 筆也一樣（base `_conditions: []` → variant 加一個 `[If]`，
  `Array.size: 1` 與 `data[0]` 都正確寫入）。
- 甚至不用跑 `auto`：只下 `if|`，存檔前的 `OnBeforePrefabSave` callback 就會把繼承節點的
  `_conditions` 補上。

所以**不要為了 variant 預防性改用 `addel` + `ref`**，直接 `auto`，寫完照常 `peek` 驗一次。
真的遇到欄位是空的，先看 `auto` 的輸出（`[Auto*] 欄位綁上 N、沒綁上 M`）分辨是「綁上了
沒存進去」還是「根本沒綁上」，再往兩個方向查：目標 component 是否來自**巢狀 prefab**
（override 規則與 variant base 不同）、或 `[Auto*]` 本來就合法地綁不到（型別／層級不符）。

**巢狀 prefab 實例上的例外（2026-09-15 實測）**：steal 飛行怪 variant 上，`[State] Chase/[Transition] => RunAway`
這類「住在 nested prefab 實例（CharacterModules）裡、又繼承自 base」的 TransitionBehaviour，加了 `[If]` 子節點後跑
`auto|CharacterModules/Character FSM`，log 印「綁上 N、沒綁上 0」但存檔後 `_conditions` 仍是 base 的舊值（沒長出
array override）。改用 `addel` + `ref|…|_conditions.Array.data[i]|…` 就寫得進去；同一批操作在模組**源** prefab 上
`auto` 完全正常。所以上面的「不要預防性改寫法」只對 variant 直接繼承的節點成立；**目標在 nested 實例裡時，`auto` 完
一定 `locate --members _conditions` 驗，空的就補 `addel`+`ref`**。工具側待辦見 AgentToolTODO.md。

`auto` 不是 Python 端做的 —— `up` 只把字串轉發給 Unity，實作在
`MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/EditResolve.cs::RunAuto`，
真正寫欄位的是 `AutoAttributeManager` 的反射。

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

⚠️ **反過來，callback 也可能自動換掉 sync 元件的型別**。2026-08-27 在「中控開關」「中控室建築候選」
上新增 VarInt + `NetworkedVarTag` 後，root 的 `NetworkedVarSyncFloat4` /
`NetworkedVarSyncBool4Float4` 被自動升級成 `NetworkedVarSyncArray`，新 VarInt 自己進了
`_syncInts`，既有 `_syncFloats` / `_syncBools` 內容保留。所以 `peek` 驗的時候別預設元件型別沒變。

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

## variant 的 parent 不能「抽換」，只能重建（而且會斷外部引用）

想把一顆 variant 改成繼承另一份 base prefab 時，兩條看起來合理的路都是死路：

- **改 `SerializedObject` 沒用。** `m_SourcePrefab` 寫在 .prefab 的
  `--- !u!1001 PrefabInstance` document 上，不在 root GameObject 上 ——
  `new SerializedObject(prefabAsset).FindProperty("m_SourcePrefab")` 一定回 `null`，
  `AssetDatabase.SaveAssets()` 也存不回 prefab 內容。寫成 Editor 工具會**靜默無作用**。
- **硬改 YAML 的 guid 也不行。** override 的 `target: {fileID, guid}` 裡的 fileID 是
  「該物件在 parent 檔案中的 fileID」；對**未被中間層覆寫的繼承物件**，這個值由 Unity 依
  `base 物件 fileID + PrefabInstance id` 動態算出，**不寫進 YAML**，外部無從重算或對照。
  實測某顆 prefab 的 93 條 override 只有 8 條能對到新 parent，其餘全丟。

**唯一可行的做法是重建**（覆蓋同路徑可保留原 .meta guid，不斷檔案級引用）：
preview scene 裡 `PrefabUtility.InstantiatePrefab(新 parent, previewScene)` → 補回原本的
自有節點 → `PrefabUtility.SaveAsPrefabAsset(instance, 原路徑)`。對 prefab instance 存檔才會
產生 variant；`LoadPrefabContents` 那條路存出來是普通 prefab，不行。

**必然的副作用：所有指向繼承節點的外部引用都會變 null。** 引用是 `{guid, fileID}` 兩層，
guid 只保證「同一個檔案」，沒有「指向 prefab 整體」的寫法（`100100000` 只用於
`m_SourcePrefab`）。一般欄位引用 variant 時寫的是 root 或 root 上 component 的 fileID，
而 variant 的 root 是繼承節點、fileID 是動態值 —— 重建後對不上（pool prewarm、SpawnMarker
這類指向 root 的最容易中）。動手前先用 guid 反查出引用點清單。

推論：要讓別的 prefab 引用 variant 內部的東西，指「variant **自有新增**的節點」
（fileID 實寫在 YAML）比指繼承節點耐操。
