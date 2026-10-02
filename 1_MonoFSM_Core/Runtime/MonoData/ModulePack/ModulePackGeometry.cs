using MonoFSM.Runtime;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.LifeCycle.Update
{
    /// <summary>
    /// 標記：MonoModulePack 底下「要長在宿主身上」的那包幾何（判定 trigger、detector、slot view、spawn 點）。
    /// pack 的 <c>_geometryRoot</c> 指這顆；宿主 <c>MonoEntity.EnterSceneAwake</c> 會把它 reparent 到宿主掛點
    /// （<c>MonoEntity.ModuleGeometryAnchor</c>，預設 ViewRoot.Root = 通常是 Context/Animator），所以跟著宿主的剛體 / 動畫走。
    /// edit-time 它還在 pack 底下（<c>up prefab read</c> 看到的是搬之前的位置），local pose 以掛點為準，宿主存檔時由 pack 記下。
    /// 是 <see cref="IEntityScopeBoundary" />：搬進宿主之後，宿主自己的 folder 掃描不會把裡面的東西當成宿主的。
    /// 底下的 BaseEffectDetectTarget 由 pack 存檔時把 _detectableOverride 指回 pack 自己的 EffectDetectable。
    /// </summary>
    [DisallowMultipleComponent]
    public class ModulePackGeometry : MonoBehaviour, IEntityScopeBoundary
    {
    }
}
