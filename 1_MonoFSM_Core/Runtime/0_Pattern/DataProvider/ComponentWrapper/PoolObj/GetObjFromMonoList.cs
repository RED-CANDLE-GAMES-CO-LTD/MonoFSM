using MonoFSM.Core;
using MonoFSM.Core.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Core.DataProvider.ComponentWrapper
{
    public class GetObjFromMonoList : GetObjFromMonoList<MonoPoolObj>, IMonoObjectProvider
    {
        //這個類別是用來從MonoList中獲取MonoPoolObj的
        //可以直接使用Get()方法來獲取當前的MonoPoolObj
    }

    public abstract class GetObjFromMonoList<T> : MonoBehaviour, ICompProvider<T> where T : Object
    {
        //還要先list provider嗎？
        [DropDownRef] public AbstractVarList _varList;
        public string Description { get; }

        public T Get()
        {
            return _varList.CurrentRawObject as T;
        }
    }
}