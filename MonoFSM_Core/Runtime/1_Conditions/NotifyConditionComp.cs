using MonoFSM.Condition;
using RCGMaker.Core.Attributes;

namespace MonoFSM.Condition
{
    public abstract class NotifyConditionComp : AbstractConditionComp, IResetStart, ITransitionCheckInvoker,ISceneStart
    {
        public virtual void ResetStart() //應該在這裡註冊嗎？還是sceneStart?
        {
            Register();
        }

        //要能實作OnConditionChanged?
        [PreviewInInspector]
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

        public void EnterSceneStart()
        {
            Register();
        }
    }
}