# Progress

- 新增 AssetEdit：ScriptableObject asset 的建立/編輯 API（與 PrefabEdit/SceneEdit 同一套 batch DSL 風格）。
- 新增 EditGid：GlobalObjectId 連結（Editor 產的 scene 物件連結）→ 定位物件並匯出子樹；`PrefabTextReader.ExportNode` 抽出來供其重用。
- 路徑解析支援同名節點索引 `名稱[n]`（read 與 do 共用 `EditResolve.TryNode`），錯誤訊息列出的子節點也會替同名的標上 `[n]`。
- 新增 EditAnchor：離線索引 anchor（`資產#fileID`）→ 合併後可直接餵給 `--node` 的完整路徑（`up find --resolve`），路徑生成抽成 `EditResolve.PathOf`，scene 的 root 段也支援 `[n]`。
- 新增 batch DSL `addel|<node>|<comp>|<field>`：陣列/List 尾端加元素（`set` 改不了 `.Array.size`，ArraySize propertyType 走不進 ApplyValue），prefab / scene 兩邊共用 `EditResolve.AddArrayElement`。
- `ApplyValue` 支援 LayerMask 欄位（整數位元遮罩 / Everything / Nothing / 逗號分隔 layer 名稱）。
- prefab batch DSL 新增 `prefab|<prefabPath>|<parent>|<name>`：在 prefab asset 內放 nested prefab 實例（把模組 prefab 裝進宿主 prefab 用），語意與 scene 版一致。
- prefab batch DSL 的 `comp` / `set` / `ref` / `aref` / `addel` 的 `<node>` 留空 = root（`MonoEntity` / `MonoObj` 都掛在 root，之前只有 `add` / `delcomp` 允許），訊息一律走 `EditResolve.Describe`。
- 新增 `idx|<node>|<siblingIndex>`（prefab / scene 共用語意，負數從尾端算）：child 順序在 MonoFSM 裡就是 value source / condition 的優先序，之前沒有調整順序的手段。
- 路徑支援 `\/` 逃逸：節點名本身含 `/`（`=> Localized: GameplayUI/grab` 這類自動命名）時 `Transform.Find` 會誤判成階層，改走自掃子節點；`ChildLabels` / `PathOf` 列出的路徑也會自動逃逸。
- 新增 `up prefab copy --out <path> [--name]`（`PrefabEdit.CopyAsset`）：複製成獨立 prefab 並順手改 root 名稱，拿既有 prefab 當模板改比從零建安全（`variant` 是要保留繼承時才用）。
- prefab batch DSL 新增 `rename|<node>|<newName>`（`<node>` 留空 = root）：複製模板後 root / 節點名字還是舊的，之前沒有改名手段。注意帶 `AbstractDescriptionBehaviour` 的節點存檔後會被自動命名蓋掉。
- `up refs` 的 inbound 每筆命中會接上引用來源的 `_note`（`# 安全區慢慢充電`）：節點名是自動命名的（`[Action] Stamina 電力 += 2`），看不出用途，只印路徑會讓人對每一筆再下鑽一次 `read` 才知道哪筆是要找的。
- note 抽取抽成共用的 `MonoFSM.Editor.NoteText`（走 cache 過的反射，不 new SerializedObject —— 摺疊行要數整棵子樹的 note，而 `PrefabTextReader.Layered` 會把同一棵樹重跑幾十次探深度），涵蓋 `_note`（`AbstractDescriptionBehaviour` / `AbstractSOConfig`）與 `Note` 的舊 `note` 欄位。三處輸出面接上：hierarchy node 行尾（`up read` / `up scene ls` / `up obj`，同時把 `_note` 從欄位堆移除，免得被 `_maxFieldCharsPerComponent` 截掉）、摺疊行的 `(+N nodes, M notes)`、FSM markdown 的 state / transition / condition / action / variable。
- `up refs --out` 的引用目標補上 note（原本只有 inbound 有，順著引用往下追一樣看不出用途）；目標是 Transform 這種本身沒 note 的 component 時退回節點層級找。
- 路徑解析新增 `\n` 逃逸（`EditResolve.SplitPath` / `Unescape` / `EscapeName`）：localized 自動命名會把含換行的譯文塞進節點名，CLI 的 op 是一行一個，不逃逸就完全指不到。hierarchy 匯出的節點名也一起逃逸（`HierarchyTextExporter.NodeName`），不然候選抄不回來。
- `set` 支援 long（`ApplyValue` 的 Integer 分支超出 int 範圍走 `longValue`）：`m_TableEntryReference.m_KeyId` 原本會 OverflowException；四處顯示端（`CompactValueFormatter` / `UnityTypeFormatter` / `PrefabToTextExporter` / `EditResolve.Preview`）與 `ComponentDefaultCache` 的預設值判斷一併改讀 `longValue`，否則 key 會被截斷成負數、或被誤判成 0 而整個欄位不輸出。
- `up prompt --check`（`PromptEdit.Check`）：只驗不改，印出每顆 value source 組出的字串＋inspector 的「Token 檢查」報告（`LocalizedStringValueSource.GetTokenReportEditor`）。手工組的 `ConditionRef` / `SmartStringTokenBinding` 是 `--case` 蓋不到的，之前只能開 Unity 用眼睛看。順手修 `PromptEdit.ResolveNode`：改用逐一比 `child.name`（`Transform.Find` 對名稱含換行的節點一律找不到）並支援 `\/` `\n`，路徑錯也會把原因印出來。
- 2026-08-24 `up peek` 留空不再盲掃 public property（getter 會 native crash，managed catch 攔不到）；新增 `ProbeMineField` 麵包屑機制自動把閃退的屬性列入黑名單，以及 Inspector 右鍵「Dump 欄位 / 欄位+屬性 → 剪貼簿」（`ComponentDumpMenu`）
- 2026-08-24 `auto` 的回報改成「其中幾顆屬於繼承來的 prefab instance ＋ [Auto*] 綁上/沒綁上」；之前只回掃過幾顆 MonoBehaviour，「一顆都沒綁上」跟「全部綁上」的輸出一模一樣。順手查證：在 variant 上對繼承節點做反射寫入（[Auto*] 的做法）SaveAsPrefabAsset 會正確寫出 m_Modifications，不需要 RecordPrefabInstancePropertyModifications。
- prefab batch DSL 新增 `delmissing|<node>`，供 C# 型別已刪、無法再以 `delcomp` 解析名稱時，透過 Unity 官方 API 清除該節點上的 MissingScript。

