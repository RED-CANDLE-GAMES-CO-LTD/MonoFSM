using System;
using System.Collections.Generic;
using System.Linq;
using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core
{
    /// <summary>
    /// auto bind a TParent as parent to find components implementing 
    /// </summary>
    /// <typeparam name="TParent"></typeparam>
    /// <typeparam name="TInterface"></typeparam>
    [Serializable]
    public abstract class InterfaceMonoRef<TParent, TInterface>
        where TParent : MonoBehaviour
    {
        [SerializeField]
        [DropDownRef]
        [ValueDropdown(nameof(GetComps), NumberOfItemsBeforeEnablingSearch = 3)]
        [HideLabel]
        protected MonoBehaviour ValueSource;

        public string Name => ValueSource.name.Replace("[Variable]", "");

        IEnumerable<MonoBehaviour> GetComps()
        {
            if (owner == null)
                return null;
            var comps = owner.GetComponentsInChildren<TInterface>(true);
            return comps.Select(c => c as MonoBehaviour);
            // return comps.Select(c => (MonoBehaviour)c);
        }

        //避免serialization, 讓drawer看到的時候暫時拿到
        [HideIf("@true")] [ShowInInspector] [AutoParent]
        TParent owner;
        // public T Source => ValueSource;
    }
}