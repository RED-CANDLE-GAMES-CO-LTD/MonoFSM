using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    /// <summary>
    ///     把另一顆 <see cref="VarEntity" /> 轉成 value source，讓「這顆 VarEntity 的值 = 那顆的值」。
    ///     ⚠ <c>_varEntity</c> 沒指東西時 Description 顯示 <c>None</c>、值就是 null，
    ///     而且因為它是 value source，會讓所屬 VarEntity 的 _defaultValue fallback 整個失效
    ///     （<see cref="VarEntity" /> 的說明有解法）—— 看到節點叫 <c>=> None</c> 就是這種空殼，
    ///     那條鏈在 runtime 一定解不到值，不要把它當成「會自己填」的東西照抄。
    /// </summary>
    //FIXME: 好像可以留著喔？重寫？
    //FIXME: 哪裡需要用到？
    //provider是從variable拿到的MonoEntity，這個MonoEntity是
    // [Obsolete]
    public class VarEntityRef : AbstractValueSource<MonoEntity>
    {
        public override string Description => $"{_varEntity?.name ?? "None"}";

        // [ValueTypeValidate(typeof(MonoEntity))] [SerializeField]
        // private ValueProvider _varEntityProvider;
        //FIXME: 必定從自身拿到？
        [DropDownRef]
        [SerializeField]
        private VarEntity _varEntity;
        public MonoEntity monoEntity => _varEntity?.Value;

        [ShowInInspector]
        public MonoEntityTag entityTag => monoEntity?.DefaultTag ?? _varEntity?.EntityTag; //FIXME: monoEntity是null, 這樣 _varEntity要先知道有什麼tag?

        public override MonoEntity Value => monoEntity;
    }
}
