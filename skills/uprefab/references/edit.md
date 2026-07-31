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
| `prefab\|<prefabPath>\|<parent>\|<name>` | 放 prefab 實例（**只有 scene**） |
| `comp\|<node>\|<comp,comp>` | 對既有節點加 component |
| `set\|<node>\|<comp>\|<field>\|<value>` | 設值。float / int / bool / string / enum（傳名稱）/ Vector3（`"x,y,z"`） |
| `ref\|<node>\|<comp>\|<field>\|<target>[\|<targetComp>]` | 指向另一個節點。targetComp 省略 = 用欄位宣告型別去找 |
| `aref\|<node>\|<comp>\|<field>\|<assetPath>` | 指向 asset（prefab / SO）。prefab 會按欄位型別取 component |
| `addel\|<node>\|<comp>\|<field>` | 陣列 / List 欄位尾端加一個元素，回傳新 index；接著用 `set` / `aref` 補 `<field>.Array.data[<i>]`。**不能用 `set` 改 `.Array.size`**（ArraySize propertyType 走不進 ApplyValue） |
| `pos\|<node>\|x,y,z` | 設 localPosition（**只有 scene**） |
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