## 2026-09-03 一批 uprefab 驅動的 op / probe 改動

設計理由、踩過的坑與驗收數字都寫在 `MonoFSM/Tools~/uprefab/PROGRESS.md`（那份會被 agent 實際讀到），
這裡只留索引：

- `revert|` op（`PrefabEdit.cs`）—— 清單一 property override。**必須排在 before-save callbacks 之後**，
  在那之前清會被 callback 原封寫回。見 PROGRESS「`revert|` —— 清掉單一 property override」
- override 星號（新增 `PrefabOverrideMark.cs`，`EditProbe` / `HierarchyTextExporter` 共用同一顆判準）。
  原本「LoadPrefabContents 看不到 root override」的疑慮實測推翻。見 PROGRESS「peek / locate / 寫後驗證吐 override 狀態」
- `rect|` op、`pos`/`scale`/`rot` 的 nodePath 留空 = root、`ApplyValue` 支援 Quaternion / Vector4。
  見 PROGRESS「`rect|` + `rot` 吃 root + `set` 吃 Quaternion」
- **Transform 系 op 的靜默假陽性**（`VerifyTouch` 的 expected 前移到 op 當下 +
  `RecordTransformWrite`）。這是本批最重要的正確性修正：驗證原本在存檔後才取 expected，
  「寫不進去」會被洗成「一致」。見 PROGRESS「Transform 系 op 的靜默假陽性」
- `EditResolve` 的路徑逃逸補「字面反斜線」，`PromptEdit` 改成轉呼 `EditResolve`
  （asmdef reference + `InternalsVisibleTo`，刻意不把 `EditResolve` 改 public）。
  見 PROGRESS「`prompt --var` 定位名字含字面 \n 的節點」

## GlobalObjectId 連結：prefab 裡的物件也要解得開（2026-09-09）

拿到 `globalId=` 連結最常見的時機恰好是它指的容器**沒開著** —— 那時 `EditGid` 只回一句
「來源不是 scene；物件可能已從那份資產裡刪掉了」。那句話是錯的（物件好好地在 prefab 裡，
只是 Prefab Stage 沒開），照它去查會整條線走歪，所以這批的重點是把錯誤訊息換成能執行的下一步。

- `TryOpenOwnerScene` 分出 `TryOpenPrefabStage`：`.prefab` 走 `PrefabStageUtility.OpenPrefab`
  （`--open` 才開，Stage dirty 一律拒絕），沒帶 `--open` 時 note 直接給兩條路 ——
  加 `--open`，或走離線 `up find`。刻意在 note 裡就講離線那條，不然下一步只剩「開 Editor」。
- `Locate` / `Peek` 尾端加 `NextCommand`：印出能直接貼的下一條指令。存在理由是
  **scene 與 prefab 兩側 `--node` 語意不同** —— scene 含 root object 名、prefab 不含，
  少印這行就等於要對方自己猜一次（實測猜錯：貼 `PPlayer/…` 給 `prefab read` 回「找不到子樹」）。
