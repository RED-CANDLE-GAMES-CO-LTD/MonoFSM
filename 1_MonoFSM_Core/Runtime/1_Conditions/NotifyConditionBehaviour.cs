using System;
using MonoFSM.Condition;
using MonoFSMCore.Runtime.LifeCycle;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Condition
{
    //FIXME: 還是用polling就好了
    /// <summary>
    /// 當field值改變時，會通知父級的IConditionChangeListener。
    /// </summary>
    [Obsolete]
    public abstract class NotifyConditionBehaviour : AbstractConditionBehaviour, IResetStart, ITransitionCheckInvoker,
        ISceneStart, ISceneDestroy
    {
        public virtual void ResetStart() //應該在這裡註冊嗎？還是sceneStart?
        {
            Register();
        }

        public void EnterSceneStart()
        {
            Register();
        }

        private void OnDestroy()
        {
            UnRegister();
        }

        public void OnSceneDestroy()
        {
            UnRegister();
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
            // listenField.RemoveListener(OnConditionChanged, this);
            // listenField.AddListener(OnConditionChanged, this);
        }

        private void UnRegister()
        {
            if (_isRegistered == false)
                return;
            _isRegistered = false;
            listenField.RemoveListener(OnConditionChanged, this);
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