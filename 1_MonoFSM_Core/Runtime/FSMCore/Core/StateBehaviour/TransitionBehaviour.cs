using Fusion.Addons.FSM;
using MonoFSM.Core;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using RCGExtension;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    public class TransitionBehaviour : TransitionBehaviour<MonoStateBehaviour>, IOverrideHierarchyIcon,
        IDrawHierarchyBackGround
    {
        protected override string DescriptionTag => "Transition";

        public override string Description
            => _target != null && _target.Name != null ? "=>" + _target.Name.Replace("[State]", "") : "";

        protected override void Awake()
        {
            _transitionData = new TransitionData<MonoStateBehaviour>(_target, (state, machine) =>
            {
                if (isActiveAndEnabled == false)
                    return false;

                // Check all conditions
                return _conditions.IsAllValid();
            });
        }

        [DropDownRef] public MonoStateBehaviour _target;

        [RequiredListLength(1, null)]
        [SerializeField] [CompRef] [AutoChildren(DepthOneOnly = true)]
        private AbstractConditionBehaviour[] _conditions;


#if UNITY_EDITOR
        // public Color BackgroundColor => new(1.0f, 0f, 0f, 0.3f);
        public string IconName => "CollabMoved Icon";
        public bool IsDrawingIcon => true;

        public Texture2D CustomIcon => null;
        // UnityEditor.EditorGUIUtility.ObjectContent(null, typeof(StateTransition)).image as Texture2D;

#endif
        // public bool IsDrawGUIHierarchyBackground => HasError(); //還是用icon? 


        // private bool HasError()
        // {
        //     if (_target == null)
        //     {
        //         _errorMessage = "No Target State";
        //         return true;
        //     }
        //
        //     //FIXME: cache判定？貴一點要GetComponent...什麼時候refresh? auto找不到的有點麻煩...non serialized...
        //     // if (NoChecker)
        //     // {
        //     //     _errorMessage = "No Checker Invoker in Parent or Children";
        //     //     return true;
        //     // }
        //
        //     _errorMessage = "Pass!";
        //     return false;
        // }
    }


    //這層才算是換掉的實作？上面是介面 serialized field就是一種介面的參數，如果放在最外層，名字一樣就可以直接抽換了
    public abstract class TransitionBehaviour<TState> : AbstractDescriptionBehaviour
        where TState : AbstractStateBehaviour<TState>
    {
        public TransitionData<TState> _transitionData;

        public TState TargetState => _transitionData.TargetState;
        // public TransitionData<GeneralStateBehaviour> TransitionData => _transitionData;

        // set => _transitionData = value;
    }
}