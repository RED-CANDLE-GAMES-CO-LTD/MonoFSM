# Progress

- 2026-09-29 `[Required]` 驗證（`AbstractDescriptionBehaviour.CheckNullOfRequiredFields`）對陣列 / List 永遠誤報 `Required field ... is null`：它用 `field.GetValue(this) as Object` 判斷，陣列不是 UnityEngine.Object → 一律 null，資料明明掛好了還是報錯（發電鴿滑索 `CableCartQueueView._cartProgress` 踩到）。改成建欄位快取時直接把 IList 型別的 [Required] 欄位濾掉，每個型別印一次 warning 說「陣列不需要 Required，元素檢查請寫在 component 自己的 fail reason enum」—— 讓下一個寫錯的人從 Console 學到，而不是去查文件。故意不做「驗元素不能是 null」：Unity serialize 的陣列載入後至少是空陣列，本來就不會是 null，空陣列合不合法由各 component 自己決定。其他非 Object 型別（string / struct）掛 [Required] 一樣會誤報，這次沒動。
