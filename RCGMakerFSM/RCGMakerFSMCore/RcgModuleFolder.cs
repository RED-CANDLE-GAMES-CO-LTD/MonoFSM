using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime.FSM.RCGStateMachine
{
    public interface IModuleOwner
    {
        // public RCGModuleFolder ModuleFolder { get; }
    }
    public interface IRcgModule //dynamic reference?
    {
        
    }
    public class RcgModuleFolder:MonoBehaviour, IModuleOwner
    {
        [PreviewInInspector]
        [AutoChildren] IRcgModule[] _modules;
        public T GetModule<T>() where T: class, IRcgModule
        {
            foreach (var module in _modules)
            {
                if (module is T rcgModule)
                {
                    return rcgModule;
                }
            }
            return null;
        }
        //FIXME: 需要這個嗎？
        // public RCGModuleFolder ModuleFolder { get; }
    }
}