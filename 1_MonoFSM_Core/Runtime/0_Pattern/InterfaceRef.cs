using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Variable;
using MonoFSM.Variable.VariableBinder;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// auto bind a TParent as parent to find components implementing 
    /// </summary>
    /// <typeparam name="TParent"></typeparam>
    /// <typeparam name="TInterface"></typeparam>
    [Serializable]
    public abstract class InterfaceMonoRef<TParent, TInterface> : IName //從parent下面找到所有的TInterface
        where TParent : MonoBehaviour
    {
        [SerializeField]
        [DropDownRef] //fixme 好像不行耶..還是要在這裡做
        // [ValueDropdown(nameof(GetComps), NumberOfItemsBeforeEnablingSearch = 3)]
        [HideLabel]
        protected MonoBehaviour ValueSource;


        // private ScriptableObject ValueSourceSO;
        
        public string Name => ValueSource.name.Replace("[Variable]", "").TrimStart(' ');

        IEnumerable<MonoBehaviour> GetComps()
        {
            if (_owner == null)
                return null;
            var comps = _owner.GetComponentsInChildren<TInterface>(true);
            return comps.Select(c => c as MonoBehaviour);
            // return comps.Select(c => (MonoBehaviour)c);
        }

        //避免serialization, 讓drawer看到的時候暫時拿到
        [HideIf("@true")] [ShowInInspector] [AutoParent]
        private TParent _owner;
        // public T Source => ValueSource;
    }
    
    
}