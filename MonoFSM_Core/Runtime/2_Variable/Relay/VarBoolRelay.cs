using MonoFSM.Foundation;

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
    public class VarBoolRelay : AbstractDescriptionBehaviour, IResetStart
    {
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

        /// <summary>
        /// Initializes the relay by setting up a listener on the source variable.
        /// Called when the component is being ResetStart by LevelRunner.
        /// </summary>
        public void ResetStart()
        {
            _source.Field.AddListener(value => { _target.Field.SetCurrentValue(value, this); }, this);
        }

        protected override string Description 
            => "when '$" + _source?._varTag?.name + "' changed, set '$" + _target?._varTag?.name + "'";

        protected override string DescriptionTag 
            => "Relay";
    }
}