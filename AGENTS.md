<!-- GENERATED FILE: run `python3 Tools/sync-agent-instructions.py` after editing the source. -->
<!-- Source: MonoFSM/CLAUDE.md -->

* 當我提出需求時，先回應我清楚度 1-10分，當問題不清楚時(<7)，請要求我提供更多資訊
* 當 cs 檔案過長時，應進行 refactor 或是需要拆模組到其他檔案
* 此專案使用 Odin Inspector, 編輯器工具盡量使用已有的 Attribute (已搭配AttributeDrawer)
    * ex: 1_MonoFSM_Core/Runtime/Attributes/CompRefAttribute.cs
* SerializedField 和 public field 以 _開頭命名
* Component reference 在對應的 member 欄位上用 [Auto], [AutoParent], [AutoChildren] 來標記即可, 不需要在awake時獲取
* 當需求涉及 MonoFSM 相關操作（State、Action、Condition、Transition、EffectDealer、Timer、VarFloat、Prefab FSM 編輯等），**先調用
  MonoFSM skill**
* 實作細節與架構：盡量只完成必要功能即可，不要過度設計
    * Action,功能實作等，透過繼承AbstractStateAction來實現 (
      @MonoFSM/1_MonoFSM_Core/Runtime/Action/AbstractStateAction.cs)
    * Condition,條件實作等，透過繼承 AbstractConditionBehaviour 來實現 (
      @MonoFSM/1_MonoFSM_Core/Runtime/1_Conditions/AbstractConditionBehaviour.c)
    * 新寫或改動 Action / Condition 時，class 上**一定要有 `/// <summary>`**：第一句就要能獨立看懂用途
      （`up catalog` 的清單只顯示第一句），寫「什麼時候用它、掛在哪」而不是實作細節；欄位語意不明顯時加
      `[Tooltip]`。挑既有 component 用 `up catalog`，讀到沒說明的就順手補
* 用 Debug.Log 來讓我協助測試與除錯
    * Debug.Log 第二個參數記得加上 this，或是需要顯示標記目標對象的Object，方便我點擊訊息後定位到程式碼位置或對象
    * Debug 用的欄位可以加上 Odin 的 ShowInInspector，方便我在 Inspector 中觀察數值變化
* 盡量不要用 awake 和 start, 用 ISceneAwake, ISceneStart 來取代, IResetStateRestore 和 IResetStart 可以在遊戲重置狀態時呼叫
* 執行完任務時，在該模組資料夾建立Progress.md, 有的話就沿用，每次用一句話或盡量簡短描述這次的改動或修正
* 當我要求更動、修改 prefab，或是要看 scene 上的內容，或是自動測試驗證時，使用 uprefab。測試時我可能隨時會介入操作或是更改
  inspector 上的值
* 當編輯過程中不順、或是需要耗費大量 token 來做 execute-dynamic-code 時，應優先補齊 uprefab 的 cli 工具
* 每次查找都要想看看是不是效率不好，發現效率不好就要改善流程或補齊工具，並跟我反應

## Project skills

When a task matches one of these skills, read its `SKILL.md` before acting and follow it.
The skill files below are project-local instructions, not optional reference material.

- `MonoFSM/skills/Gizmos/SKILL.md` — MonoGizmoUtility 除錯繪製工具的使用指南。當需要在 Scene/Game View 繪製除錯用的線、球、方塊、文字等 Gizmo 時使用此 skill。
- `MonoFSM/skills/MonoFSM/SKILL.md` — MonoFSM 有限狀態機框架的使用指南。當需要：(1) 了解 MonoFSM 架構與設計理念 (2) 在 Unity Scene 中新增/修改 State、Transition、Condition、Action (3) 撰寫新的 Action、Condition C# 腳本 (4) 使用 Auto 系列 Attribute 自動引用組件 (5) 理解狀態優先級系統 (6) 設定 VarFloat 計時器 (7) 使用 EffectDealer/EffectReceiver 互動系統 (8) 解析、匯出、或讀懂既有 FSM prefab／scene 物件的結構（用 FsmTextExporter 匯出 markdown 文字）時使用此 skill。
- `MonoFSM/skills/MonoObjLifecycle/SKILL.md` — MonoObj 更新生命週期系統的使用指南。當需要：(1) 了解 WorldUpdateSimulator 的更新迴圈架構 (2) 實作 Simulate、Render 等每幀更新邏輯 (3) 新增 IUpdateSimulate、IBeforeSimulate、IAfterSimulate、IRenderSimulate 實作 (4) 理解 MonoObj 註冊/反註冊流程 (5) 理解 FixedUpdate/LateUpdate 的執行時機時使用此 skill。
- `MonoFSM/skills/hierarchy-text-exporter/SKILL.md` — Hierarchy → 精簡結構化文字匯出工具（HierarchyTextExporter）。當需要：(1) 讓 LLM 用最少 token 讀懂 Unity GameObject 階層與 Component serialized 欄位 (2) 匯出 prefab 或 scene 子樹成精簡文字（uloop execute-dynamic-code 呼叫或右鍵複製）(3) 理解/修改 HierarchyTextExporter 相關程式碼、摺疊摘要規則、值格式化規則時使用此 skill。
- `MonoFSM/skills/prefab-text-exporter/SKILL.md` — Unity Prefab 文字匯出工具的設計指南。當需要：(1) 理解或修改 PrefabToTextExporter 相關程式碼 (2) 擴充匯出格式或新增支援的類型 (3) 新增 Component 類型對應 (4) 調整篩選邏輯或輸出格式時使用此 skill。
- `MonoFSM/skills/uprefab/SKILL.md` — 能讀懂並改動 Unity serialized data（prefab / scene / ScriptableObject）。當需要：(1) 找某個 component / 節點在哪些 prefab 或 scene 裡 (2) 讀某個 prefab 的階層結構或 FSM 狀態機架構 (3) 看某個子樹的 component 欄位細節 (4) prefab override 稽核 (5) 用 API 改 prefab / scene 結構、建 prefab variant、複製場景模板、組 FSM、建立或編輯 ScriptableObject asset（registry / config 類資料） (6) 查某個型別有哪些 serialized 欄位、讀 Play Mode 下的 runtime 值、數場上物件驗證生成邏輯 (7) 查某個節點被誰引用 / 它指向誰 (8) 使用者貼了 asset guid 或 Editor webhook 連結（`?asset_guid=…`）需要換成資產路徑 (9) 理解或修改 uprefab 離線索引（MonoFSM/Tools~/uprefab/*.py）、PrefabTextReader 或 PrefabEdit / SceneEdit 時使用此 skill。
