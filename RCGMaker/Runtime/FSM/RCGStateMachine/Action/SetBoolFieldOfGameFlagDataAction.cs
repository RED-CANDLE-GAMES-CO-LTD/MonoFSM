using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGFSM.Variable
{
    public class SetBoolFieldOfGameFlagDataAction : AbstractStateAction
    {
        public AbstractVariable targetVariable;
        public bool TargetValue = true;

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

            flag.CurrentValue = TargetValue;
        }
    }
}