using System.Linq;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGUIBinder;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables
{
    public class MonoRefFromVariableOfMonoDescriptableProvider:UIMonoDescriptableProvider
    {
        
        bool IsVariableTagNotMatchingMonoTag()
        {
            if (_variableTag == null)
            {
                // Debug.LogError("VariableTag is null",this);
                return true;
            }

            if(monoTag.containsVariableTypeTags.Contains(_variableTag) == false)
            {
                return true;
            }
            return false;
        }
        [InfoBox("VariableTag should be in the MonoTag", InfoMessageType.Error, nameof(IsVariableTagNotMatchingMonoTag))]
        public VariableTag _variableTag;

        public override MonoDescriptable monoInstance
        {
            get
            {
                if(Application.isPlaying == false)
                {
                    return null;
                }
                if (_variableTag == null)
                {
                    Debug.LogError("VariableTag is null",this);
                    return null;
                }

                var variable = base.monoInstance.GetVariable(_variableTag);
                if(variable == null)
                {
                    Debug.LogError("Variable is null"+_variableTag,this);
                    return null;
                }
                var monoDescriptable = variable.GetValue<MonoDescriptable>();
                return monoDescriptable;

            }
        }
    }
}