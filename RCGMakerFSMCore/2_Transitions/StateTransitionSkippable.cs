using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

//裝在Projectile 上標記他不能被Skip
    public class StateTransitionSkippable:MonoBehaviour,ISkippableAnimationTransition
    {
        public bool canSkip = true;
        public bool CanSkip()
        {
            return canSkip;
        }
    }
    
    public interface ISkippableAnimationTransition
    {
        bool CanSkip();
    }

