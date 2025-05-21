using MonoFSM.Condition;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Condition
{
    public abstract class NotifyConditionComp : AbstractConditionComp, IResetStart, ITransitionCheckInvoker,ISceneStart
    {
        public virtual void ResetStart() //應該在這裡註冊嗎？還是sceneStart?
        {
            Register();
        }

        public void EnterSceneStart()
        {
            Register();
        }

        //要能實作OnConditionChanged?
        [PreviewInInspector]
        [AutoParent] protected IConditionChangeListener _parentConditionChangeListener;

        [ShowInPlayMode] [InfoBox("not Register to listenField", InfoMessageType.Error, "@!_isRegistered")]
        private bool _isRegistered = false;
        private void Register()
        {
            _isRegistered = true;
            // Debug.Log("Register: " + listenField, this);
            // Debug.Break();
            listenField.RemoveListener(OnConditionChanged, this);
            listenField.AddListener(OnConditionChanged, this);
        }


        protected abstract IVariableField listenField { get; }

        private void OnConditionChanged()
        {
            if (_parentConditionChangeListener == null)
            {
                Debug.LogError("VarBoolValueCondition: No _parentConditionChangeListener found", this);
                return;
            }

            // Debug.Log("OnConditionChanged: " + listenField, this);
            _parentConditionChangeListener.OnConditionChanged();
        }

    
    }
}