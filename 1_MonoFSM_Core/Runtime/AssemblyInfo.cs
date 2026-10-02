using System.Runtime.CompilerServices;

// GameData 的 editor 讀寫 helper（GameData.ConfigOverview.cs 的 EditorTryGetOwnConfig / EditorSetOwnConfig /
// EditorRemoveOwnConfig …）是 internal，跨 asset 的「GameData 總表」視窗（MonoFSM.Core.Editor）要共用同一套
// 疊層查詢 + Undo 寫入，不自己複製一份。只開給 Editor 這一個 assembly，不把 helper 改 public：
// 那些是 editor 慣例 API，不該讓 runtime 的其他模組拿去用。
[assembly: InternalsVisibleTo("MonoFSM.Core.Editor")]
