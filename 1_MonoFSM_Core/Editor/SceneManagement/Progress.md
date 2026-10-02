# Progress

- `OnPrefabSaving` / `OnCustomPrefabSaving` / `OnPrefabStageOpened` 的 receiver null 判斷改成先轉 `Object` 再比（`savingObj is Object o && o == null`）：原因同 `PrefabEditing/Progress.md` 的 `RunBeforeSaveCallbacks` 那條（快照清單裡的元件可能被前面的 callback destroy，interface 的 `!= null` 不走 Unity 的 overload）。`OnPrefabSaving` 刻意不加 try/catch，維持原本的錯誤行為；`OnSceneSaving` 沒動。
