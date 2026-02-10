* 如果我沒有選擇程式碼片段(select line)或沒有提供目前編輯的檔案時，先提醒我
* 當我提出需求時，先回應我清楚度 1-10分，當問題不清楚時(<7)，請要求我提供更多資訊
* 當修改的檔案超出500行時，可以進行refactor或是需要拆模組到其他檔案
* 改完一個檔案後，要先用getDiagnostics檢查有沒有錯誤，記得檔案要用絕對路徑，遇到 "File not found" 要試不同的路徑格式看看，不可略過
  ex: ide - getDiagnostics (MCP)(uri: "/Users/jerryee/Documents/...", 開頭不要有 "file://")
* 此專案使用Odin Inspector, 編輯器工具盡量使用已有的Attribute (已搭配AttributeDrawer)
    * ex: 1_MonoFSM_Core/Runtime/Attributes/CompRefAttribute.cs
  * Editor工具如果不是非常複雜的情況也是用 Odin Inspector做就好，如果需要EdtiorWindow時也要用OdinEditorWindow
* SerializedField 和 public field 以底線開頭命名
* Component cache 在對應的 member 欄位上用 [Auto], [AutoParent], [AutoChildren] 來標記即可, 不需要在awake時獲取

* 實作細節與架構：盡量只完成必要功能即可，不要過度設計
    * 當我提到關於 Action,功能實作等，透過繼承AbstractStateAction來實現 (
      @MonoFSM/1_MonoFSM_Core/Runtime/Action/AbstractStateAction.cs)
    * 當我提到關於 Condition,條件實作等，透過繼承 AbstractConditionBehaviour 來實現 (
      @MonoFSM/1_MonoFSM_Core/Runtime/1_Conditions/AbstractConditionBehaviour.c)
* 可以用 Debug.Log 來讓我協助測試與除錯
* 盡量不要用 awake 和 start, 用 ISceneAwake, ISceneStart 來取代