- `EditResolve.TryNode` 加 `TryDropRootName`：正常解析失敗、且第一段等於 root 名時切掉再解一次。
  那條路徑是給人複製的，在這裡罰一次沒有任何好處；只在失敗後才補，不影響真的有同名子節點的情況。
- 2026-09-09 `EditGid` 加 `Resolve` / `ResolveInPrefab`：`GlobalObjectIdentifierToObjectSlow` 不認
  Prefab Stage 裡的物件（實測 stage 開著也回 null），更不認沒開的 prefab。prefab 連結的 `targetObjectId`
  就是 imported asset 的 local fileID，所以改成自己掃：stage 開著比對 `GetGlobalObjectIdSlow`
  （含 nested），否則 `AssetDatabase.LoadAssetAtPath` 後比對 `TryGetGUIDAndLocalFileIdentifier`。
  結果是 **貼連結一次呼叫就拿到內容、不用開 stage**；`--open` 降為「順便開給人看」。
  Python 端原本 Unity 解不開會把離線索引結果接在失敗訊息後面印，兩段互相矛盾，改成 Unity 是主路、
  離線只在 Unity 沒回應時當備援（它本來就只能定位、不能給欄位）。
- 2026-09-09 派 agent 實測「貼 gid 連結 → 讀欄位」花了 7 次呼叫，6 次是被輸出帶偏，三個根因全修在工具層：
  (1) 葉節點匯出省略預設值 → `_boundType=Max`/`_percentage=0` 消失，agent 以為欄位不存在。`PrefabTextReader.Options`
  改成 root 無子節點時 `_excludeDefaults=false`（有子樹才是省 token 的地方）。
  (2) 同一支 prefab 但在匯出子樹外的引用印成 `res:<自己這支 prefab>`，看不出指到哪個節點。
  `CompactValueFormatter.FormatObjectRefCore` 加「同 root 就印相對路徑」。
  (3) `EditGid.Peek` 尾端「接著用 prefab read」讓 agent 以為 read 才是拿欄位的正解；欄位已印時改列「下鑽 / 單顆 component」兩條真正的後續路。
  Python 端 `up peek --node` 的錯誤訊息「不認得 --node。最接近：--node」自相矛盾，改成明說「屬於哪些子指令」並指向 `up prefab peek`。

- 2026-09-15 右鍵 Dump 對 reference 欄位多印 `= ValueInfo`（`IHierarchyValueInfo`，與 hierarchy 右欄同源：Var → CurrentValue、Condition → FinalResult、Getter → 取值）。起因是右鍵沒辦法像 CLI 用點路徑穿過 reference；先做成「被引用的 Component 各展一層」，太吵，改成只印這一個值。用 `s_refValueInfo` 旗標只在右鍵路徑開，CLI `peek` 留空「不呼叫任何 getter」的約定不動。右鍵版本同時把巢狀 [Serializable] 攤 2 層（`MenuDeep`）。
- reload 驗證的 `VerifyTouch.Serialized` 會在寫入當下 pin expected；同一批對同一欄位寫第二次（典型：`addel` 連加多筆）時，前一筆的 `array-size:1` 已過期卻仍拿來比對，報假失敗且附上誤導的「nested prefab override」提示。現在 set / ref / aref / addel 加 touch 前先 `RemoveAll(IsSameSerializedField)`，只留最後一次寫入（2026-09-16）

## 2026-09-17 `set` 支援 AnimationCurve

`AnimationCurveSetFloatValueSource._curve`、profile SO 的曲線欄位這類 AnimationCurve 過去沒有
寫入入口，整條「CLI 接線」流程會斷在「請人手動去 Inspector 畫一條」。`EditResolve.ApplyValue`
補上 `SerializedPropertyType.AnimationCurve`，語法 `[linear:|ease:|smooth:|flat:]t,v;t,v;…`。

- 預設是 `ease:`（Keyframe 切線 0）而不是 `linear:`：value source 的曲線幾乎都要 smoothstep，
  兩個關鍵幀 + 切線 0 就是最常見的形狀，讓最短的寫法直接給出可用結果。
- `linear:` 用手算斜率而不是 `AnimationUtility.SetKeyTangentMode`，避免 tangentMode 的
  版本差異，存出來的 inSlope/outSlope 是明確的數值。
- 存檔後的 reload 驗證還沒支援 AnimationCurve（會印 unsupported），要確認值有進去目前只能
  `peek`（只顯示型別名）或直接看 YAML 的 `m_Curve`。

