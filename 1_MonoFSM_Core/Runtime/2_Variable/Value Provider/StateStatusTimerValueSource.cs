using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM.Core.Variable.Providers
{
    //提供目標 State 的 statusTimer (進入該 state 後經過的時間)
    public class StateStatusTimerValueSource : AbstractValueSource<float>, IValueProvider<float>
    {
        [PreviewInInspector]
        [AutoParent] private GeneralState _parentState;

        [SerializeField] private GeneralState _externalState; //如果要參考別的 state 的時間
        private GeneralState TargetState => _externalState != null ? _externalState : _parentState;

        public override string Description => $"{TargetState?.name} StatusTimer";
        public override float Value => TargetState?.statusTimer ?? -1f;
    }
}
