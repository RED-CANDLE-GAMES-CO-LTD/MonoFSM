using MonoFSM.Core;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Runtime.Interact.EffectHit
{
    // 用這個觸發action?
    public sealed class EffectEnterNode : AbstractEventHandler
    {
        [AutoParent] private EffectResolver _parentResolver;
        //local variable, 這在這個enter下的生命週期
        // [Required]
        [Component] //[Component?
        public VarEntity _hittingEntity; //to set
        //這個只有 dealer才需要吧？ receiver自己就是了？

        protected override void Rename()
        {
            base.Rename();
#if UNITY_EDITOR
            if (_hittingEntity != null &&
                _hittingEntity.transform.parent == transform) //這樣才是local variable
            {
                _hittingEntity.gameObject.name =
                    "[local] " + _parentResolver._effectType.name + " hitEntity";
                _hittingEntity._isRuntimeOnly = true;
                EditorUtility.SetDirty(_hittingEntity.gameObject);
            }
#endif
        }
    }
}
