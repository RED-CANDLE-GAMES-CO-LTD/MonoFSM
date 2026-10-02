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
- 2026-09-25 `prefab read`（PrefabTextReader）輸出有行首 `~` / `+` 旗標時在 header 補一行圖例。沒圖例時 agent 把 `~[If] v_IsDead == True`（inactive）讀成生效中的 receiver gate，整個調查結論反了。
- 2026-09-25 GameObject layer 讀寫（EditLayer.cs）：`layer|<node>|<名字>[|children]`（prefab / scene do），名字打錯列可用 layer、只差大小寫會直接提示正確寫法；prefab 版走 VerifyKind.Layer 做 reload 驗證並記 nested override。read / peek / locate 的節點行在 layer 非 Default 時印 `layer=X`。另外把「AbstractDetectionSource 一律 Detector layer」慣例做成檢查：read header 列整棵子樹違規＋修正指令；`prefab do` 只對「這批新造成的」違規逐條警告（載入時先記既有違規路徑，reload 後比對），既有的只報數量 —— 不然每次 do PPlayer 都會被 4 顆既有的 cast source 洗版；scene 沒有存檔後驗證，改在 add / comp / layer 當下檢查該節點子樹。起因：新 trigger detector 放在 Default layer 時 uprefab 完全看不出來，只能進 runtime 猜。
- 2026-09-25 layer 檢查範圍從 `AbstractDetectionSource` 縮到 `RequiresDetectorLayer == true`（目前只有 TriggerDetectorSource）：cast / overlap 的 GameObject layer 不影響它們打到什麼，警告只會變噪音。layer 名字改讀 `DetectorLayer.Name`，不再各自寫死 "Detector"。
- 2026-09-25 新增 `EditBounds`（`up prefab bounds`）：renderer AABB + 沿長軸粗細分佈。為什麼走 Unity 端不離線解析 FBX 見 `MonoFSM/Tools~/uprefab/PROGRESS.md`。
- 2026-09-27 prefab batch 新增 `invoke|<node>|<comp>|<method>`：按 prefab 內 component 上的 Odin `[Button]`（agent 按不到滑鼠，edit-time 排版工具只能靠這條）。跟 `up asset invoke` 故意不同：asset 那邊 invoke 不可回滾所以不收進批次；prefab 這邊改的是 LoadPrefabContents 的記憶體副本，任一行失敗整批不存檔，原子性是真的，所以收進批次。方法有回傳值就印出來 —— tool 被自己的檢查擋下時通常只寫 fail reason 不丟例外，沒印的話「被擋」跟「重跑結果一樣」分不出來。
- 2026-09-29 `up peek` 在 Edit Mode 跳過 Renderer.material(s) / MeshFilter.mesh / Collider.material，提示改讀 shared 版：這幾個 getter 在 Edit Mode 會 new instance 蓋掉 shared 欄位，是「讀」卻偷改 scene（查發財車引擎蓋透明時踩到）。跟 ProbeMineField 黑名單分開：黑名單是會閃退，這個是有副作用，Play Mode 讀 instance 是合法的所以只擋 Edit Mode。scene peek 的提示也從 `--comp` 改成 positional（scene 版沒有 --comp）
- 2026-09-29 `up asset-refs` 全庫掃描跳過沒 pull 的 git LFS pointer：microverse demo / splines examples 有 18 顆 TerrainData 只是 LFS pointer，Unity 當壞檔，GetDependencies 碰到就噴「File may be corrupted」洗 Console。GetDependencies 內部 log 攔不到，只能掃之前讀檔頭（<1KB 才讀，其他只多一次 stat）；Packages/ 要用 FileUtil.GetPhysicalPath 轉實體路徑。輸出會標跳了幾顆，免得漏掃沒人知道。why-in-build 沒加，它只從 build root 往下走，碰不到 demo
- 2026-09-29 PrefabEdit / SceneEdit / AssetEdit 的 `EnsureDirectory` 修「多出 `<資料夾> 1`」：agent 先 `mkdir -p` 再 `up prefab variant`，資料夾在磁碟上但 Unity 還沒 import → IsValidFolder=false → CreateFolder 撞到磁碟上的同名資料夾，Unity 自動改建「Enemy 廢鐵青蛙 1」，後面存檔還是寫進原本那個，留下一個空資料夾（廢鐵青蛙 variant 那次）。改成每層先看磁碟：已存在就 ImportAsset（不行再 Refresh），不存在才 CreateFolder，而且核對 CreateFolder 回傳的實際路徑，被改名就直接 Abort 並叫人刪掉多出來的。三份 helper 還是各自一份沒抽共用，因為抽成跨檔案的新成員 hot reload 做不到
- `PrefabEdit` 的 `rename` 補上 `RecordPrefabInstancePropertyModifications`：改的是 nested prefab instance 上的節點時，m_Name 的 override 以前沒被記下來，存檔後名字會變回原本的（2026-09-29 StreetLamp 的 MPB action 改名踩到）
- 2026-09-29 `aref` 指到剛寫到磁碟、Unity 還沒 import 的 asset（Auto Refresh 關著 / Editor 失焦）會報「找不到 asset」。`AssetRef.Resolve` 改成檔案存在就先 `ImportAsset` 再找，agent 自己產的 .mat 不用再請使用者按 Cmd+R。
- 2026-09-30 新增 `MatEdit.SetParent`（`up mat set-parent`）：把 .mat 改成 Material Variant / 解除，前後快照合併後的值、跟 parent 一樣的 revert、不同的寫回成 override。設計理由與坑見 `MonoFSM/Tools~/uprefab/PROGRESS.md` 同日條目。
- 2026-10-01 `scene do` 加 `dup|<node>|<newName>`，`rect` 也開放給 scene。起因是大廳加退出按鈕時 scene 沒辦法複製節點，只能 `add` 再一格一格 `set` 補欄位。dup 用 `Object.Instantiate(src, parent, false)`，因為它本來就會把子樹內部互指的 reference 對到複本、外面的維持原樣，照抄一顆 UI 再改字要的就是這個。Instantiate 會扯斷 prefab 連結，所以原節點是 instance root 時改走 `EditCopy.InstantiateLikeInstance`（從 copyfrom 的 Rebuild 抽出來共用：同一個來源 asset + PropertyModifications），失敗就退回 Instantiate 並印警告；子樹裡的 nested instance 和「節點在 instance 裡面」的情況只印警告不重建（沒做 copyfrom 那套 remap，scene 端還沒碰到需要）。撞名故意擋下來，不像 `add` 那樣跳過，不然批次後面的 `$` 會默默改到舊節點。`rect` 本體搬到 `EditBatch.ApplyRect` 讓兩邊共用：prefab 端另外做存檔驗證 touch 和 override 記錄，scene 端只標 dirty。故意不用 `Unsupported.DuplicateGameObjectsUsingPasteboard`（就是 Ctrl+D）：它最完整，但要靠 Selection、而且是 Unsupported API。
- 2026-10-01 `EditBatch` 的 `$$` 跳脫改成參數任何位置都生效：以前 `Expand` 只看參數開頭，`$BR/…/Set $$[Var] x` 的子路徑段會帶著 `$$` 去找節點，回「找不到節點」卻看不出原因（插電救援踩到）。只解使用者寫的那段（rest），`_last` / mark 存的是真實節點路徑，再解一次會把名字裡真的 `$$` 弄壞。沒用佔位符：代換只認參數開頭，直接 Replace 子路徑段就等價。單一 `$` 接非識別字（`$[Var]`、`${token}`）照舊原樣保留。另外失敗行只要有參數被代換/跳脫過，就多印一段「原始 → 實際」—— 錯誤訊息自己講清楚是代換造成的，下一隻 agent 不用猜。
- 2026-10-01 `PrefabEdit.Batch` 加 5 參數 overload（dryRun）：ops、copyfrom 收尾、存檔前 callback、revert 全照跑，只跳過 SaveAsPrefabAsset 和 reload 驗證，finally 照常 Unload。callback 也跑是因為它會改 [Auto*] 值，試跑要盡量接近真跑。Prefab Mode 開著不擋 dry-run（不寫檔就不會互蓋），只提示跑的是磁碟版。Python 端 `--dry-run` 一定走 5 參數版，C# 還沒編到時寧可報找不到 method，也不能退回 4 參數（那會真的存檔，就是 2026-09-25 的事故）。scene do 刻意不做 dry-run：它直接改開著的 scene，跑完不 save 也會留下 dirty 內容，比拒絕更危險。
- `RunBeforeSaveCallbacks` 改用 `receiver is UnityEngine.Object o && o == null` 跳過已被 destroy 的 receiver：清單是開跑前 `GetComponentsInChildren` 的快照，前面的 callback（`NetworkAutoSuggestVarSyncComp.ReconcileSyncs` 換 sync 元件）會 `Undo.DestroyObjectImmediate` 掉後面的元件；interface 型別的 `receiver == null` 是 C# 參考比較、不走 UnityEngine.Object 的 `==` overload，destroy 掉的照樣被呼叫、噴 MissingReferenceException 被當成「失敗」。跳過的另外計數成「已被前面的 callback 移除 N 個」，跟真的炸掉的分開。`SceneSaveManager` 的 `OnPrefabSaving` / `OnCustomPrefabSaving` / `OnPrefabStageOpened` 也是同樣的 `!= null` 寫法，這次沒動。
- 2026-10-02 `prefab|` / scene `prefab` op 的「找不到 prefab」分兩種：檔案在磁碟上但 AssetDatabase 沒 import（新匯出的 FBX、Editor 失焦）vs 路徑真的打錯。前者現在會印出 `uloop execute-dynamic-code … ImportAsset(folder)` 的整句指令。故意不在 op 裡自動 ImportAsset：這時候正在 LoadPrefabContents 的編輯批次裡，同步 import 一支新 model 可能連帶 refresh 別的東西，寧願讓 agent 在批次外先 import 再重跑。
