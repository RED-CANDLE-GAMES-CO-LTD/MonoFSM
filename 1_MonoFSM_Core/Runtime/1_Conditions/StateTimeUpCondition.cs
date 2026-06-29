using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core
{
    //時間到就是true
    public class StateTimeUpCondition : AbstractConditionBehaviour
    {
        public override string Description =>
            $"{targetState.name} State Time Up: >= {_time}";

        [PreviewInInspector]
        [AutoParent] private GeneralState _parentState;

        [SerializeField] GeneralState _externalState; //如果要參考別的 state 的時間
        private GeneralState targetState => _externalState != null ? _externalState : _parentState;

        [FormerlySerializedAs("time")] public float _time;
        [ShowInInspector] private float targetStateStatusTimer => targetState?.statusTimer ?? -1f;
        protected override bool IsValid => targetState.statusTimer >= _time;
    }
}
