using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Core.DataProvider.ComponentWrapper
{
    public class GetPoolObjFromMonoList : GetCompFromMonoList<MonoPoolObj>, IMonoObjectProvider
    {
        //這個類別是用來從MonoList中獲取MonoPoolObj的
        //可以直接使用Get()方法來獲取當前的MonoPoolObj
    }

    public abstract class GetCompFromMonoList<T> : MonoBehaviour, ICompProvider<T> where T : Component
    {
        //還要先list provider嗎？
        [DropDownRef] public AbstractVarList _varList;
        public string Description { get; }

        [ShowInPlayMode]
        public T Get()
        {
            return _varList.CurrentRawObject as T;
        }

        Component ICompProvider.Get()
        {
            return Get();
        }
    }
}