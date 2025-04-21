using MonoFSM.Condition;

namespace MonoFSM.Condition
{
    public abstract class NotifyConditionComp : AbstractConditionComp, IResetStart, ITransitionCheckInvoker
    {
        public virtual void ResetStart()
        {
            Register();
        }

        //要能實作OnConditionChanged?
        [AutoParent] protected IConditionChangeListener _parentConditionChangeListener;

        private void Register()
        {
            listenField.AddListener(OnConditionChanged, this);
        }


        protected abstract IVariableField listenField { get; }

        private void OnConditionChanged()
        {
            if (_parentConditionChangeListener == null)
                // Debug.LogError("VarBoolValueCondition: No parent transition found", this);
                return;
            _parentConditionChangeListener.OnConditionChanged();
        }
    }
}