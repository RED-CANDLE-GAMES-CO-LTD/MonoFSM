using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSMCore.Runtime.LifeCycle;

namespace MonoFSM.Variable
{
    /// <summary>
    /// Relays values from a source variable to a target variable.
    /// The parent GameObject needs to have a VariableOwner component.
    /// </summary>
    /// <remarks>
    /// This component listens for changes in the source variable and 
    /// automatically propagates those changes to the target variable.
    /// </remarks>
    public class VarBoolRelay : AbstractDescriptionBehaviour, IResetStart, IUpdateSimulate
    {
        //可以分監聽型和Polling型?
        private bool _isPolling = false;
        //FIXME: source不一定是var?
        /// <summary>
        /// The source variable that will be monitored for changes.
        /// Changes from this variable will be relayed to the target.
        /// </summary>
        [DropDownRef] public VarBool _source;

        /// <summary>
        /// The target variable that will receive values from the source.
        /// This variable's value will be updated whenever the source changes.
        /// </summary>
        [DropDownRef] public VarBool _target;

        private bool _lastValue;
        
        /// <summary>
        /// Initializes the relay by setting up a listener on the source variable.
        /// Called when the component is being ResetStart by LevelRunner.
        /// </summary>
        public void ResetStart()
        {
            _lastValue = _source.Field.CurrentValue;
            if (!_isPolling)
                _source.Field.AddListener(value => { _target.Field.SetCurrentValue(value, this); }, this);
        }
        
        public override string Description 
            => "when '$" + _source?._varTag?.name + "' changed, set '$" + _target?._varTag?.name + "'";

        protected override string DescriptionTag 
            => "Relay";

        public void Simulate(float deltaTime)
        {
            var currentValue = _source.Field.Value;
            if (currentValue != _lastValue)
            {
                _lastValue = currentValue;
                _target.Field.SetCurrentValue(currentValue, this);
            }
        }

        public void AfterUpdate()
        {
        }
    }
}