- 2026-09-22 ops 的欄位分隔支援 `\|` 逃逸（`EditBatch.SplitFields`）。原因：自動命名會生出
  名字裡就有 `|` 的節點 —— `DistanceValueSource.Description` 是 `|a - b|`，`FloatMathValueSource`
  再把兩邊串成 `|a - b| min |c - d|`。在那之前這種節點**在 ops 裡完全指不到**，而且失敗方式很毒：
  `line.Split('|')` 直接把路徑切斷，`mark` 這種不驗路徑的操作還會靜默記下半截字串
  （實測 `mark|D1|…/[Getter] \|a - b\|` 記成 `…/[Getter] \`），錯要等到後面 `ref` 才爆。
  只解 `\|` 一種，其餘反斜線（路徑的 `\/`、`\\n`）原樣往下游丟，不然會跟既有的路徑逃逸打架。
- 2026-09-23 `ApplyValue` / `Preview` / `Snapshot` 補 Vector2Int / Vector3Int / RectInt（逗號分隔整數）。之前設 `AmbientLightningRender._countRange` 撞 default 分支只能靠 C# 預設值。
- 2026-09-23 `EditProbe.Dump`（peek / locate --members）反射找不到成員時退回 `SerializedObject.FindProperty`。Unity 內建 component（MeshRenderer…）的 `m_Materials` 只存在 native 端，C# 只有 `sharedMaterials` property，之前 `aref` 寫得進去但 peek 讀不回來。`m_X[0]` 會自動轉 `m_X.Array.data[0]`；native component 沒點名欄位時改用 SerializedObject iterator 列頂層可見欄位。故意保持「反射優先」—— MonoBehaviour 走反射才讀得到 property 和 deep 展開。
- 2026-09-23 `auto|` 改成可驗證（`PrefabEdit.RunAutoVerified`）：RunAuto 前後各快照每顆 MonoBehaviour 的 serialized [Auto*] 欄位（整個欄位攤成 leaf，object reference 留物件本身、存檔後才轉 path），真的變了的才加 `VerifyKind.AutoField` touch，存檔後逐欄比對；instance 上的 component 順手補 `RecordPrefabInstancePropertyModifications`。以前 auto 只記一筆 unsupported，「綁上 N、存檔後是 base 舊值」就會印 OK。
  - 踩過的坑：leaf 走訪只能往 Generic（陣列 / 巢狀 class）裡鑽。ObjectReference 底下有 `m_FileID` / `m_PathID`，每次 reload 都不同，第一版因此永遠報 mismatch。
  - expected 在存檔**前一刻**重取（`RepinBeforeSave`），不是 auto 當下、也不是存檔後：OnBeforePrefabSave callback 可能又重綁一次；存檔後取則會被「沒成為 override 的寫入」洗回 base，驗證失去意義（跟 TransformValue 同一個道理）。
  - 根因沒重現：自建 fixture（variant ⊃ nested、variant ⊃ nested ⊃ nested，含中間層也有 `_conditions` override）上，反射寫 nested 實例的 [AutoChildren] 陣列、整顆 `auto|` 後既有 `_targets` override / added 節點全都正常保留 —— SaveAsPrefabAsset 本來就會 diff。PPlayer 上的現象（09-15 steal、09-17 手電筒）所以沒找到 uprefab 端的根因；09-17「三種完全不相關的改動一起退回、連 added 節點都不見」比較像整份檔案被舊內容蓋掉（Prefab Mode stage 那條路徑，見下條），但沒證據。
- 2026-09-23 `OverrideGuard`（EditOverrideGuard.cs）：`prefab do` 開始時記下既有 m_Modifications 與 added GameObject/Component，reload 後比對「這批沒寫的」override 有沒有消失、被蓋回 base、引用變 null、added 節點消失，有就印 `# ⚠ 連帶損失`。逐欄驗證只驗「這批寫了什麼」，驗不到「沒寫的被順手蓋掉」—— 09-17 的洞就在這。故意**不**抓一般值變動（callback / auto 本來就會合法改值，全抓只會變成噪音）；被這批刪掉的節點、m_Name / m_RootOrder 跳過。只警告不擋：存檔已經發生，擋了也救不回。
- 2026-09-23 `prefab do` 目標正開在 Prefab Mode 就拒絕（`--force` 才寫）。實測 stage 乾淨時 Unity 會自己從磁碟 reload，沒事；危險的是 stage 有未存改動 —— 之後存檔或退出按 Discard 會拿 stage 的舊內容整份蓋回（09-16 實際發生）。理論上只擋 dirty 就夠（乾淨的 stage 會 reload 成新內容），目前照使用者要求一律擋、訊息裡帶出 dirty 與否；嫌擋太多時可以放寬成只擋 dirty。Python 端只有帶 `--force` 才傳第 4 個參數，C# 沒編到新 overload 時一般的 do 不會壞。
