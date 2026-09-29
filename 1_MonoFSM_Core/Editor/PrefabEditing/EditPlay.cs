using System.Globalization;
using MonoFSM.Core.Simulate;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    ///     `up play play` 等 Play Mode 穩定用的狀態查詢（uprefab CLI 端輪詢，Unity 這邊只回一行快照、不阻塞 main thread）。
    ///     回傳 `playing=1 changing=0 compiling=0 updating=0 sims=1 ready=1 tick=123 time=4.567`：
    ///     tick / time 取 WorldUpdateSimulator.CurrentTick / SimulationTime，CLI 比對前後兩次有沒有前進，才算 tick 真的在跑。
    /// </summary>
    public static class EditPlay
    {
        public static string PlayStatus()
        {
            var sims = 0;
            var ready = 0;
            if (EditorApplication.isPlaying)
                foreach (var sim in Object.FindObjectsByType<WorldUpdateSimulator>(FindObjectsInactive.Exclude,
                             FindObjectsSortMode.None))
                {
                    sims++;
                    if (sim.IsReady) ready++;
                }

            return "playing=" + (EditorApplication.isPlaying ? 1 : 0) +
                   " changing=" + (EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying ? 1 : 0) +
                   " compiling=" + (EditorApplication.isCompiling ? 1 : 0) +
                   " updating=" + (EditorApplication.isUpdating ? 1 : 0) +
                   " sims=" + sims + " ready=" + ready +
                   " tick=" + WorldUpdateSimulator.CurrentTick +
                   " time=" + WorldUpdateSimulator.SimulationTime.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
