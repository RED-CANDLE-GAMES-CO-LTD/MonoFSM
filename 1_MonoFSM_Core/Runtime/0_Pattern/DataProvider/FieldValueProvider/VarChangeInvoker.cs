using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    //FIXME: 什麼時候提示要裝這個才會更新？？
    //如果Polling就不需要這個了
    public class VarChangeInvoker : MonoBehaviour, IResetStart, IDataChangedProvider, IVarChangedListener
    {
        [Required] [CompRef] [Auto] private AbstractVariableProviderRef _variableProviderRef; //當這個var值變化時, 需要監聽多個？

        // [Required] [CompRef] [Auto] private AbstractFieldOfVarProvider _fieldOfVarProvider; //用這個值
        [Required] [CompRef] [Auto] private IDataChangedListener _dataChangedListener;

//Proxy updater要怎麼辦？沒有備注冊進去？
        public void ResetStart() //FIXME: 應該在這註冊？還是scene註冊一次就好？
        {
            //FIXME: Global還沒拿到嗎？應該不會吧
            var listenToVar = _variableProviderRef.VarRaw;
            //這個variable已經準備好了嗎？
            if (listenToVar)
            {
                //FIXME: update polling通知？通知就是一種事件... polling?
                // listenToVar.OnValueChangedRaw += OnValueChanged;
                listenToVar.AddListener(this);
                OnVarChanged(listenToVar);
            }
            else
            {
                Debug.LogError("ListenToVariable is null", this);
                if (isActiveAndEnabled)
                    Debug.Break();
                
            }
        }

        // private AbstractMonoVariable ListenToVariable => _variableProviderRef.VarRaw;
        public void OnVarChanged(AbstractMonoVariable variable)
        {
            _dataChangedListener.OnDataChanged(_variableProviderRef.VarRaw);
        }
    }
}