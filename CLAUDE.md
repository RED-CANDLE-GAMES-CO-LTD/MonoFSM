* 如果我沒有選擇程式碼片段(select line)或沒有提供目前編輯的檔案時，先提醒我
* 當我提出需求時，先回應我清楚度 1-10分，當問題不清楚時(<7)，請要求我提供更多資訊
* 當你創建的檔案超出 500 行時，應進行 refactor 或是需要拆模組到其他檔案
* 新增/修改 C# script 後的必要流程（不可跳過）：
  1. `refresh_unity` (compile: request, wait_for_ready: true)
  2. `read_console` (types: error) 確認無編譯錯誤
  3. 有錯就修，修完重複 1~2，沒錯才繼續
  4. 若檔案在 Rider 中有開啟，用 `getDiagnostics` 做進一步檢查
* 此專案使用 Odin Inspector, 編輯器工具盡量使用已有的Attribute (已搭配AttributeDrawer)
    * ex: 1_MonoFSM_Core/Runtime/Attributes/CompRefAttribute.cs
  * Editor工具如果不是非常複雜的情況也是用 Odin Inspector 做就好，如果需要 EdtiorWindow 時也要用OdinEditorWindow
* SerializedField 和 public field 以底線開頭命名
* Component cache 在對應的 member 欄位上用 [Auto], [AutoParent], [AutoChildren] 來標記即可, 不需要在awake時獲取

* 當需求涉及 MonoFSM 相關操作（State、Action、Condition、Transition、EffectDealer、Timer、VarFloat、Prefab FSM 編輯等），**必須先調用 MonoFSM skill**，不可跳過直接搜尋
* 實作細節與架構：盡量只完成必要功能即可，不要過度設計
    * 當我提到關於 Action,功能實作等，透過繼承AbstractStateAction來實現 (
      @MonoFSM/1_MonoFSM_Core/Runtime/Action/AbstractStateAction.cs)
    * 當我提到關於 Condition,條件實作等，透過繼承 AbstractConditionBehaviour 來實現 (
      @MonoFSM/1_MonoFSM_Core/Runtime/1_Conditions/AbstractConditionBehaviour.c)
* 可以用 Debug.Log 來讓我協助測試與除錯，適當保留檔案中的 Log 不要刪除，等到我說 ok 再刪除
* 盡量不要用 awake 和 start, 用 ISceneAwake, ISceneStart 來取代
* 我提到記下來或是記到skill時，預設使用 skill-creator 來把對應的知識記到 MonoFSM 的 Skill 裡