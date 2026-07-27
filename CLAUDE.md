* 當我提出需求時，先回應我清楚度 1-10分，當問題不清楚時(<7)，請要求我提供更多資訊
* 當 cs 檔案過長時，應進行 refactor 或是需要拆模組到其他檔案
* 此專案使用 Odin Inspector, 編輯器工具盡量使用已有的 Attribute (已搭配AttributeDrawer)
    * ex: 1_MonoFSM_Core/Runtime/Attributes/CompRefAttribute.cs
* SerializedField 和 public field 以 _開頭命名
* Component reference 在對應的 member 欄位上用 [Auto], [AutoParent], [AutoChildren] 來標記即可, 不需要在awake時獲取
* 當需求涉及 MonoFSM 相關操作（State、Action、Condition、Transition、EffectDealer、Timer、VarFloat、Prefab FSM 編輯等），**先調用
  MonoFSM skill**
* 當需求涉及讀懂 / 定位 prefab、scene 的序列化內容（不想把整個大 scene 塞進 context），**先調用 uprefab skill** —— 離線索引
  CLI 在 `Tools~/uprefab/`（`~` 後綴讓 Unity 不 import）
* 實作細節與架構：盡量只完成必要功能即可，不要過度設計
    * Action,功能實作等，透過繼承AbstractStateAction來實現 (
      @MonoFSM/1_MonoFSM_Core/Runtime/Action/AbstractStateAction.cs)
    * Condition,條件實作等，透過繼承 AbstractConditionBehaviour 來實現 (
      @MonoFSM/1_MonoFSM_Core/Runtime/1_Conditions/AbstractConditionBehaviour.c)
* 可以用 Debug.Log 來讓我協助測試與除錯
    * Debug.Log 第二個參數記得加上 this，方便我點擊訊息後定位到程式碼位置
    * Debug 用的欄位可以加上 Odin 的 ShowInInspector，方便我在 Inspector 中觀察數值變化
* 盡量不要用 awake 和 start, 用 ISceneAwake, ISceneStart 來取代, IResetStateRestore 和 IResetStart 可以在遊戲重置狀態時呼叫