using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGFSM.Variable
{
    public class SetBoolFieldOfVariableAction : AbstractStateAction
    {
        public AbstractVariable targetVariable;
        public bool TargetValue = true;
        public SetBoolType targetType;

        public enum SetBoolType
        {
            True,
            False,
            Toggle
        }
        private FieldInfo targetField;

        [ValueDropdown(nameof(GetAllFieldNames))]
        public string targetFieldName;

        private IEnumerable<string> GetAllFieldNames()
        {
            if (targetVariable == null) yield break;
            foreach (var field in targetVariable.FinalDataType.GetFields())
            {
                if (field.FieldType != typeof(FlagFieldBool)) continue;
                yield return field.Name;
            }
        }

        protected override void OnStateEnterImplement()
        {
            if (targetVariable == null)
            {
                Debug.LogError("SetBoolFieldOfGameFlagDataAction: targetVariable is null", this);
                return;
            }
            if (targetVariable.FinalData == null)
            {
                Debug.LogWarning(
                    $"SetBoolFieldOfGameFlagDataAction: targetVariable.FinalData:{targetVariable.name} is null", this);
                return;
            }
            targetField = targetVariable.FinalDataType.GetField(targetFieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (targetField == null)
            {
                Debug.LogError($"SetBoolFieldOfGameFlagDataAction: {targetFieldName} not found");
                return;
            }

            var flag = targetField.GetValue(targetVariable.FinalData) as FlagFieldBool;
            if (flag == null)
            {
                Debug.LogError($"SetBoolFieldOfGameFlagDataAction: {targetFieldName} is not FlagFieldBool");
                return;
            }

            if (targetType == SetBoolType.Toggle)
                flag.CurrentValue = !flag.CurrentValue;
            else
            {
                //FIXME: refactor: use switch
                flag.CurrentValue = TargetValue;
            }
                
        }
    }
}