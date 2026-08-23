# catalog —— Action / Condition 目錄

組 FSM 的成本大半不在改 prefab，而在「有哪些 component 可以用、每個欄位要填什麼」。
`up catalog` 把這份資訊從 .cs 離線抽出來（掃全庫約 2 秒，跟著 `up index` 一起建），
一次列完用途與 serialized 欄位，取代逐檔 Read 原始碼。

```bash
up catalog                        # 全部 Action（預設）
up catalog condition              # 全部 Condition
up catalog action grab            # 名稱或說明含 'grab' 的 Action
up catalog --type SwitchGrabSlotAction   # 單一型別：完整說明 + 每個欄位 + tooltip
up catalog action --missing       # 缺 /// summary 的待補清單
```

`kind` 沿繼承鏈遞移判定，所以中間層 abstract 的子類也會歸到正確類別
（abstract 本身預設不列，要看加 `--abstract`）：

| kind | 基底 | 是什麼 | 數量 |
|---|---|---|---|
| `action` | AbstractStateAction | 狀態進出／事件觸發時做事 | 152 |
| `condition` | AbstractConditionBehaviour | 轉換與 `[If]` 的判斷 | 77 |
| `getter` | AbstractGetter / AbstractValueSource | 提供數值給欄位引用 | 78 |
| `render` | AbstractRenderBehaviour | 每 render frame 的畫面表現，不改狀態 | 36 |
| `handler` | AbstractEventHandler | 收事件並分派給底下的 action | 31 |
| `var` | AbstractMonoVariable | 變數本體 | 24 |
| `so` | ScriptableObject | tag / config / data 類 asset | 600+ |

**歸類看的是實際基底不是命名** —— `AnimatorSetBoolAction` 名字叫 Action，
但它繼承 AbstractRenderBehaviour，所以在 `render` 裡。挑不到就換一類找。

## 輸出怎麼讀

```
SwitchGrabSlotAction ─ 切換 GrabSlotHolder 欄位的進入點。
    _holder:GrabSlotHolder  _slotIndex:int  _slotIndexVar:VarInt
GrabbableHandlerAction ─ ~ 放在可被抓取物件的 EffectReceiver 底下
RotateGrabbedYAction ⚠無說明  Assets/0_Gameplay/RotateGrabbedYAction.cs
    _grabber:NetworkedGrabber  _degrees:float
```

- `─` 後面是說明，清單模式只給第一句，完整版用 `--type` 或 `-v`。
- `~` 代表說明來自 `//` 註解而非正式 `/// <summary>`，可信度較低。
- `⛔Obsolete` 代表這個型別（或它的基底）標了 `[Obsolete]`，**不要挑它**。
  這類預設不列出來，`--obsolete` 才會顯示 —— 整批 `VarXxxProviderRef` 都在裡面。
- `⚠無說明` 會直接附上檔案路徑 —— 那是「要嘛去讀、要嘛去補」的訊號。
- `[Auto]` / `[AutoParent]` / `[AutoChildren]` 前綴的欄位由框架自動填，
  **不要在 prefab 上手動指定**，組完下 `auto|` 就好。

`up fields <Type>` 也會在 Unity 的欄位真值前面補上這裡的說明與欄位 tooltip，
所以要看「繼承來的欄位也算進去的完整清單」時用 `fields`，要挑型別時用 `catalog`。

## `/// <summary>` 撰寫規範

目錄的品質完全等於原始碼註解的品質，所以新寫或改動 Action / Condition 時，
class 上一定要有 `/// <summary>`，並且：

- **第一句就講清楚用途，而且能獨立看懂** —— 清單模式只顯示第一句。
  寫「切換 GrabSlotHolder 欄位的進入點。」而不是「處理 slot 的邏輯。」
- **寫「什麼時候用它」而不是「它怎麼實作」** —— 實作看程式碼就好，
  目錄要回答的是「我這個需求該挑哪一顆」。
- **掛的位置是關鍵資訊就寫進去**（放在 State 的 OnEnter？放在被打的一方的 EffectReceiver 下？）。
- 欄位語意不是望文生義時，在欄位上加 `[Tooltip("…")]` —— 那也會進目錄。
- FIXME / TODO 照舊寫，但別讓它取代 summary（純待辦的註解會被目錄忽略）。

**遇到 `⚠無說明` 而你為了工作實際讀了那份 .cs：讀完順手補一段 summary 再走。**
目錄沒有別的補齊來源，補一次省掉之後每一次的重讀。反過來說，沒讀過的不要憑欄位名亂猜補。

補完跑 `up index` 讓目錄跟上（catalog 每次都全庫重建，不需要特別指定）。

## 實作

`MonoFSM/Tools~/uprefab/catalog.py` 純字串比對抽取（不解析 C# 語法），資料進
`.uprefab.db` 的 `catalog` 表，欄位：class / path / kind / bases / is_abstract /
summary / has_doc / fields(JSON)。認的是「宣告名 == 檔名 stem」的那個 class，
所以一檔多 class 時只收主要那個 —— 這跟 Unity 對 MonoBehaviour 的要求一致。
