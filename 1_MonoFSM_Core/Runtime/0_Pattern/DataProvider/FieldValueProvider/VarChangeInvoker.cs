using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core.DataProvider
{
    // public interface IFieldValueProvider
    // {
    //     public Object targetObject { get; }
    // }

    public class VarChangeInvoker : MonoBehaviour, IResetStart
    {
        [Required] [CompRef] [Auto] private AbstractVariableProviderRef _variableProviderRef; //當這個var值變化時
        [Required] [CompRef] [Auto] private AbstractFieldOfVarProvider _fieldOfVarProvider; //用這個值
        [Required] [CompRef] [Auto] private IDataChangedListener _dataChangedListener; //給這個對象


        public void ResetStart() //FIXME: 應該在這註冊？還是scene註冊一次就好？
        {
            var listenToVar = _variableProviderRef.VarRaw;
            //這個variable已經準備好了嗎？
            if (listenToVar)
            {
                listenToVar.OnValueChangedRaw += OnValueChanged;
                Debug.Log("Bind Variable", this);
                _dataChangedListener.OnDataChanged(_fieldOfVarProvider.targetObject);
            }
            else
            {
                Debug.LogError("ListenToVariable is null", this);
                if (isActiveAndEnabled)
                    Debug.Break();
                
            }
        }

        private void OnValueChanged()
        {
            _dataChangedListener.OnDataChanged(_fieldOfVarProvider.targetObject);
        }

        // private AbstractMonoVariable ListenToVariable => _variableProviderRef.VarRaw;
    }
}