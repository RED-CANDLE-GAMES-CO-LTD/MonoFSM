using MonoFSM.Core;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Runtime.Interact.EffectHit
{
    // EffectEnterNode / EffectEnterBestMatchNode 的共用基類：
    // 提供 local 的 _hittingEntity（命中的 entity），由 Resolver 在 enter 時寫入
    public abstract class AbstractEffectEnterNode : AbstractEventHandler
    {
        [AutoParent] private EffectResolver _parentResolver;

        //local variable, 這在這個 enter 下的生命週期
        [Component] public VarEntity _hittingEntity; //to set

        //子類可覆寫 local var 命名後綴（預設 hitEntity）
        protected virtual string HitEntityLabel => "hitEntity";

        protected override void Rename()
        {
            base.Rename();
#if UNITY_EDITOR
            if (_hittingEntity != null &&
                _hittingEntity.transform.parent == transform) //這樣才是 local variable
            {
                _hittingEntity.gameObject.name =
                    "[local] " + _parentResolver._effectType.name + " " + HitEntityLabel;
                _hittingEntity._isRuntimeOnly = true;
                EditorUtility.SetDirty(_hittingEntity.gameObject);
            }
#endif
        }
    }
}
