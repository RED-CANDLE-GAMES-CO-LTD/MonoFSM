# HierarchyFavorites Progress

- 搜尋打字效能：收集結果快取（只在 `Build()` 失效，打字不再重掃 hierarchy）、輸入 debounce 120ms、一次最多建 300 個 entry button（超出顯示提示）。
- `VariableEntry` 加上 `Note`（收集時取 `AbstractDescriptionBehaviour.Note`），Variables/Effects/States/Descriptions 四個 tab 的搜尋都可以命中 note，按鈕文字後面附上 note 摘要與 tooltip。
- root 判定抽到 `HierarchyFavoritesRootResolver`：parent 找不到 MonoObj 時往 children 找最上層的那幾個（支援多 root），Favorites/Variables/Effects/States/Descriptions 共用；Favorites tab 也加上 search bar（UIToolkit 與 IMGUI 兩版）。
