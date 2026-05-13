using System;
using System.Collections.Generic;
using Fusion.Addons.FSM;
using MonoFSM.Core;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    public class StateFolder : MonoDictFolder<string, MonoStateBehaviour>
    {
        public StateFolder bindingRootFolder => _bindingRoot as StateFolder;
        [NonSerialized] [ShowInInspector] List<AnyState> _allAnyStates = new();
        public List<AnyState> AllAnyStates => _allAnyStates;

        [AutoChildren] private AnyState[] _anyStates;
        //parent folder?
        [AutoParent] MonoEntity _owningEntity;

        // [Auto] private StateMachineLogic _context;
        // public StateMachineLogic bindingContext => _owningEntity.StateFolder._context;
        protected override string DescriptionTag => "StateFolder";

        public override void EnterSceneAwake()
        {
            base.EnterSceneAwake();
            foreach (var anyState in _anyStates)
            {
                _allAnyStates.Add(anyState);
            }
        }

        protected override void AddImplement(MonoStateBehaviour item)
        {
            // else
            // {
            //     base.AddImplement(item);
            // }
        }

        protected override void AddFailImplement(MonoStateBehaviour item)
        {
            //anystate不放進去？好醜的設計XDDD
            // if (item is AnyState anyState)
            // {
            //     _allAnyStates.Add(anyState);
            // }
            // else
            // {
            //     Debug.LogError(
            //         $"[StateFolder] Failed to add '{item.Name}' to StateFolder '{name}' because it is not an AnyState.???",
            //         this);
            // }
        }

        public override void AddExternalSource(object source)
        {
            base.AddExternalSource(source);
            //hmm這段特規處理，不太爽
            if (source is StateFolder dict)
            {
                foreach (var item in dict._anyStates)
                {
                    _allAnyStates.Add(item);
                }
            }
        }

        // protected override void AddExternalImplement(MonoStateBehaviour item)
        // {
        //     base.AddExternalImplement(item);
        //
        // }

        protected override void RemoveImplement(MonoStateBehaviour item)
        {
        }

        protected override bool CanBeAdded(MonoStateBehaviour item)
        {
            if (item is AnyState)
            {
                return false;
            }

            return true;
        }
    }
}
