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