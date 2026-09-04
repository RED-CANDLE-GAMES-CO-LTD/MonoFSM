using System.Runtime.CompilerServices;

// PromptEdit（MonoFSMPro.Editor）要跟 uprefab 的 prefab read / do 共用同一套節點路徑解析
// （EditResolve 的逃逸規則 + 同層自動命名容錯）。原本它自己複製了一份 SplitPath，
// 結果同一個節點 `prefab read` 指得到、`prompt --var` 指不到 —— 兩份實作漂移的代價。
// 只開這一個 assembly，不把 EditResolve 改成 public：那套 API 是 CLI 內部慣例，
// 不是給 runtime 用的。
[assembly: InternalsVisibleTo("MonoFSMPro.Editor")]
